using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;

namespace BezierRenderSystem
{
    public class BoundingBox
    {
        public Vector2 Min { get; set; }
        public Vector2 Max { get; set; }

        public BoundingBox()
        {
            Min = Vector2.Zero;
            Max = Vector2.Zero;
        } 

        public BoundingBox(Vector2 min, Vector2 max)
        {
            Min = Vector2.Min(min, max);
            Max = Vector2.Max(min, max);
        }

        public void Union(Vector2 point)
        {
            Min = Vector2.Min(Min, point);
            Max = Vector2.Max(Max, point);
        }
    }
}
