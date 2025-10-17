using System;
using Core.Models;

namespace Core.Models
{
    /// <summary>
    /// Represents the complete game state at any point in time.
    /// Immutable - any changes create a new GameState instance.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public GridState Grid { get; }
        public PathNetworkState Paths { get; }
        public QuestData Quest { get; }
        public int MoveCount { get; }
        
        public GameState(GridState grid, PathNetworkState paths, QuestData quest, int moveCount = 0)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Paths = paths ?? throw new ArgumentNullException(nameof(paths));
            Quest = quest ?? throw new ArgumentNullException(nameof(quest));
            MoveCount = moveCount;
        }
        
        /// <summary>
        /// Creates a new GameState with updated components.
        /// </summary>
        public GameState WithUpdates(
            GridState grid = null,
            PathNetworkState paths = null,
            QuestData quest = null,
            int? moveCount = null)
        {
            return new GameState(
                grid ?? Grid,
                paths ?? Paths,
                quest ?? Quest,
                moveCount ?? MoveCount
            );
        }
        
        /// <summary>
        /// Creates a new GameState with move count incremented.
        /// </summary>
        public GameState WithMoveIncremented()
        {
            return new GameState(Grid, Paths, Quest, MoveCount + 1);
        }
        
        public override string ToString()
        {
            return $"GameState (Move {MoveCount}):\n{Grid}\n{Paths}";
        }
    }
    
    /// <summary>
    /// Represents a move in the game.
    /// </summary>
    [Serializable]
    public struct Move
    {
        public MoveType Type { get; }
        public int Slot { get; }
        public int? TargetSlot { get; } // For swaps
        public int? NewRotation { get; } // For rotations
        
        private Move(MoveType type, int slot, int? targetSlot = null, int? newRotation = null)
        {
            Type = type;
            Slot = slot;
            TargetSlot = targetSlot;
            NewRotation = newRotation;
        }
        
        public static Move Rotate(int slot, int newRotation)
        {
            return new Move(MoveType.Rotate, slot, newRotation: newRotation);
        }
        
        public static Move Swap(int slot, int targetSlot)
        {
            return new Move(MoveType.Swap, slot, targetSlot: targetSlot);
        }
        
        public override string ToString()
        {
            return Type switch
            {
                MoveType.Rotate => $"Rotate slot {Slot} to rotation {NewRotation}",
                MoveType.Swap => $"Swap slots {Slot} and {TargetSlot}",
                _ => "Unknown move"
            };
        }
    }
    
    public enum MoveType
    {
        Rotate,
        Swap
    }
}
