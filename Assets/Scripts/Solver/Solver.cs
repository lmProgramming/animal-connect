using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Models;
using Core.Logic;

namespace Solver
{
    /// <summary>
    /// Brute-force solver for Animal Connect puzzles.
    /// Uses the new Core architecture.
    /// </summary>
    public class Solver : MonoBehaviour
    {
        private DateTime _startTime;
        private int _iterations;
        private const int MAX_ITERATIONS = 100000;
        private const int TIME_LIMIT_SECONDS = 10;

        private readonly TileData[] _tilesToUse = new TileData[]
        {
            new TileData(TileType.Curve, 0),
            new TileData(TileType.Curve, 0),
            new TileData(TileType.Curve, 0),
            new TileData(TileType.TwoCurves, 0),
            new TileData(TileType.TwoCurves, 0),
            new TileData(TileType.XIntersection, 0),
            new TileData(TileType.Bridge, 0),
            new TileData(TileType.Intersection, 0),
            new TileData(TileType.Intersection, 0)
        };

        public void SolveCurrentQuest()
        {
            if (GameManager.Instance == null || GameManager.Instance.Quest == null)
            {
                Debug.LogError("Solver: No active quest!");
                return;
            }
            SolveQuest(GameManager.Instance.Quest);
        }

        public void SolveQuest(Quest.Quest quest)
        {
            Debug.Log("🔍 Solver: Starting...");
            _startTime = DateTime.Now;
            _iterations = 0;

            var questData = quest.ToQuestData();
            var solution = FindSolution(questData);

            if (solution != null)
            {
                Debug.Log($"✅ Solution found in {_iterations} iterations!");
                PrintSolution(solution);
            }
            else
            {
                Debug.Log($"❌ No solution after {_iterations} iterations");
            }
        }

        private GridState FindSolution(QuestData questData)
        {
            var gridState = new GridState();
            var freeSlots = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            return ExploreFurther(0, gridState, freeSlots, questData);
        }

        private GridState ExploreFurther(int tileIndex, GridState currentGrid, List<int> freeSlots, QuestData questData)
        {
            if ((DateTime.Now - _startTime).TotalSeconds > TIME_LIMIT_SECONDS || _iterations > MAX_ITERATIONS)
            {
                return null;
            }

            _iterations++;

            if (tileIndex >= _tilesToUse.Length)
            {
                return IsWinning(currentGrid, questData) ? currentGrid : null;
            }

            var tile = _tilesToUse[tileIndex];
            int maxRotations = GetMaxRotations(tile.Type);

            for (int i = 0; i < freeSlots.Count; i++)
            {
                int slot = freeSlots[i];

                for (int rotation = 0; rotation < maxRotations; rotation++)
                {
                    var rotatedTile = new TileData(tile.Type, rotation);
                    var newGrid = currentGrid.WithTile(slot, rotatedTile);

                    if (!QuickValidate(newGrid, slot))
                        continue;

                    var newFreeSlots = new List<int>(freeSlots);
                    newFreeSlots.RemoveAt(i);

                    var solution = ExploreFurther(tileIndex + 1, newGrid, newFreeSlots, questData);
                    if (solution != null)
                        return solution;
                }
            }
            return null;
        }

        private bool QuickValidate(GridState grid, int placedSlot)
        {
            var pathCalculator = new PathCalculator();
            var pathNetwork = pathCalculator.CalculatePathNetwork(grid);
            
            // Get the 4 path points for this slot
            var slotPathPoints = Core.Configuration.GridConfiguration.SlotToPathPoints[placedSlot];

            foreach (var pathPoint in slotPathPoints)
            {
                int connectionCount = pathNetwork.GetConnectionCount(pathPoint);
                if (connectionCount > 2)
                    return false;
                if (Core.Configuration.GridConfiguration.IsEntityPoint(pathPoint) && connectionCount > 1)
                    return false;
            }
            return true;
        }

        private bool IsWinning(GridState gridState, QuestData questData)
        {
            var pathCalculator = new PathCalculator();
            var pathNetwork = pathCalculator.CalculatePathNetwork(gridState);
            var validator = new ConnectionValidator();
            
            if (!validator.IsValid(pathNetwork))
                return false;

            var questEvaluator = new QuestEvaluator();
            var result = questEvaluator.EvaluateQuest(questData, pathNetwork);
            return result.IsComplete;
        }

        private int GetMaxRotations(TileType type)
        {
            return type switch
            {
                TileType.TwoCurves => 2,
                TileType.XIntersection => 1,
                TileType.Bridge => 2,
                _ => 4
            };
        }

        private void PrintSolution(GridState solution)
        {
            Debug.Log("=== SOLUTION ===");
            for (int i = 0; i < 9; i++)
            {
                var tile = solution.GetTile(i);
                if (tile.HasValue)
                    Debug.Log($"Slot {i}: {tile.Value.Type} R{tile.Value.Rotation}");
                else
                    Debug.Log($"Slot {i}: Empty");
            }
            Debug.Log("================");
        }
    }
}
