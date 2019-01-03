using CGA;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DirectInput;
using SlimDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using Vector4 = SlimDX.Vector4;
using Vector3 = SlimDX.Vector3;
using Vector2 = SlimDX.Vector2;

namespace CGARenderer
{
    public class CGARendererApp : BaseApp, IDisposable
    {
        // is used to specify how we want to draw the cube.
        private enum ShadingModel
        {
            Lambert,
            BlinnPhong
        }

        // struct stores data for a single vertex.
        private struct Vertex
        {
            public Vector4 Position;   // The position of the vertex in 3D space.
            public Vector3 Normal;
            public Color4 Color;       // The color to use for vertex when we are not using textured mode.
            public Vector2 TexCoord;   // The textures coordinates for vertex.  We need these when we are using textured mode.
        }

        private const float _MoveSpeed = 0.01f;     // Sets the speed you move around at.
        private static readonly ShadingModel _ShadingModel = ShadingModel.Lambert;
        private const float _ZNear = 0.5f;
        private const float _Zfar = 1000.0f;
        private const int _VertexSize = 52;
        private const int _MatricesBufferSize = 64 * 3 + 16;
        private static readonly Vector3 _CameraTarget = new Vector3(0, 0, 0);
        private static readonly Vector3 _CameraUp = new Vector3(0, 1, 0);
        private readonly IEnumerable<Shape> _shapes;
        private Device _device; // The Direct3D device.
        private DeviceContext _deviceContext; // is just a convenience member.  It holds the context for the Direct3D device.
        private RenderTargetView _renderTargetView; // Our render target.
        private SwapChain _swapChain; // Our swap chain.
        private Viewport _viewport; // The viewport.
        private InputLayout _inputLayout;  // Tells Direct3D about the vertex format we are using.
        private VertexShader _vertexShader; // is the vertex shader.
        private ShaderSignature _vertexShaderSignature; // The vertex shader signature.
        private PixelShader _pixelShader; // is the pixel shader.
        private Buffer _matricesBuffer;
        private DataStream _matricesBufferDataStream;
        private Matrix _view;  // is our view matrix.
        private Matrix _projection;  // The projection matrix.
        private Texture2D _depthStencilTexture = null;     // Holds the depth stencil texture.
        private DepthStencilView _depthStencilView = null; // The depth stencil view object.
        private Buffer _boxVertexBuffer;
        private Buffer _quadVertexBuffer;
        private static Vector3 _cameraPosition = new Vector3(0, 2, -5); // The position of our camera in 3D space.
        private float _rotationAngle = 0.005f; // The current rotation amount for the cube on the Y axis.

        public CGARendererApp(string title, int width, int height, bool fullscreen, IEnumerable<Shape> shapes)
            : base(title, width, height, fullscreen)
        {
            _shapes = shapes;
            InitD3D();
            InitShaders();
            InitScene();
            InitDepthStencil();
            InitConstantBuffers();
        }

        public void InitD3D()
        {
            // Setup the configuration for the SwapChain.
            var swapChainDesc = new SwapChainDescription()
            {
                BufferCount = 2, // 2 back buffers (a.k.a. Triple Buffering).
                Usage = Usage.RenderTargetOutput,
                OutputHandle = FormObject.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            // Create the SwapChain and check for errors.
            if (Device.CreateWithSwapChain(DriverType.Hardware,
                DeviceCreationFlags.Debug,
                new FeatureLevel[] { FeatureLevel.Level_11_0 },
                swapChainDesc,
                out _device,
                out _swapChain).IsFailure)
            {
                // An error has occurred.  Initialization of the Direct3D device has failed for some reason.
                return;
            }

            // Create a view of our render target, which is the backbuffer of the swap chain we just created
            using (var resource = Resource.FromSwapChain<Texture2D>(_swapChain, 0))
            {
                _renderTargetView = new RenderTargetView(_device, resource);
            };

            // Get the device context and store it in our _DeviceContext member variable.
            _deviceContext = _device.ImmediateContext;

            // Setting a viewport is required if you want to actually see anything
            _viewport = new Viewport(0.0f,
                0.0f,
                FormObject.Width,
                FormObject.Height,
                0.0f,
                1.0f);

            _deviceContext.Rasterizer.SetViewports(_viewport);
            _deviceContext.OutputMerger.SetTargets(_renderTargetView);

            // Prevent DXGI handling of Alt+Enter since it does not work properly with Winforms
            using (var factory = _swapChain.GetParent<Factory>())
            {
                factory.SetWindowAssociation(FormObject.Handle, WindowAssociationFlags.IgnoreAltEnter);
            };
        }

        public void InitConstantBuffers()
        {
            var bufferDescription = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                SizeInBytes = _MatricesBufferSize
            };
            _matricesBuffer = new Buffer(_device, bufferDescription);
            _deviceContext.VertexShader.SetConstantBuffer(_matricesBuffer, 1);
            _matricesBufferDataStream = new DataStream(_MatricesBufferSize, true, true)
            {
                Position = 0
            };
        }

