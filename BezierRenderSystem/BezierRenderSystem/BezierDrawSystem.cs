using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;

using GalEngine;
using GalEngine.Runtime.Graphics;

namespace BezierRenderSystem
{
    public class BezierDrawSystem : BehaviorSystem
    {
        private Image mCanvas;
        private BezierRender mRender;

        public Rectangle<int> Area { get; set; }

        public BezierDrawSystem(BezierRender render, Rectangle<int> area) : base("BezierDrawSystem")
        {
            RequireComponents.AddRequireComponentType<BezierComponent>();

            Area = area;

            mRender = render;

            mCanvas = new Image(
               new Size<int>(Area.Right - Area.Left, Area.Bottom - Area.Top),
               PixelFormat.RedBlueGreenAlpha8bit,
               mRender.GpuDevice);
        }

        protected override void Update()
        {
            //update the render area, we need to update the canvas and render target
            //if the area's size is not equal the canvas's size
            if (mCanvas.Size.Width != Area.Right - Area.Left ||
                mCanvas.Size.Height != Area.Bottom - Area.Top)
            {
                Utility.Dispose(ref mCanvas);

                mCanvas = new Image(
                    new Size<int>(Area.Right - Area.Left, Area.Bottom - Area.Top),
                    PixelFormat.RedBlueGreenAlpha8bit,
                    mRender.GpuDevice);
            }
        }

        protected override void Present(PresentRender render)
        {
            render.Draw(mCanvas, Area, 1.0f);
        }

        protected override void Excute(List<GameObject> passedGameObjectList)
        {
            mRender.BeginDraw(mCanvas, BezierRender.RenderMode.Draw);
            mRender.Clear(mCanvas, new Color<float>(1, 1, 1, 0));

            foreach (var gameObject in passedGameObjectList)
            {
                var component = gameObject.GetComponent<BezierComponent>();

                mRender.DrawBezier(component.Controls, component.Colors[0], 2.0f);
            }

            mRender.EndDraw();
        }

    }
}
