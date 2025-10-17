using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Solver
{
    public class Solver : MonoBehaviour
    {
        private readonly Entity[] _entities = new Entity[12];
        private readonly GridSlotVirtual[] _gridSlots = new GridSlotVirtual[9];
        private readonly PathPoint[] _pathPoints = new PathPoint[24];
        private readonly TileVirtual[] _tiles = new TileVirtual[9];
        private DateTime _currentTime;

        private int _x;

        private void Start()
        {
            _tiles[0] = new TileVirtual(Tile.TileType.Curve);
            _tiles[1] = new TileVirtual(Tile.TileType.Curve);
            _tiles[2] = new TileVirtual(Tile.TileType.Curve);
            _tiles[3] = new TileVirtual(Tile.TileType.TwoCurves);
            _tiles[4] = new TileVirtual(Tile.TileType.TwoCurves);
            _tiles[5] = new TileVirtual(Tile.TileType.XIntersection);
            _tiles[6] = new TileVirtual(Tile.TileType.Bridge);
            _tiles[7] = new TileVirtual(Tile.TileType.Intersection);
            _tiles[8] = new TileVirtual(Tile.TileType.Intersection);

            for (var i = 0; i < 9; i++) _gridSlots[i] = new GridSlotVirtual();

            for (var i = 0; i < 24; i++) _pathPoints[i] = new PathPoint(-1, 0);

            _gridSlots[0].PathPoints[0] = _pathPoints[0];
            _gridSlots[0].PathPoints[1] = _pathPoints[13];
            _gridSlots[0].PathPoints[2] = _pathPoints[3];
            _gridSlots[0].PathPoints[3] = _pathPoints[12];

            _gridSlots[1].PathPoints[0] = _pathPoints[1];
            _gridSlots[1].PathPoints[1] = _pathPoints[14];
            _gridSlots[1].PathPoints[2] = _pathPoints[4];
            _gridSlots[1].PathPoints[3] = _pathPoints[13];

            _gridSlots[2].PathPoints[0] = _pathPoints[2];
            _gridSlots[2].PathPoints[1] = _pathPoints[15];
            _gridSlots[2].PathPoints[2] = _pathPoints[5];
            _gridSlots[2].PathPoints[3] = _pathPoints[14];

            _gridSlots[3].PathPoints[0] = _pathPoints[3];
            _gridSlots[3].PathPoints[1] = _pathPoints[17];
            _gridSlots[3].PathPoints[2] = _pathPoints[6];
            _gridSlots[3].PathPoints[3] = _pathPoints[16];

            _gridSlots[4].PathPoints[0] = _pathPoints[4];
            _gridSlots[4].PathPoints[1] = _pathPoints[18];
            _gridSlots[4].PathPoints[2] = _pathPoints[7];
            _gridSlots[4].PathPoints[3] = _pathPoints[17];

            _gridSlots[5].PathPoints[0] = _pathPoints[5];
            _gridSlots[5].PathPoints[1] = _pathPoints[19];
            _gridSlots[5].PathPoints[2] = _pathPoints[8];
            _gridSlots[5].PathPoints[3] = _pathPoints[18];

            _gridSlots[6].PathPoints[0] = _pathPoints[6];
            _gridSlots[6].PathPoints[1] = _pathPoints[21];
            _gridSlots[6].PathPoints[2] = _pathPoints[9];
            _gridSlots[6].PathPoints[3] = _pathPoints[20];

            _gridSlots[7].PathPoints[0] = _pathPoints[7];
            _gridSlots[7].PathPoints[1] = _pathPoints[22];
            _gridSlots[7].PathPoints[2] = _pathPoints[10];
            _gridSlots[7].PathPoints[3] = _pathPoints[21];

            _gridSlots[8].PathPoints[0] = _pathPoints[8];
            _gridSlots[8].PathPoints[1] = _pathPoints[23];
            _gridSlots[8].PathPoints[2] = _pathPoints[11];
            _gridSlots[8].PathPoints[3] = _pathPoints[22];

            _entities[0] = new Entity(0, _pathPoints[0]);
            _entities[1] = new Entity(0, _pathPoints[1]);
            _entities[2] = new Entity(0, _pathPoints[2]);
            _entities[3] = new Entity(0, _pathPoints[15]);
            _entities[4] = new Entity(0, _pathPoints[19]);
            _entities[5] = new Entity(0, _pathPoints[23]);
            _entities[6] = new Entity(0, _pathPoints[11]);
            _entities[7] = new Entity(0, _pathPoints[10]);
            _entities[8] = new Entity(0, _pathPoints[9]);
            _entities[9] = new Entity(0, _pathPoints[20]);
            _entities[10] = new Entity(0, _pathPoints[16]);
            _entities[11] = new Entity(0, _pathPoints[12]);
        }

        public void SolveQuest(Quest.Quest quest)
        {
            var entitiesIndexesThatMustBeConnectedList = new List<int>();
            for (var i = 0; i < quest.entitiesToConnectIDs.Count; i++)
                if (quest.entitiesToConnectIDs[i].entitiesIDs.Count > 1)
                    for (var j = 0; j < quest.entitiesToConnectIDs[i].entitiesIDs.Count; j++)
                        entitiesIndexesThatMustBeConnectedList.Add(quest.entitiesToConnectIDs[i].entitiesIDs[j]);

            _currentTime = DateTime.Now;

            entitiesIndexesThatMustBeConnectedList.Sort();
            var entitiesIndexesThatMustBeConnected = entitiesIndexesThatMustBeConnectedList.Distinct().ToArray();

            var tilesOccupied = new List<int>();
            var tilesFree = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

            _x = 0;

            ExploreFurther(0, tilesOccupied, tilesFree, quest);

            Debug.Log("MIAU");

            Debug.Log(_gridSlots);

            PrintCurrentBoard();
        }

        public void SolveCurrentQuest()
        {
            SolveQuest(GameManager.Instance.Quest);
        }

        public void PrintCurrentBoard()
        {
            for (var i = 0; i < 9; i++) Debug.Log(_gridSlots[i].Tile.TileType + " " + _gridSlots[i].Tile.Rotations);
        }

        private bool ExploreFurther(int tilesUsed, List<int> tilesOccupied, List<int> tilesFree, Quest.Quest quest)
        {
            if (tilesUsed >= 8) return CheckIfWon(quest);

            if (_currentTime.Second + 4 < DateTime.Now.Second || _x > 10000)
            {
                Debug.Log(tilesUsed);
                Debug.Log(_x);
                throw new Exception();
            }

            _x++;

            var tile = _tiles[tilesUsed];

            tilesUsed++;

            for (var i = 0; i < tilesFree.Count; i++)
            {
                var tileIndex = tilesFree[i];

                _gridSlots[tileIndex].Tile = _tiles[tilesUsed];

                tilesFree.RemoveAt(i);

                for (var j = 0; j < 4; j++)
                {
                    Debug.Log(tilesUsed);

                    PrintCurrentBoard();

                    _x++;

                    if (_currentTime.Second + 4 < DateTime.Now.Second || _x > 100)
                    {
                        Debug.Log(tilesUsed);
                        Debug.Log(_x);
                        PrintCurrentBoard();

                        throw new Exception();
                    }

                    tile.Rotations = j;

                    if (!FastCheckIfConnectionsAreValid(tileIndex)) return false;

                    var won = ExploreFurther(tilesUsed, tilesOccupied, tilesFree, quest);

                    if (won) return true;
                }

                tilesFree.Add(tileIndex);

                _gridSlots[tileIndex].Tile = null;
            }

            return false;
        }

        private bool FastCheckIfConnectionsAreValid(int gridSlotIndex)
        {
            return
                _gridSlots[gridSlotIndex].PathPoints[0].CheckIfPathOnTopValid() &&
                _gridSlots[gridSlotIndex].PathPoints[1].CheckIfPathOnTopValid() &&
                _gridSlots[gridSlotIndex].PathPoints[2].CheckIfPathOnTopValid() &&
                _gridSlots[gridSlotIndex].PathPoints[3].CheckIfPathOnTopValid();
        }

        private bool CheckIfWon(Quest.Quest quest)
        {
            if (quest.CheckIfCompleted(_entities))
            {
                for (var i = 0; i < _gridSlots.Length; i++)
                    if (_gridSlots[i].Tile == null)
                        return false;

                for (var i = 0; i < _pathPoints.Length; i++)
                    if (!_pathPoints[i].CheckIfPathOnTopValid())
                        return false;

                return true;
            }

            return false;
        }
    }
}