        public void InitShaders()
        {
            string compileError;

            // Load and compile the vertex shader
            using (var byteCode = ShaderBytecode.CompileFromFile("Effects.fx",
                "Vertex_Shader",
                "vs_4_0",
                ShaderFlags.Debug,
                SlimDX.D3DCompiler.EffectFlags.None,
                null,
                null,
                out compileError))
            {
                _vertexShaderSignature = ShaderSignature.GetInputSignature(byteCode);
                _vertexShader = new VertexShader(_device, byteCode);
            }

            // Load and compile the pixel shader
            var pixelShaderName = "";
            if (_ShadingModel == ShadingModel.Lambert)
                pixelShaderName = "Pixel_Shader_Lambert";
            else if (_ShadingModel == ShadingModel.BlinnPhong)
                pixelShaderName = "Pixel_Shader_BlinnPhong";
            else
                throw new Exception($"Unknown {nameof(ShadingModel)}");

            using (var byteCode = ShaderBytecode.CompileFromFile("Effects.fx",
                pixelShaderName,
                "ps_4_0",
                ShaderFlags.Debug,
                SlimDX.D3DCompiler.EffectFlags.None,
                null,
                null,
                out compileError))
            {
                _pixelShader = new PixelShader(_device, byteCode);
            }

            // Set the shaders.
            _deviceContext.VertexShader.Set(_vertexShader);
            _deviceContext.PixelShader.Set(_pixelShader);
        }

        private Buffer CreateVertexBuffer(IEnumerable<Vertex> vertices)
        {
            using (var dataStream = new DataStream(_VertexSize * vertices.Count(), true, true))
            {
                dataStream.Position = 0;
                foreach (var vertex in vertices)
                {
                    dataStream.Write(vertex);
                }
                dataStream.Position = 0;
                var bufferDescription = new BufferDescription
                {
                    Usage = ResourceUsage.Default,
                    SizeInBytes = _VertexSize * vertices.Count(),
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = _VertexSize
                };
                return new Buffer(_device, dataStream, bufferDescription);
            }
        }

        public void InitScene()
        {
            // Create our projection matrix.
            _projection = Matrix.PerspectiveFovLH((float)Math.PI * 0.5f, // is 90 degrees in radians
                FormObject.Width / (float)FormObject.Height,
                _ZNear,
                _Zfar);

            // Create our view matrix.
            _view = Matrix.LookAtLH(_cameraPosition, _CameraTarget, _CameraUp);

            _boxVertexBuffer = CreateVertexBuffer(new[]
            {
                // Bottom face
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },

                // Front face
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(0.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                
                // Right face
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 1) },

                // Back face
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },

