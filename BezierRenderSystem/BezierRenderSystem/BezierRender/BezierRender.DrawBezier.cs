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
        private GpuVertexShader mDrawBeziersVertexShader;

        private GpuPixelShader mDrawBezierPixelShader;
        private GpuPixelShader mDrawBeziersPixelShader;

        private GpuBuffer mDrawVertexBuffer;
        private GpuBuffer mDrawIndexBuffer;
        
        private GpuBuffer mEquationBuffer;

        private GpuBufferArray mColorBufferArray;
        private GpuBufferArray mEquationBufferArray;
        private GpuBufferArray mTransformBufferArray;

        private GpuResourceUsage mColorBufferArrayUsage;
        private GpuResourceUsage mEquationBufferArrayUsage;
        private GpuResourceUsage mTransformBufferArrayUsage;

        private void InitializeDrawComponent()
        {
            mDrawBeziersVertexShader = new GpuVertexShader(mDevice, GpuVertexShader.Compile(Properties.Resources.DrawBeziersShader, "vs_main"));
            mDrawBezierVertexShader = new GpuVertexShader(mDevice, GpuVertexShader.Compile(Properties.Resources.DrawBezierShader, "vs_main"));

            mDrawBeziersPixelShader = new GpuPixelShader(mDevice, GpuPixelShader.Compile(Properties.Resources.DrawBeziersShader, "ps_main"));
            mDrawBezierPixelShader = new GpuPixelShader(mDevice, GpuPixelShader.Compile(Properties.Resources.DrawBezierShader, "ps_main"));

            mEquationBuffer = new GpuBuffer(
                Utility.SizeOf<Equation>(),
                Utility.SizeOf<Equation>(),
                mDevice,
                GpuResourceInfo.ConstantBuffer());

            mDrawVertexBuffer = new GpuBuffer(
                Utility.SizeOf<Vertex>() * 4,
                Utility.SizeOf<Vertex>(),
                mDevice,
                GpuResourceInfo.VertexBuffer());

            mDrawIndexBuffer = new GpuBuffer(
                Utility.SizeOf<uint>() * 6,
                Utility.SizeOf<uint>(),
                mDevice,
                GpuResourceInfo.IndexBuffer());

            var indices = new uint[]
            {
                0, 1, 2,
                0, 2, 3
            };

            mDrawIndexBuffer.Update(indices);
        }
    }
}
