using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;

namespace Core.Logic
{
    /// <summary>
    ///     Processes moves and returns new game state.
    ///     Coordinates PathCalculator, ConnectionValidator, and QuestEvaluator.
    ///     Pure functions - no side effects, completely testable.
    /// </summary>
    public class MoveProcessor
    {
        private readonly ConnectionValidator _connectionValidator;
        private readonly PathCalculator _pathCalculator;
        private readonly QuestEvaluator _questEvaluator;

        public MoveProcessor()
        {
            _pathCalculator = new PathCalculator();
            _connectionValidator = new ConnectionValidator();
            _questEvaluator = new QuestEvaluator();
        }

        /// <summary>
        ///     Processes a move and returns the complete result.
        ///     This is the main entry point for game logic.
        /// </summary>
        public MoveResult ProcessMove(GameState currentState, Move move)
        {
            // Apply the move to get new grid state
            GridState newGrid;
            try
            {
                newGrid = ApplyMoveToGrid(currentState.Grid, move);
            }
            catch (Exception ex)
            {
                return MoveResult.Invalid(currentState, move, ex.Message);
            }

            // Recalculate paths based on new grid
            var newPaths = move.Type switch
            {
                MoveType.Rotate => _pathCalculator.UpdateForTileChange(
                    currentState.Grid, newGrid, move.Slot),
                MoveType.Swap => _pathCalculator.UpdateForTileSwap(
                    currentState.Grid, newGrid, move.Slot, move.TargetSlot!.Value),
                _ => _pathCalculator.CalculatePathNetwork(newGrid)
            };

            // Create new game state
            var newGameState = new GameState(
                newGrid,
                newPaths,
                currentState.Quest,
                currentState.MoveCount + 1
            );

            // Validate connections
            var validationResult = _connectionValidator.ValidateConnections(newPaths);

            // Evaluate quest
            var questResult = _questEvaluator.EvaluateQuest(currentState.Quest, newPaths);

            // Return complete result
            return new MoveResult(
                newGameState,
                move,
                validationResult,
                questResult
            );
        }

        /// <summary>
        ///     Previews a move without committing it.
        ///     Useful for AI, hints, or undo preview.
        /// </summary>
        public MoveResult PreviewMove(GameState currentState, Move move)
        {
            // Same as ProcessMove but doesn't increment move count
            return ProcessMove(currentState, move);
        }

        /// <summary>
        ///     Checks if a move is legal (can be applied to current state).
        /// </summary>
        public bool IsMoveValid(GameState currentState, Move move)
        {
            try
            {
                ApplyMoveToGrid(currentState.Grid, move);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Gets all possible moves from the current state.
        ///     Useful for AI and hint systems.
        /// </summary>
        public List<Move> GetAllPossibleMoves(GameState currentState)
        {
            var moves = new List<Move>();

            // For each occupied slot, add rotation moves
            foreach (var (slot, tile) in currentState.Grid.GetOccupiedSlots())
            {
                var maxRotations = tile.GetMaxRotations();
                for (var rotation = 0; rotation < maxRotations; rotation++)
                    if (rotation != tile.Rotation) // Don't include current rotation
                        moves.Add(Move.Rotate(slot, rotation));
            }

            // For each pair of occupied slots, add swap move
            var occupiedSlots = currentState.Grid.GetOccupiedSlots()
                .Select(x => x.position)
                .ToArray();

            for (var i = 0; i < occupiedSlots.Length; i++)
            for (var j = i + 1; j < occupiedSlots.Length; j++)
                moves.Add(Move.Swap(occupiedSlots[i], occupiedSlots[j]));

            return moves;
        }

        /// <summary>
        ///     Applies a move to the grid and returns the new grid state.
        /// </summary>
        private GridState ApplyMoveToGrid(GridState grid, Move move)
        {
            return move.Type switch
            {
                MoveType.Rotate => ApplyRotation(grid, move),
                MoveType.Swap => ApplySwap(grid, move),
                _ => throw new ArgumentException($"Unknown move type: {move.Type}")
            };
        }

        private GridState ApplyRotation(GridState grid, Move move)
        {
            if (!move.NewRotation.HasValue)
                throw new InvalidOperationException("Rotation move must have NewRotation value");

            var tile = grid.GetTile(move.Slot);
            if (!tile.HasValue)
                throw new InvalidOperationException($"Cannot rotate empty slot {move.Slot}");

            // Validate rotation is within allowed range
            var maxRotations = tile.Value.GetMaxRotations();
            if (move.NewRotation.Value < 0 || move.NewRotation.Value >= maxRotations)
                throw new ArgumentException(
                    $"Rotation {move.NewRotation.Value} is invalid for tile type {tile.Value.Type} (max: {maxRotations})");

            return grid.WithRotation(move.Slot, move.NewRotation.Value);
        }

        private GridState ApplySwap(GridState grid, Move move)
        {
            if (!move.TargetSlot.HasValue)
                throw new InvalidOperationException("Swap move must have TargetSlot value");

            return grid.WithSwap(move.Slot, move.TargetSlot.Value);
        }
    }

    /// <summary>
    ///     Complete result of processing a move.
    ///     Contains all information about what happened.
    /// </summary>
    public struct MoveResult
    {
        public GameState NewState { get; }
        public Move Move { get; }
        public ValidationResult Validation { get; }
        public QuestResult QuestResult { get; }
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public MoveResult(
            GameState newState,
            Move move,
            ValidationResult validation,
            QuestResult questResult)
        {
            NewState = newState;
            Move = move;
            Validation = validation;
            QuestResult = questResult;
            IsValid = true;
            ErrorMessage = null;
        }

        private MoveResult(GameState currentState, Move move, string errorMessage)
        {
            NewState = currentState;
            Move = move;
            Validation = default;
            QuestResult = default;
            IsValid = false;
            ErrorMessage = errorMessage;
        }

        public static MoveResult Invalid(GameState currentState, Move move, string errorMessage)
        {
            return new MoveResult(currentState, move, errorMessage);
        }

        /// <summary>
        ///     True if this move results in a winning state.
        /// </summary>
        public bool IsWinningMove =>
            IsValid &&
            Validation.IsValid &&
            QuestResult.IsSuccessful;

        /// <summary>
        ///     True if the move is legal and creates valid connections.
        /// </summary>
        public bool IsLegalMove =>
            IsValid &&
            Validation.IsValid;

        public override string ToString()
        {
            if (!IsValid)
                return $"Invalid Move: {ErrorMessage}";

            return "Move Result:\n" +
                   $"  Move: {Move}\n" +
                   $"  Validation: {(Validation.IsValid ? "PASS" : "FAIL")}\n" +
                   $"  Quest: {QuestResult}\n" +
                   $"  Winning: {IsWinningMove}";
        }
    }
}