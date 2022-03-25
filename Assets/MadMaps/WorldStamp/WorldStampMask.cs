using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.Collections;
using UnityEngine;

namespace MadMaps.WorldStamps
{
    [Serializable]
    public class WorldStampMask : MadMaps.Common.Collections.CompositionDictionary<int, float>, Common.Painter.IPaintable
    {
        public bool Dirty { get; set; }

        public void SetValue(int cell, float val)
        {
            if (ContainsKey(cell) && Math.Abs(this[cell] - val) < .0001f)
            {
                return;
            }
            this[cell] = val;
            Dirty = true;
        }

        public float AvgValue()
        {
            float avg = 0;
            int count = 0;
            var enumerator = AllValues();
            while (enumerator.MoveNext())
            {
                avg += enumerator.Current.Value;
                count++;
            }
            return avg/count;
        }

        public float GetValue(int cell)
        {
            float val;
            if (!TryGetValue(cell, out val))
            {
                return 0;
            }
            return val;
        }

        public IEnumerator<KeyValuePair<int, float>> AllValues()
        {
            return GetEnumerator();
        }

        public void RemoveCell(int cell)
        {
            if (Remove(cell))
            {
                Dirty = true;
            }
        }

        public float GetBilinear(Common.Painter.GridManagerInt gridManager, Vector3 position)
        {
            var gridSize = gridManager.GetGridSize();
            var xDiff = Mathf.Abs(position.x) % gridSize;
            var zDiff = Mathf.Abs(position.z) % gridSize;
            var xSign = Mathf.Sign(position.x);
            if (xDiff < gridSize / 2f)
            {
                xSign *= -1;
            }
            var zSign = Mathf.Sign(position.z);
            if (zDiff < gridSize / 2f)
            {
                zSign *= -1;
            }
            var xOffset = new Vector2(gridSize, 0) * xSign;
            var zOffset = new Vector2(0, gridSize) * zSign;

            var p0 = gridManager.GetCellCenter(position);
            var p1 = p0 + xOffset;
            var p2 = p0 + zOffset;
            var p3 = p0 + xOffset + zOffset;

            var v0 = GetValue(gridManager.GetCell(p0.x0z()));
            var v1 = GetValue(gridManager.GetCell(p1.x0z()));
            var v2 = GetValue(gridManager.GetCell(p2.x0z()));
            var v3 = GetValue(gridManager.GetCell(p3.x0z()));

            var total = gridSize*gridSize;

            var w0 = ((gridSize - Mathf.Abs(position.x - p0.x))*(gridSize - Mathf.Abs(position.z - p0.y)))/total;
            var w1 = ((gridSize - Mathf.Abs(position.x - p1.x))*(gridSize - Mathf.Abs(position.z - p1.y)))/total;
            var w2 = ((gridSize - Mathf.Abs(position.x - p2.x))*(gridSize - Mathf.Abs(position.z - p2.y)))/total;
            var w3 = ((gridSize - Mathf.Abs(position.x - p3.x))*(gridSize - Mathf.Abs(position.z - p3.y)))/total;

            return w0*v0 + w1*v1 + w2*v2 + w3*v3;
        }

    }
}