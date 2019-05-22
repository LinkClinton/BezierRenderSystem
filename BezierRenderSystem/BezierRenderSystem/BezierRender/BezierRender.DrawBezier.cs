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

    public partial class BezierRender
    {
        private struct Equation
        {
            public Vector4 Coefficient0;
            public Vector4 Coefficient1;
            public Vector4 Coefficient2;
        }

        private GpuVertexShader mDrawBezierVertexShader;

        private GpuPixelShader mDrawBezierPixelShader;

        private GpuBuffer mDrawBezierVertexBuffer;
        private GpuBuffer mDrawBezierIndexBuffer;
        
        private GpuBuffer mEquationBuffer;

        private void InitializeDrawComponent()
        {
            mDrawBezierVertexShader = new GpuVertexShader(mDevice, GpuVertexShader.Compile(Properties.Resources.DrawBezierShader, "vs_main"));
            mDrawBezierPixelShader = new GpuPixelShader(mDevice, GpuPixelShader.Compile(Properties.Resources.DrawBezierShader, "ps_main"));

            mEquationBuffer = new GpuBuffer(
                Utility.SizeOf<Equation>(),
                Utility.SizeOf<Equation>(),
                mDevice,
                GpuResourceInfo.ConstantBuffer());

            mDrawBezierVertexBuffer = new GpuBuffer(
                Utility.SizeOf<Vertex>() * 4,
                Utility.SizeOf<Vertex>(),
                mDevice,
                GpuResourceInfo.VertexBuffer());

            mDrawBezierIndexBuffer = new GpuBuffer(
                Utility.SizeOf<uint>() * 6,
                Utility.SizeOf<uint>(),
                mDevice,
                GpuResourceInfo.IndexBuffer());

            var indices = new uint[]
            {
                0, 1, 2,
                0, 2, 3
            };

            mDrawBezierIndexBuffer.Update(indices);
        }
    }
}
