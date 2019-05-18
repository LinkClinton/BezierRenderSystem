using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalEngine;

namespace BezierRenderSystem
{
    public class BezierComponent : Component
    {
        public Position<float>[] Controls { get; set; }
        public Color<float>[] Colors { get; set; }

        public BezierComponent()
        {
            BaseComponentType = typeof(BezierComponent);

            Controls = new Position<float>[] 
            {
                new Position<float>(0, 0),
                new Position<float>(0.5f, 0),
                new Position<float>(1, 1)
            };

            Colors = new Color<float>[]
            {
                new Color<float>(0, 0, 0, 1),
                new Color<float>(0, 0, 0, 1),
                new Color<float>(0, 0, 0, 1)
            };
        }
    }
}
