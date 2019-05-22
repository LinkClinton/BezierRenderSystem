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
        private struct TrianglePoints
        {
            public Vector4 Position0;
            public Vector4 Position1;
            public Vector4 Position2;
        }

        private struct TriangleColors
        {
            public Vector4 Color0;
            public Vector4 Color1;
            public Vector4 Color2;
        }

        private GpuVertexShader mFillBeziersVertexShader;
        private GpuVertexShader mFillBezierVertexShader;

        private GpuPixelShader mFillBeziersPixelShader;
        private GpuPixelShader mFillBezierPixelShader;

        private GpuBuffer mFillVertexBuffer;
        private GpuBuffer mFillIndexBuffer;

        private GpuBuffer mTrianglePointsBuffer;
        private GpuBuffer mTriangleColorsBuffer;

        private GpuBufferArray mTrianglePointsBufferArray;
        private GpuBufferArray mTriangleColorsBufferArray;
        private GpuBufferArray mTrianglePointsCanvasBufferArray;

        private GpuResourceUsage mTrianglePointsBufferArrayUsage;
        private GpuResourceUsage mTriangleColorsBufferArrayUsage;
        private GpuResourceUsage mTrianglePointsCanvasBufferArrayUsage;

		private void InitializeFillComponent()
        {
            //compile vertex and pixel shader to render bezier curve
            mFillBeziersVertexShader = new GpuVertexShader(mDevice, GpuVertexShader.Compile(Properties.Resources.FillBeziersShader, "vs_main"));
            mFillBezierVertexShader = new GpuVertexShader(mDevice, GpuVertexShader.Compile(Properties.Resources.FillBezierShader, "vs_main"));

            mFillBeziersPixelShader = new GpuPixelShader(mDevice, GpuPixelShader.Compile(Properties.Resources.FillBeziersShader, "ps_main"));
            mFillBezierPixelShader = new GpuPixelShader(mDevice, GpuPixelShader.Compile(Properties.Resources.FillBezierShader, "ps_main"));

            //init vertex and index buffer
            //vertex data will be made when we draw bezier curve
            mFillVertexBuffer = new GpuBuffer(
                Utility.SizeOf<Vertex>() * 3,
                Utility.SizeOf<Vertex>() * 1,
                mDevice,
                GpuResourceInfo.VertexBuffer());

            mFillIndexBuffer = new GpuBuffer(
                Utility.SizeOf<uint>() * 3,
                Utility.SizeOf<uint>() * 1,
                mDevice,
                GpuResourceInfo.IndexBuffer());

            uint[] indices = new uint[] { 0, 1, 2 };

            mFillIndexBuffer.Update(indices);

            mTrianglePointsBuffer = new GpuBuffer(
                Utility.SizeOf<TrianglePoints>(),
                Utility.SizeOf<TrianglePoints>(),
                mDevice,
                GpuResourceInfo.ConstantBuffer());

            mTriangleColorsBuffer = new GpuBuffer(
                Utility.SizeOf<TriangleColors>(),
                Utility.SizeOf<TriangleColors>(),
                mDevice,
                GpuResourceInfo.ConstantBuffer());

        }
    }
}
