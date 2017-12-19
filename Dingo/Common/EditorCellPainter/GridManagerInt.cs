using UnityEngine;

#if !HURTWORLDSDK
namespace Dingo.Common.Painter
{
    public class GridManagerInt : IGridManager
    {
        public readonly float GRID_SIZE;
        public readonly int GRID_MAX;
        public readonly int UNSIGNED_GRID_MAX;
        public readonly int UNSIGNED_GRID_MAX_SQUARED;
        public readonly int GLOBAL_OFFSET;

        public GridManagerInt(float cellSize, int offset = 0, int size = 100 * 1000)
        {
            GLOBAL_OFFSET = offset;
            GRID_SIZE = cellSize;
            GRID_MAX = (int) (size / GRID_SIZE);
            UNSIGNED_GRID_MAX = GRID_MAX * 2;
            UNSIGNED_GRID_MAX_SQUARED = UNSIGNED_GRID_MAX * UNSIGNED_GRID_MAX;
        }

        public int GetCell(Vector3 position)
        {
            return Mathf.FloorToInt(position.x / GRID_SIZE) + GRID_MAX
                   + ((Mathf.FloorToInt(position.z / GRID_SIZE) + GRID_MAX) * UNSIGNED_GRID_MAX) + GLOBAL_OFFSET;
        }

        public Vector2 GetCellMax(Vector3 pos)
        {
            return GetCellMax(GetCell(pos));
        }

        public Vector2 GetCellMax(int cell)
        {
            //Increment x and y
            return GetCellMin(cell + 1 + UNSIGNED_GRID_MAX);
        }

        public Vector2 GetCellMin(Vector3 pos)
        {
            return GetCellMin(GetCell(pos));
        }

        public Vector2 GetCellMin(int cell)
        {
            cell -= GLOBAL_OFFSET;
            return (new Vector2(cell % UNSIGNED_GRID_MAX, (cell / UNSIGNED_GRID_MAX) % UNSIGNED_GRID_MAX)
                    - new Vector2(GRID_MAX, GRID_MAX)) * GRID_SIZE;
        }

        public int GetOffset(int x, int y)
        {
            return x + y * UNSIGNED_GRID_MAX;
        }

        public int GetOffset(Vector2 offset)
        {
            return Mathf.FloorToInt(offset.x / GRID_SIZE) + Mathf.FloorToInt(offset.y / GRID_SIZE) * UNSIGNED_GRID_MAX;
        }

        public Vector2 GetCellCenter(int cell)
        {
            return GetCellMin(cell) + Vector2.one * (GRID_SIZE / 2);
        }

        public Vector2 GetCellCenter(Vector3 pos)
        {
            return GetCellCenter(GetCell(pos));
        }

        public Vector2 GetRandomPointInCell(int cell)
        {
            var cellMin = GetCellMin(cell);

            return new Vector2(Random.Range(cellMin.x, cellMin.x + GRID_SIZE),
                Random.Range(cellMin.y, cellMin.y + GRID_SIZE));
        }

        public float GetGridSize()
        {
            return GRID_SIZE;
        }
    }
}
#endif