                // Left face
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 1) },

                // Top face
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
            });

            _quadVertexBuffer = CreateVertexBuffer(new[]
            {
                // Back face
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
            });

            // Define the vertex format.
            // tells Direct3D what information we are storing for each vertex, and how it is stored.
            _inputLayout = new InputLayout(_device, _vertexShaderSignature, new InputElement[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL",   0, Format.R32G32B32_Float,    InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR",    0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float,       InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0)
            });
        }

        public void InitDepthStencil()
        {
            // Create the depth stencil texture description
            var depthStencilTextureDesc = new Texture2DDescription
            {
                Width = FormObject.ClientSize.Width,
                Height = FormObject.ClientSize.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            // Create the Depth Stencil View description
            var depthStencilViewDesc = new DepthStencilViewDescription
            {
                Format = depthStencilTextureDesc.Format,
                Dimension = DepthStencilViewDimension.Texture2D,
                MipSlice = 0
            };

            // Create the depth stencil texture.
            _depthStencilTexture = new Texture2D(_device, depthStencilTextureDesc);

            // Create the DepthStencilView object.
            _depthStencilView = new DepthStencilView(_device, _depthStencilTexture, depthStencilViewDesc);

            // Make the DepthStencilView active.
            _deviceContext.OutputMerger.SetTargets(_depthStencilView, _renderTargetView);
        }

        public override void UpdateScene(double frameTime)
        {
            base.UpdateScene(frameTime);

            // Keep the cube rotating by increasing its rotation amount
            _rotationAngle += 0.00025f;
            if (_rotationAngle > (2.0f * Math.PI))
            {
                _rotationAngle = 0.0f;
            }

            // Check for user input.

            // If the player pressed forward.
            if (UserInput.IsKeyPressed(Key.UpArrow) || UserInput.IsKeyPressed(Key.W))
            {
                _cameraPosition.Z = _cameraPosition.Z + _MoveSpeed;
            }

            // If the player pressed back.
            if (UserInput.IsKeyPressed(Key.DownArrow) || UserInput.IsKeyPressed(Key.S))
            {
                _cameraPosition.Z = _cameraPosition.Z - _MoveSpeed;
            }

            // Update the view matrix.
            _view = Matrix.LookAtLH(_cameraPosition, new Vector3(0, 0, 0), new Vector3(0, 1, 0));
        }

        private void UpdateMatrices(Matrix model)
        {
            var modelView = model * _view;
            var modelViewProjection = model * _view * _projection;

            _matricesBufferDataStream.Position = 0;
            _matricesBufferDataStream.Write(Matrix.Transpose(model));
            _matricesBufferDataStream.Write(Matrix.Invert(modelView));
            _matricesBufferDataStream.Write(Matrix.Transpose(modelViewProjection));
            _matricesBufferDataStream.Write(_cameraPosition);
            _matricesBufferDataStream.Position = 0;
            _device.ImmediateContext.UpdateSubresource(new DataBox(0, 0, _matricesBufferDataStream), _matricesBuffer, 0);
        }

        private void DrawBox(Matrix model)
        {
            UpdateMatrices(model);
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_boxVertexBuffer, _VertexSize, 0));
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _deviceContext.Draw(36, 0);
        }

        private void DrawQuad(Matrix model)
        {
            UpdateMatrices(model);
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadVertexBuffer, _VertexSize, 0));
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _deviceContext.Draw(6, 0);
        }

        public override void RenderScene()
        {
            if ((!IsInitialized) || IsDisposed)
            {
                return;
            }

            // Clear the screen before we draw the next frame.
            _deviceContext.ClearRenderTargetView(_renderTargetView, ClearColor);
            _deviceContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            foreach (var shape in _shapes)
            {
                var model = ToSlimDXMatrix(shape.Transform);
                if (shape.GetType() == typeof(Box))
                {
                    DrawBox(model);
                }
                else if (shape.GetType() == typeof(Quad))
                {
                    DrawQuad(model);
                }
            }

            // Present the frame we just rendered to the user.
            _swapChain.Present(0, PresentFlags.None);
        }

        private Matrix ToSlimDXMatrix(Matrix4x4 matrix)
        {
            return new Matrix() {
                M11 = matrix.M11, M12 = matrix.M12, M13 = matrix.M13, M14 = matrix.M14,
                M21 = matrix.M21, M22 = matrix.M22, M23 = matrix.M23, M24 = matrix.M24,
                M31 = matrix.M31, M32 = matrix.M32, M33 = matrix.M33, M34 = matrix.M34,
                M41 = matrix.M41, M42 = matrix.M42, M43 = matrix.M43, M44 = matrix.M44,
            };
        }

        public override void ToggleFullscreen()
        {
            base.ToggleFullscreen();
            _swapChain.IsFullScreen = IsFullscreen;
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                /*
                * The following text is from MSDN  (http://msdn.microsoft.com/en-us/library/fs2xkftw%28VS.80%29.aspx)
                * 
                * Dispose(bool disposing) executes in two distinct scenarios:
                * If disposing equals true, the method has been called directly or indirectly by a user's code and managed and unmanaged resources can be disposed.
                * If disposing equals false, the method has been called by the runtime from inside the finalizer and only unmanaged resources can be disposed. 
                * When an object is executing its finalization code, it should not reference other objects, because finalizers do not execute in any particular order. 
                * If an executing finalizer references another object that has already been finalized, the executing finalizer will fail.
                */
                if (disposing)
                {
                    // Unregister events

                    // get rid of managed resources
                    if (_vertexShader != null)
                        _vertexShader.Dispose();

                    if (_pixelShader != null)
                        _pixelShader.Dispose();

                    if (_boxVertexBuffer != null)
                        _boxVertexBuffer.Dispose();

                    if (_swapChain != null)
                        _swapChain.Dispose();

                    if (_renderTargetView != null)
                        _renderTargetView.Dispose();

                    if (_inputLayout != null)
                        _inputLayout.Dispose();

                    if (_matricesBuffer != null)
                        _matricesBuffer.Dispose();

                    if (_matricesBufferDataStream != null)
                        _matricesBufferDataStream.Dispose();

                    if (_depthStencilTexture != null)
                        _depthStencilTexture.Dispose();

                    if (_depthStencilView != null)
                        _depthStencilView.Dispose();

                    if (_device != null)
                        _device.Dispose();

                    if (_deviceContext != null)
                        _deviceContext.Dispose();
                }
                // get rid of unmanaged resources
            }
            base.Dispose(disposing);
        }
    }
}
