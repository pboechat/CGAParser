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
using Vector2 = SlimDX.Vector2;
using Vector3 = SlimDX.Vector3;
using Vector4 = SlimDX.Vector4;

namespace CGARenderer
{
    public class CGARendererApp : BaseApp, IDisposable
    {
        private enum ShadingModel
        {
            Lambert,
            SolidGreen
        }

        private struct Vertex
        {
            public Vector4 Position;
            public Vector3 Normal;
            public Color4 Color;
            public Vector2 TexCoord;
        }

        private const float _MouseSpeed = 20.0f;
        private const float _MoveSpeed = 10.0f;
        private const float _FoV = (float)Math.PI * 0.5f;
        private const float _ZNear = 0.5f;
        private const float _Zfar = 1000.0f;
        private const int _VertexSize = (4 * 4) + (3 * 4) + (4 * 4) + (2 * 4);
        private const int _MatricesBufferSize = (16 * 4) * 3 + (4 * 4);
        private static readonly Vertex[] _BoxVertices = {
            // Bottom face
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4(0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4(0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
            new Vertex() { Position = new Vector4(0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(0, -1, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },

            // Front face
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(0.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
            new Vertex() { Position = new Vector4(0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 0, -1), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                
            // Right face
            new Vertex() { Position = new Vector4(0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 1) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 0) },
            new Vertex() { Position = new Vector4(0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(1, 0, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 1) },

            // Back face
            new Vertex() { Position = new Vector4(0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },

            // Left face
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 1) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), Normal = new Vector3(-1, 0, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 1) },

            // Top face
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
            new Vertex() { Position = new Vector4(-0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, 0.5f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 0) },
            new Vertex() { Position = new Vector4(0.5f, 0.5f, -0.5f, 1.0f), Normal = new Vector3(0, 1, 0), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
        };
        private static readonly Vertex[] _QuadVertices = {
            new Vertex() { Position = new Vector4( 0.5f, -0.5f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
            new Vertex() { Position = new Vector4( 0.5f,  0.5f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
            new Vertex() { Position = new Vector4( 0.5f,  0.5f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
            new Vertex() { Position = new Vector4(-0.5f,  0.5f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
            new Vertex() { Position = new Vector4(-0.5f, -0.5f,  0.0f, 1.0f), Normal = new Vector3(0, 0, 1), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
        };
        private static readonly Vector3 _StartingEyePosition = new Vector3(0, 0, -2.5f);
        private readonly IEnumerable<Shape> _shapes;
        private ShadingModel _shadingModel = ShadingModel.Lambert;
        private Device _device;
        private DeviceContext _deviceContext;
        private RenderTargetView _renderTargetView;
        private SwapChain _swapChain;
        private Viewport _viewport;
        private RasterizerStateDescription _rasterizerDescription;
        private InputLayout _inputLayout;
        private VertexShader _vertexShader;
        private ShaderSignature _vertexShaderSignature;
        private IDictionary<ShadingModel, PixelShader> _pixelShaders = new Dictionary<ShadingModel, PixelShader>();
        private Buffer _matricesBuffer;
        private DataStream _matricesBufferDataStream;
        private Matrix _view;
        private Matrix _projection;
        private Texture2D _depthStencilTexture = null;
        private DepthStencilView _depthStencilView = null;
        private Buffer _boxVertexBuffer;
        private Buffer _quadVertexBuffer;
        private static Vector3 _eye = _StartingEyePosition;
        private float _theta = 0;
        private float _phi = 0;

        public CGARendererApp(string title, int width, int height, bool fullscreen, IEnumerable<Shape> shapes)
            : base(title, width, height, fullscreen)
        {
            _shapes = shapes;
            InitializeD3D();
            InitializeShaders();
            InitializeScene();
            InitDepthStencil();
            InitConstantBuffers();
        }

        public void InitializeD3D()
        {
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

            if (Device.CreateWithSwapChain(DriverType.Hardware,
                DeviceCreationFlags.Debug,
                new FeatureLevel[] { FeatureLevel.Level_11_0 },
                swapChainDesc,
                out _device,
                out _swapChain).IsFailure)
            {
                throw new Exception("Error creating swap chain");
            }

            using (var resource = Resource.FromSwapChain<Texture2D>(_swapChain, 0))
            {
                _renderTargetView = new RenderTargetView(_device, resource);
            };

            _deviceContext = _device.ImmediateContext;

            _viewport = new Viewport(0.0f,
                0.0f,
                FormObject.Width,
                FormObject.Height,
                0.0f,
                1.0f);

            _rasterizerDescription = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                IsAntialiasedLineEnabled = true,
                IsFrontCounterclockwise = true,
                IsMultisampleEnabled = true,
                IsDepthClipEnabled = true,
                IsScissorEnabled = false
            };

            _deviceContext.Rasterizer.State = RasterizerState.FromDescription(_device, _rasterizerDescription);

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

        public void InitializeShaders()
        {
            string compileError;

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

            foreach (ShadingModel shadingModel in Enum.GetValues(typeof(ShadingModel)))
            {
                using (var byteCode = ShaderBytecode.CompileFromFile("Effects.fx",
                    $"Pixel_Shader_{shadingModel.ToString()}",
                    "ps_4_0",
                    ShaderFlags.Debug,
                    SlimDX.D3DCompiler.EffectFlags.None,
                    null,
                    null,
                    out compileError))
                {
                    _pixelShaders[shadingModel] = new PixelShader(_device, byteCode);
                }
            }
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

        public void InitializeScene()
        {
            var aspect = FormObject.Width / (float)FormObject.Height;
            _projection = Matrix.PerspectiveFovLH(_FoV, aspect, _ZNear, _Zfar);

            _boxVertexBuffer = CreateVertexBuffer(_BoxVertices);
            _quadVertexBuffer = CreateVertexBuffer(_QuadVertices);

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
            var depthStencilViewDesc = new DepthStencilViewDescription
            {
                Format = depthStencilTextureDesc.Format,
                Dimension = DepthStencilViewDimension.Texture2D,
                MipSlice = 0
            };
            _depthStencilTexture = new Texture2D(_device, depthStencilTextureDesc);
            _depthStencilView = new DepthStencilView(_device, _depthStencilTexture, depthStencilViewDesc);
            _deviceContext.OutputMerger.SetTargets(_depthStencilView, _renderTargetView);
        }

        public override void UpdateScene(float deltaTime)
        {
            base.UpdateScene(deltaTime);
            if (Input.IsMouseButtonPressed(1))
            {
                var mousePath = Input.MousePosition() - Input.LastMousePosition();
                _theta += mousePath.X * _MouseSpeed * deltaTime;
                _phi += mousePath.Y * _MouseSpeed * deltaTime;
            }
            var rotation = Matrix.RotationYawPitchRoll(_theta, _phi, 0);
            var right = new Vector3(rotation.M11, rotation.M12, rotation.M13);
            var up = new Vector3(rotation.M21, rotation.M22, rotation.M23);
            var forward = new Vector3(rotation.M31, rotation.M32, rotation.M33);
            if (Input.IsKeyPressed(Key.UpArrow) || Input.IsKeyPressed(Key.W))
            {
                _eye += forward * _MoveSpeed * deltaTime;
            }
            else if (Input.IsKeyPressed(Key.DownArrow) || Input.IsKeyPressed(Key.S))
            {
                _eye -= forward * _MoveSpeed * deltaTime;
            }
            if (Input.IsKeyPressed(Key.RightArrow) || Input.IsKeyPressed(Key.D))
            {
                _eye += right * _MoveSpeed * deltaTime;
            }
            else if (Input.IsKeyPressed(Key.LeftArrow) || Input.IsKeyPressed(Key.A))
            {
                _eye -= right * _MoveSpeed * deltaTime;
            }
            _view = Matrix.LookAtLH(_eye, _eye + forward, up);
            if (Input.IsKeyJustPressed(Key.Space))
            {
                ToggleFillMode();
            }
        }

        private void ToggleFillMode()
        {
            _rasterizerDescription.FillMode = (_rasterizerDescription.FillMode == FillMode.Solid) ? FillMode.Wireframe : FillMode.Solid;
            var fillMode = _rasterizerDescription.FillMode;
            switch (fillMode)
            {
                case FillMode.Solid:
                    _shadingModel = ShadingModel.Lambert;
                    break;
                case FillMode.Wireframe:
                    _shadingModel = ShadingModel.SolidGreen;
                    break;
                default:
                    throw new Exception($"Unknown {nameof(FillMode)}: {fillMode}");
            }
            _deviceContext.Rasterizer.State = RasterizerState.FromDescription(_device, _rasterizerDescription);
        }

        private void UpdateMatrices(Matrix model)
        {
            var modelView = model * _view;
            var inverseModelView = Matrix.Invert(modelView);
            var modelViewProjection = model * _view * _projection;
            _matricesBufferDataStream.Position = 0;
            _matricesBufferDataStream.Write(Matrix.Transpose(model));
            _matricesBufferDataStream.Write(inverseModelView);
            _matricesBufferDataStream.Write(Matrix.Transpose(modelViewProjection));
            _matricesBufferDataStream.Write(_eye);
            _matricesBufferDataStream.Position = 0;
            _device.ImmediateContext.UpdateSubresource(new DataBox(0, 0, _matricesBufferDataStream), _matricesBuffer, 0);
        }

        private void DrawBox(Matrix model)
        {
            UpdateMatrices(model);
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_boxVertexBuffer, _VertexSize, 0));
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _deviceContext.VertexShader.Set(_vertexShader);
            _deviceContext.PixelShader.Set(_pixelShaders[_shadingModel]);
            _deviceContext.Draw(_BoxVertices.Length, 0);
        }

        private void DrawQuad(Matrix model)
        {
            UpdateMatrices(model);
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadVertexBuffer, _VertexSize, 0));
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _deviceContext.VertexShader.Set(_vertexShader);
            _deviceContext.PixelShader.Set(_pixelShaders[_shadingModel]);
            _deviceContext.Draw(_QuadVertices.Length, 0);
        }

        public override void RenderScene()
        {
            if ((!IsInitialized) || IsDisposed)
            {
                return;
            }

            _deviceContext.ClearRenderTargetView(_renderTargetView, ClearColor);
            _deviceContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            foreach (var shape in _shapes)
            {
                var SRT = Matrix4x4.CreateScale(shape.Size) * shape.Transform;
                var model = ToSlimDXMatrix(SRT);
                if (shape.GetType() == typeof(Box))
                {
                    DrawBox(model);
                }
                else if (shape.GetType() == typeof(Quad))
                {
                    DrawQuad(model);
                }
            }

            _swapChain.Present(0, PresentFlags.None);
        }

        private Matrix ToSlimDXMatrix(Matrix4x4 matrix)
        {
            return new Matrix()
            {
                M11 = matrix.M11,
                M12 = matrix.M12,
                M13 = matrix.M13,
                M14 = matrix.M14,
                M21 = matrix.M21,
                M22 = matrix.M22,
                M23 = matrix.M23,
                M24 = matrix.M24,
                M31 = matrix.M31,
                M32 = matrix.M32,
                M33 = matrix.M33,
                M34 = matrix.M34,
                M41 = matrix.M41,
                M42 = matrix.M42,
                M43 = matrix.M43,
                M44 = matrix.M44,
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
                    if (_vertexShader != null)
                    {
                        _vertexShader.Dispose();
                    }

                    foreach (var pixelShader in _pixelShaders.Values)
                    {
                        pixelShader.Dispose();
                    }
                    _pixelShaders.Clear();

                    if (_boxVertexBuffer != null)
                    {
                        _boxVertexBuffer.Dispose();
                    }

                    if (_swapChain != null)
                    {
                        _swapChain.Dispose();
                    }

                    if (_renderTargetView != null)
                    {
                        _renderTargetView.Dispose();
                    }

                    if (_inputLayout != null)
                    {
                        _inputLayout.Dispose();
                    }

                    if (_matricesBuffer != null)
                    {
                        _matricesBuffer.Dispose();
                    }

                    if (_matricesBufferDataStream != null)
                    {
                        _matricesBufferDataStream.Dispose();
                    }

                    if (_depthStencilTexture != null)
                    {
                        _depthStencilTexture.Dispose();
                    }

                    if (_depthStencilView != null)
                    {
                        _depthStencilView.Dispose();
                    }

                    if (_device != null)
                    {
                        _device.Dispose();
                    }

                    if (_deviceContext != null)
                    {
                        _deviceContext.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
