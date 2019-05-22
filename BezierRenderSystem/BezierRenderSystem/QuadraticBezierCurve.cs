using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;

namespace BezierRenderSystem
{
    public static class QuadraticBezierCurve
    {
        public static Vector2[] ParameterFormat(Vector2[] controls)
        {
            //the formula of bezier curve is Q(t) = At^2 + Bt + C
            //where A = (P0 - 2P1 + P2), B = (2P1 - 2P0), C = (P0)
            //so the derivative Q'(t) = 2At + B
            var A = controls[0] - 2 * controls[1] + controls[2];
            var B = 2 * (controls[1] - controls[0]);
            var C = controls[0];

            return new Vector2[] { A, B, C };
        }

        public static BoundingBox BoundingBox(Vector2[] controls, float width)
        {
            //first, we compute the tangent(Q'(t)) at t = 0 and t = 1
            //so the segment of end-point is (Q(ep) - Q'(ep) * width * 0.5, Q(ep) + Q'(ep) * width * 0.5)
            //where ep = 0 or ep = 1
            //the bounding box of bezier is an AABB contains these points(control points and end-points)
            var parameters = ParameterFormat(controls);
            var A = parameters[0];
            var B = parameters[1];
            var C = parameters[2];

            //ep = 0, ep = 1
            var derivativeQ0 = Vector2.Normalize(B);
            var derivativeQ1 = Vector2.Normalize(2 * A + B);

            //delta offset for Q(0) and Q(1)
            var deltaQ0 = derivativeQ0 * width * 0.5f;
            var deltaQ1 = derivativeQ1 * width * 0.5f;



            //accept the control points
            BoundingBox box = new BoundingBox(controls[0], controls[1]);
            box.Union(controls[2]);

            //accept the end-points of segment
            box.Union(controls[0] - deltaQ0);
            box.Union(controls[0] + deltaQ0);
            box.Union(controls[2] - deltaQ1);
            box.Union(controls[2] + deltaQ1);

            return box;
        }
    }
}
