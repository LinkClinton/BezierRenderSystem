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
    public class BezierFillSystem : BehaviorSystem
    {
        private Image mCanvas;
        private BezierRender mRender;

        public Rectangle<int> Area { get; set; }

        public BezierFillSystem(BezierRender render, Rectangle<int> area) : base("BezierFillSystem")
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
            mRender.BeginDraw(mCanvas, BezierRender.RenderMode.Fill);
            mRender.Clear(mCanvas, new Color<float>(1, 1, 1, 1));

            Position<float>[] controls = new Position<float>[passedGameObjectList.Count * 3];
            Color<float>[] colors = new Color<float>[passedGameObjectList.Count * 3];

            for (int i = 0; i < passedGameObjectList.Count; i++)
            {
                var component = passedGameObjectList[i].GetComponent<BezierComponent>();

                controls[i * 3 + 0] = component.Controls[0];
                controls[i * 3 + 1] = component.Controls[1];
                controls[i * 3 + 2] = component.Controls[2];

                colors[i * 3 + 0] = component.Colors[0];
                colors[i * 3 + 1] = component.Colors[1];
                colors[i * 3 + 2] = component.Colors[2];
            }

            mRender.FillBeziers(passedGameObjectList.Count, controls, colors, null);

            /*foreach (var x in passedGameObjectList)
            {
                var component = x.GetComponent<BezierComponent>();

                mRender.FillBezier(component.Controls, component.Colors);
            }*/

            mRender.EndDraw();
        }
    
    }
}
