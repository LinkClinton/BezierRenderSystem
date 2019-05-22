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

        public enum RenderMode
        {
            Fill,
            Draw
        }

        private readonly GpuDevice mDevice;

        private readonly GpuRasterizerState mRasterizerState;
        private readonly GpuInputLayout mInputLayout;
        private readonly GpuBlendState mBlendState;

        private readonly GpuBuffer mTransformBuffer;

        private Matrix4x4 mProjection;
        private Size<int> mCanvasSize;

        public Matrix4x4 Transform { get; set; }

        public bool MSAAStatus { get; }

        public RenderMode Mode { get; private set; }

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

            //initalize render component
            InitializeFillComponent();
            InitializeDrawComponent();

            //init input layout
            //Position : float3
            //Texcoord : float2
            //Color    : float4
            mInputLayout = new GpuInputLayout(mDevice, new InputElement[]
            {
                new InputElement("POSITION", 0, 12),
                new InputElement("TEXCOORD", 0, 8),
                new InputElement("COLOR", 0, 16)
            }, mFillBezierVertexShader);

            //init constant buffer
            mTransformBuffer = new GpuBuffer(
                Utility.SizeOf<TransformMatrix>(),
                Utility.SizeOf<TransformMatrix>(),
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

        public void BeginDraw(Image image, RenderMode mode)
        {
            //begin draw and we need set the render target before we draw anything
            Mode = mode;

            //reset device and set render target
            mDevice.Reset();
            mDevice.SetRenderTarget(image);

            //set blend and raster state and input layout
            mDevice.SetBlendState(mBlendState);
            mDevice.SetInputLayout(mInputLayout);
            mDevice.SetRasterizerState(mRasterizerState);

            //set vertex shader, pixel shader
            switch (Mode)
            {
                case RenderMode.Fill: 
                    mDevice.SetPixelShader(mFillBezierPixelShader);
                    mDevice.SetVertexShader(mFillBezierVertexShader);
                    break;
                case RenderMode.Draw:
                    mDevice.SetPixelShader(mDrawBezierPixelShader);
                    mDevice.SetVertexShader(mDrawBezierVertexShader);
                    break;
                default: throw new Exception("Mode Not Supported.");
            }

            //set primitive type
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

        public void FillBezier(Position<float>[] controls, Color<float>[] colors)
        {
            Utility.Assert(controls.Length >= 3 && colors.Length >= 3);
            Utility.Assert(Mode == RenderMode.Fill);

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

            trianglePoints.Position0.Y = mCanvasSize.Height - trianglePoints.Position0.Y;
            trianglePoints.Position1.Y = mCanvasSize.Height - trianglePoints.Position1.Y;
            trianglePoints.Position2.Y = mCanvasSize.Height - trianglePoints.Position2.Y;
            
            //update data to gpu
            mFillVertexBuffer.Update(vertices);
            mTransformBuffer.Update(transform);
            mTrianglePointsBuffer.Update(trianglePoints);
            mTriangleColorsBuffer.Update(triangleColors);

            mDevice.SetVertexBuffer(mFillVertexBuffer);
            mDevice.SetIndexBuffer(mFillIndexBuffer);
            mDevice.SetBuffer(mTransformBuffer, 0);
            mDevice.SetBuffer(mTrianglePointsBuffer, 1);
            mDevice.SetBuffer(mTriangleColorsBuffer, 2);

            mDevice.DrawIndexed(3);
        }

        public void DrawBezier(Position<float>[] controls, Color<float> color, float width = 2.0f)
        {
            Utility.Assert(controls.Length >= 3);
            Utility.Assert(Mode == RenderMode.Draw);

            //convert Position<T> to Vector2
            Vector2[] points = new Vector2[]
            {
                new Vector2(controls[0].X, controls[0].Y),
                new Vector2(controls[1].X, controls[1].Y),
                new Vector2(controls[2].X, controls[2].Y)
            };

            //get bounding box(before transform) of bezier curve
            var box = QuadraticBezierCurve.BoundingBox(points, width);

            var vertices = new Vertex[]
            {
                new Vertex() { Position = new Vector3(box.Min.X, box.Min.Y, 0) },
                new Vertex() { Position = new Vector3(box.Min.X, box.Max.Y, 0) },
                new Vertex() { Position = new Vector3(box.Max.X, box.Max.Y, 0) },
                new Vertex() { Position = new Vector3(box.Max.X, box.Min.Y, 0) }
            };

            for (int i = 0; i < 4; i++)
            {
                vertices[i].Color = new Vector4(color.Red, color.Green, color.Blue, color.Alpha);
                vertices[i].TexCoord = new Vector2(width);
            }

            //we need transform the control points to screen space
            var transformMatrix = Transform * mProjection;
            var size = new Vector2(mCanvasSize.Width, mCanvasSize.Height) * 0.5f;

            //transform points
            for (int i = 0; i < 3; i++)
            {
                points[i] = (Vector2.Transform(points[i], transformMatrix) + Vector2.One) * size;
                points[i].Y = mCanvasSize.Height - points[i].Y;
            }

            //get parameter format of bezier curve
            //Q(t) = At^2 + B^t + C
            var parameters = QuadraticBezierCurve.ParameterFormat(points);
            var A = parameters[0];
            var B = parameters[1];
            var C = parameters[2];

            //For point P, when any root of equation(Q'(s) * (P - Q(s)) = 0) is legal, the P is inside the curve
            //The equation full-format is (-2A^2)t^3 + (-3AB)t^2 + (2AP - 2AC - B^2)t + B(P - C) = 0
            //we can use Cardano formula to solve it in pixel shader.
            //but we can cache some calculation for opt.
            var c0 = -2 * Vector2.Dot(A, A);
            var c1 = -3 * Vector2.Dot(A, B);
            var c2 = -2 * Vector2.Dot(A, C) - Vector2.Dot(B, B);
            var c3 = -Vector2.Dot(B, C);

            var equation = new Equation()
            {
                Coefficient0 = new Vector4(c0, c1, c2, c3),
                Coefficient1 = new Vector4(A.X, A.Y, B.X, B.Y),
                Coefficient2 = new Vector4(C.X, C.Y, width * 0.5f, width * 0.5f)
            };

            mTransformBuffer.Update(new TransformMatrix()
            {
                World = Transform,
                Projection = mProjection
            });

            mDrawBezierVertexBuffer.Update(vertices);
            mEquationBuffer.Update(equation);

            mDevice.SetVertexBuffer(mDrawBezierVertexBuffer);
            mDevice.SetIndexBuffer(mDrawBezierIndexBuffer);
            mDevice.SetBuffer(mTransformBuffer, 0);
            mDevice.SetBuffer(mEquationBuffer, 1);

            mDevice.DrawIndexed(6);
        }

        public void FillBeziers(int count, Position<float>[] controls, Color<float>[] colors, Matrix4x4[] transforms)
        {
            Utility.Assert(controls.Length >= count * 3 && colors.Length >= count * 3);
            Utility.Assert(Mode == RenderMode.Fill);

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
            mDevice.SetVertexShader(mFillBeziersVertexShader);
            mDevice.SetPixelShader(mFillBeziersPixelShader);

            //set vertex buffer
            mDevice.SetVertexBuffer(mFillVertexBuffer);
            mDevice.SetIndexBuffer(mFillIndexBuffer);

            //set constant buffer and resource
            mDevice.SetResourceUsage(mTrianglePointsBufferArrayUsage, 0);
            mDevice.SetResourceUsage(mTriangleColorsBufferArrayUsage, 1);
            mDevice.SetResourceUsage(mTrianglePointsCanvasBufferArrayUsage, 2);

            //drwa indexed instanced
            mDevice.DrawIndexedInstanced(3, count);

            //reset the shader
            mDevice.SetVertexShader(mFillBezierVertexShader);
            mDevice.SetPixelShader(mFillBezierPixelShader);
        }
    }
}
