using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalEngine;

namespace BezierRenderSystem
{
    class Flower
    {
        private static float mHeight = 0.5f;
        private static Random mRandom = new Random(0);

        private static Color<float> Any => new Color<float>(
            (float)mRandom.NextDouble(),
            (float)mRandom.NextDouble(),
            (float)mRandom.NextDouble(),
            (float)mRandom.NextDouble() * 0.5f + 0.5f);

        public GameObject[] Leaves { get; }

        public Flower(Position<float> position, float radius)
        {
            float halfRadius = radius * 0.5f;
            float height = radius * mHeight;
            
            Position<float>[] midOffset = new Position<float>[]
            {
                new Position<float>(halfRadius, height),
                new Position<float>(halfRadius, -height),
                new Position<float>(height, -halfRadius),
                new Position<float>(-height, -halfRadius),
                new Position<float>(-halfRadius, -height),
                new Position<float>(-halfRadius, height),
                new Position<float>(-height, halfRadius),
                new Position<float>(height, halfRadius)
            };

            Position<float>[] endOffset = new Position<float>[]
            {
                new Position<float>(radius, 0),
                new Position<float>(radius, 0),
                new Position<float>(0, -radius),
                new Position<float>(0, -radius),
                new Position<float>(-radius, 0),
                new Position<float>(-radius, 0),
                new Position<float>(0, radius),
                new Position<float>(0, radius)
            };

            Leaves = new GameObject[8];

            var centerColor = Any;
            var endColor = Any;

            for (int i = 0; i < Leaves.Length; i++)
            {
                var bezierComponent = new BezierComponent()
                {
                    Controls = new Position<float>[]
                    {
                        position,
                        new Position<float>(position.X + midOffset[i].X, position.Y + midOffset[i].Y),
                        new Position<float>(position.X + endOffset[i].X, position.Y + endOffset[i].Y)
                    },
                    Colors = new Color<float>[]
                    {
                        centerColor,
                        Any,
                        endColor
                    }
                };

                Leaves[i] = new GameObject();
                Leaves[i].AddComponent(bezierComponent);
            }
        }
    }

    class Program
    { 
        static void Main(string[] args)
        {
            const int width = 1920;
            const int height = 1080;
            
            var adapters = GalEngine.Runtime.Graphics.GpuAdapter.EnumerateGraphicsAdapter();

            GameSystems.Initialize(new GameStartInfo()
            {
                WindowName = "Bezier Render System",
                GameName = "Bezier Render System",
                IconName = null,
                WindowSize = new Size<int>(width, height),
                Adapter = adapters[0]
            });

            BezierRender bezierRender = new BezierRender(GameSystems.GpuDevice);

            /*GameSystems.AddBehaviorSystem(
                new BezierFillSystem(bezierRender,
                new Rectangle<int>(0, 0, width, height)));*/

            GameSystems.AddBehaviorSystem(
               new BezierDrawSystem(bezierRender,
               new Rectangle<int>(0, 0, width, height)));

            GameSystems.MainScene = new GameScene("Main");

            Random random = new Random(0);
            Flower[] flowers = new Flower[500];

            for (int i = 0; i < flowers.Length; i++)
            {
                flowers[i] = new Flower(
                    new Position<float>(random.Next(0, width), random.Next(0, height)),
                    random.Next(100, 200));

                foreach (var leaf in flowers[i].Leaves)
                {
                    GameSystems.MainScene.AddGameObject(leaf);
                }
            }
            
            GameSystems.RunLoop();
        }
    }
}
