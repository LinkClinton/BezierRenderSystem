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
    class BezierRender
    {
        private struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
            public Vector4 Color;
        }

        private struct TransformMatrix
        {
            public Matrix4x4 World;
            public Matrix4x4 Projection;
        }

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

        private readonly GpuDevice mDevice;

        private readonly GpuRasterizerState mRasterizerState;
        private readonly GpuInputLayout mInputLayout;
        private readonly GpuBlendState mBlendState;


        private readonly GpuVertexShader mBeziersVertexShader;
        private readonly GpuVertexShader mVertexShader;

        private readonly GpuPixelShader mBeziersPixelShader;
        private readonly GpuPixelShader mPixelShader;

        private readonly GpuBuffer mVertexBuffer;
        private readonly GpuBuffer mIndexBuffer;

        private readonly GpuBuffer mTransformBuffer;

        private readonly GpuBuffer mTrianglePointsBuffer;
        private readonly GpuBuffer mTriangleColorsBuffer;

        private GpuBufferArray mTrianglePointsBufferArray;
        private GpuBufferArray mTriangleColorsBufferArray;
        private GpuBufferArray mTrianglePointsCanvasBufferArray;

        private GpuResourceUsage mTrianglePointsBufferArrayUsage;
        private GpuResourceUsage mTriangleColorsBufferArrayUsage;
        private GpuResourceUsage mTrianglePointsCanvasBufferArrayUsage;

        private Matrix4x4 mProjection;
        private Size<int> mCanvasSize;

        public Matrix4x4 Transform { get; set; }

        public bool MSAAStatus { get; }

        public GpuDevice GpuDevice => mDevice;

        public BezierRender(GpuDevice device)
        {
            //bezier render is a simple render for rendering quadratic bezier curve
            mDevice = device;

            //default transform is I
            Transform = Matrix4x4.Identity;

            //init blend state
            mBlendState = new GpuBlendState(mDevice, new RenderTargetBlendDescription()
            {
                AlphaBlendOperation = GpuBlendOperation.Add,
                BlendOperation = GpuBlendOperation.Add,
                DestinationAlphaBlend = GpuBlendOption.InverseSourceAlpha,
                DestinationBlend = GpuBlendOption.InverseSourceAlpha,
                SourceAlphaBlend = GpuBlendOption.SourceAlpha,
                SourceBlend = GpuBlendOption.SourceAlpha,
                IsBlendEnable = true
            });

            //init rasterizer state
            mRasterizerState = new GpuRasterizerState(mDevice, GpuFillMode.Solid, GpuCullMode.None);

            //compile vertex and pixel shader to render bezier curve
            mBeziersVertexShader = new GpuVertexShader(mDevice, GpuVertexShader.Compile(Properties.Resources.BeziersRenderShader, "vs_main"));
            mVertexShader = new GpuVertexShader(mDevice, GpuVertexShader.Compile(Properties.Resources.BezierRenderShader, "vs_main"));

            mBeziersPixelShader = new GpuPixelShader(mDevice, GpuPixelShader.Compile(Properties.Resources.BeziersRenderShader, "ps_main"));
            mPixelShader = new GpuPixelShader(mDevice, GpuPixelShader.Compile(Properties.Resources.BezierRenderShader, "ps_main"));

            //init input layout
            //Position : float3
            //Texcoord : float2
            //Color    : float4
            mInputLayout = new GpuInputLayout(mDevice, new InputElement[]
            {
                new InputElement("POSITION", 0, 12),
                new InputElement("TEXCOORD", 0, 8),
                new InputElement("COLOR", 0, 16)
            }, mVertexShader);

            //init vertex and index buffer
            //vertex data will be made when we draw bezier curve
            mVertexBuffer = new GpuBuffer(
                Utility.SizeOf<Vertex>() * 3,
                Utility.SizeOf<Vertex>() * 1,
                mDevice,
                GpuResourceInfo.VertexBuffer());

            mIndexBuffer = new GpuBuffer(
                Utility.SizeOf<uint>() * 3,
                Utility.SizeOf<uint>() * 1,
                mDevice,
                GpuResourceInfo.IndexBuffer());

            uint[] indices = new uint[] { 0, 1, 2 };

            mIndexBuffer.Update(indices);

            //init constant buffer
            mTransformBuffer = new GpuBuffer(
                Utility.SizeOf<TransformMatrix>(),
                Utility.SizeOf<TransformMatrix>(),
                mDevice,
                GpuResourceInfo.ConstantBuffer());

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

            MSAAStatus = true;
        }

        public void Clear(Image texture, Color<float> clear)
        {
            mDevice.ClearRenderTarget(
                renderTarget: texture,
                color: new Vector4<float>(x: clear.Red, y: clear.Green, z: clear.Blue, w: clear.Alpha));
        }

        public void BeginDraw(Image image)
        {
            //begin draw and we need set the render target before we draw anything

            //reset device and set render target
            mDevice.Reset();
            mDevice.SetRenderTarget(image);

            //set blend state
            mDevice.SetBlendState(mBlendState);
            mDevice.SetRasterizerState(mRasterizerState);

            //set input layout ,vertex shader, pixel shader and primitive type
            mDevice.SetPixelShader(mPixelShader);
            mDevice.SetInputLayout(mInputLayout);
            mDevice.SetVertexShader(mVertexShader);
            mDevice.SetPrimitiveType(GpuPrimitiveType.TriangleList);

            //set view port
            mDevice.SetViewPort(new Rectangle<float>(0, 0, image.Size.Width, image.Size.Height));

            //set the project matrix, need set null when we end draw
            mProjection = Matrix4x4.CreateOrthographicOffCenter(
                0, image.Size.Width,
                0, image.Size.Height, 0, 1);

            //set the canvas size
            mCanvasSize = image.Size;
        }

        public void EndDraw()
        {
            mProjection = Matrix4x4.Identity;
            mCanvasSize = null;
        }

        public void DrawBezier(Position<float>[] controls, Color<float>[] colors)
        {
            Utility.Assert(controls.Length >= 3 && colors.Length >= 3);

            Vertex[] vertices = new Vertex[3];

            for (int i = 0; i < 3; i++)
            {
                vertices[i] = new Vertex()
                {
                    Position = new Vector3(controls[i].X, controls[i].Y, 0),
                    Color = new Vector4(colors[i].Red, colors[i].Green, colors[i].Blue, colors[i].Alpha)
                };
            }

            vertices[0].TexCoord = new Vector2(0, 0);
            vertices[1].TexCoord = new Vector2(0.5f, 0);
            vertices[2].TexCoord = new Vector2(1, 1);

            TransformMatrix transform = new TransformMatrix()
            {
                World = Transform,
                Projection = mProjection
            };

            var transformMatrix = transform.World * transform.Projection;

            TrianglePoints trianglePoints = new TrianglePoints()
            {
                Position0 = Vector4.Transform(vertices[0].Position, transformMatrix),
                Position1 = Vector4.Transform(vertices[1].Position, transformMatrix),
                Position2 = Vector4.Transform(vertices[2].Position, transformMatrix),
            };

            TriangleColors triangleColors = new TriangleColors()
            {
                Color0 = vertices[0].Color,
                Color1 = vertices[1].Color,
                Color2 = vertices[2].Color
            };

            var size = new Vector4(mCanvasSize.Width * 0.5f, mCanvasSize.Height * 0.5f, 0, 0);

            trianglePoints.Position0 = (trianglePoints.Position0 + new Vector4(1)) * size;
            trianglePoints.Position1 = (trianglePoints.Position1 + new Vector4(1)) * size;
            trianglePoints.Position2 = (trianglePoints.Position2 + new Vector4(1)) * size;

            trianglePoints.Position0.Y = mCanvasSize.Height- trianglePoints.Position0.Y;
            trianglePoints.Position1.Y = mCanvasSize.Height - trianglePoints.Position1.Y;
            trianglePoints.Position2.Y = mCanvasSize.Height - trianglePoints.Position2.Y;
            
            //update data to gpu
            mVertexBuffer.Update(vertices);
            mTransformBuffer.Update(transform);
            mTrianglePointsBuffer.Update(trianglePoints);
            mTriangleColorsBuffer.Update(triangleColors);

            mDevice.SetVertexBuffer(mVertexBuffer);
            mDevice.SetIndexBuffer(mIndexBuffer);
            mDevice.SetBuffer(mTransformBuffer, 0);
            mDevice.SetBuffer(mTrianglePointsBuffer, 1);
            mDevice.SetBuffer(mTriangleColorsBuffer, 2);

            mDevice.DrawIndexed(3);
        }

        public void DrawBeziers(int count, Position<float>[] controls, Color<float>[] colors, Matrix4x4[] transforms)
        {
            Utility.Assert(controls.Length >= count * 3 && colors.Length >= count * 3);

            if (count == 0) return;

            TrianglePoints[] trianglePointsCanvas = new TrianglePoints[count];
            TrianglePoints[] trianglePoints = new TrianglePoints[count];
            TriangleColors[] triangleColors = new TriangleColors[count];

            var size = new Vector4(mCanvasSize.Width * 0.5f, mCanvasSize.Height * 0.5f, 0, 0);

            //create triangle points struct
            for (int index = 0; index < count; index++)
            {
                //get transform matrix
                var transformMatrix = (transforms == null ? Matrix4x4.Identity : transforms[index]) * Transform * mProjection;

                //get control points for bezier_i
                var points = new Position<float>[]
                {
                    controls[index * 3 + 0],
                    controls[index * 3 + 1],
                    controls[index * 3 + 2]
                };

                Vector4 FromColor(Color<float> color) => new Vector4(color.Red, color.Green, color.Blue, color.Alpha);

                //create data for bezier_i in shader
                TrianglePoints subTrianglePoints = new TrianglePoints()
                {
                    Position0 = Vector4.Transform(new Vector3(points[0].X, points[0].Y, 0), transformMatrix),
                    Position1 = Vector4.Transform(new Vector3(points[1].X, points[1].Y, 0), transformMatrix),
                    Position2 = Vector4.Transform(new Vector3(points[2].X, points[2].Y, 0), transformMatrix),
                };

                TriangleColors subTriangleColors = new TriangleColors()
                {
                    Color0 = FromColor(colors[index * 3 + 0]),
                    Color1 = FromColor(colors[index * 3 + 1]),
                    Color2 = FromColor(colors[index * 3 + 2])
                };

                TrianglePoints subTrianglePointsCanvas = new TrianglePoints()
                {
                    Position0 = (subTrianglePoints.Position0 + new Vector4(1)) * size,
                    Position1 = (subTrianglePoints.Position1 + new Vector4(1)) * size,
                    Position2 = (subTrianglePoints.Position2 + new Vector4(1)) * size
                };

                subTrianglePointsCanvas.Position0.Y = mCanvasSize.Height - subTrianglePointsCanvas.Position0.Y;
                subTrianglePointsCanvas.Position1.Y = mCanvasSize.Height - subTrianglePointsCanvas.Position1.Y;
                subTrianglePointsCanvas.Position2.Y = mCanvasSize.Height - subTrianglePointsCanvas.Position2.Y;

                trianglePointsCanvas[index] = subTrianglePointsCanvas;
                trianglePoints[index] = subTrianglePoints;
                triangleColors[index] = subTriangleColors;
            }

            if (mTrianglePointsBufferArray == null || mTrianglePointsBufferArray.ElementCount != count)
            {
                Utility.Dispose(ref mTrianglePointsBufferArray);
                Utility.Dispose(ref mTriangleColorsBufferArray);
                Utility.Dispose(ref mTrianglePointsCanvasBufferArray);

                Utility.Dispose(ref mTrianglePointsBufferArrayUsage);
                Utility.Dispose(ref mTriangleColorsBufferArrayUsage);
                Utility.Dispose(ref mTrianglePointsCanvasBufferArrayUsage);

                mTrianglePointsBufferArray = new GpuBufferArray(
                    Utility.SizeOf<TrianglePoints>(),
                    count,
                    mDevice,
                    GpuResourceInfo.BufferArray());

                mTriangleColorsBufferArray = new GpuBufferArray(
                   Utility.SizeOf<TrianglePoints>(),
                   count,
                   mDevice,
                   GpuResourceInfo.BufferArray());

                mTrianglePointsCanvasBufferArray = new GpuBufferArray(
                   Utility.SizeOf<TrianglePoints>(),
                   count,
                   mDevice,
                   GpuResourceInfo.BufferArray());

                mTrianglePointsBufferArrayUsage = new GpuResourceUsage(mDevice, mTrianglePointsBufferArray);
                mTriangleColorsBufferArrayUsage = new GpuResourceUsage(mDevice, mTriangleColorsBufferArray);
                mTrianglePointsCanvasBufferArrayUsage = new GpuResourceUsage(mDevice, mTrianglePointsCanvasBufferArray);
            }
          
            mTrianglePointsBufferArray.Update(trianglePoints);
            mTriangleColorsBufferArray.Update(triangleColors);
            mTrianglePointsCanvasBufferArray.Update(trianglePointsCanvas);

            //change shader we use
            mDevice.SetVertexShader(mBeziersVertexShader);
            mDevice.SetPixelShader(mBeziersPixelShader);

            //set vertex buffer
            mDevice.SetVertexBuffer(mVertexBuffer);
            mDevice.SetIndexBuffer(mIndexBuffer);

            //set constant buffer and resource
            mDevice.SetResourceUsage(mTrianglePointsBufferArrayUsage, 0);
            mDevice.SetResourceUsage(mTriangleColorsBufferArrayUsage, 1);
            mDevice.SetResourceUsage(mTrianglePointsCanvasBufferArrayUsage, 2);

            //drwa indexed instanced
            mDevice.DrawIndexedInstanced(3, count);

            //reset the shader
            mDevice.SetVertexShader(mVertexShader);
            mDevice.SetPixelShader(mPixelShader);
        }
    }
}
