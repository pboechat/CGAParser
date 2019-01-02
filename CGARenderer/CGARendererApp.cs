using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DirectInput;
using SlimDX.DXGI;
using System;
using System.Windows.Forms;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;

namespace CGARenderer
{
    public class CGARendererApp : BaseApp, IDisposable
    {
        // is used to specify how we want to draw the cube.
        private enum GraphicsMode
        {
            SolidBlue = 0,
            PerVertexColoring,
            Textured
        }

        // struct stores data for a single vertex.
        private struct Vertex
        {
            public Vector4 Position;   // The position of the vertex in 3D space.
            public Color4 Color;       // The color to use for vertex when we are not using textured mode.
            public Vector2 TexCoord;   // The textures coordinates for vertex.  We need these when we are using textured mode.
        }

        private const float _MoveSpeed = 0.01f;     // Sets the speed you move around at.
        private const GraphicsMode _GraphicsMode = GraphicsMode.Textured;
        private const float _ZNear = 0.5f;
        private const float _Zfar = 1000.0f;
        private static readonly Vector3 _CameraTarget = new Vector3(0, 0, 0);
        private static readonly Vector3 _CameraUp = new Vector3(0, 1, 0);

        private Device _device; // The Direct3D device.
        private DeviceContext _deviceContext; // is just a convenience member.  It holds the context for the Direct3D device.
        private RenderTargetView _renderTargetView; // Our render target.
        private SwapChain _swapChain; // Our swap chain.
        private Viewport _viewport; // The viewport.
        private InputLayout _inputLayout;  // Tells Direct3D about the vertex format we are using.
        private VertexShader _vertexShader; // is the vertex shader.
        private ShaderSignature _vertexShaderSignature; // The vertex shader signature.
        private PixelShader _pixelShader; // is the pixel shader.
        private Buffer _cbChangesOnResize;
        private Buffer _cbChangesPerFrame;
        private Buffer _cbChangesPerObject;
        private DataStream _dataStream;
        private Matrix _viewMatrix;  // is our view matrix.
        private Matrix _projectionMatrix;  // The projection matrix.
        private Matrix _modelMatrix;   // The world matrix for the cube.  controls the current position and rotation of the cube.
        private Matrix _rotationMatrix;  // matrix controls the rotation of our cube.
        private Texture2D _depthStencilTexture = null;     // Holds the depth stencil texture.
        private DepthStencilView _depthStencilView = null; // The depth stencil view object.
        private ShaderResourceView _texture;        // Holds the texture for our cube.
        private SamplerState _textureSamplerState;      // The sampler state we will use with our cube texture.
        private Buffer _vertexBuffer; // will hold our geometry.
        private static Vector3 _cameraPosition = new Vector3(0, 2, -5); // The position of our camera in 3D space.
        private float _rotation = 0.005f; // The current rotation amount for the cube on the Y axis.

        public CGARendererApp(string title, int width, int height, bool fullscreen)
            : base(title, width, height, fullscreen)
        {
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
            // Create a buffer description.
            var bufferDescription = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                SizeInBytes = 64
            };

            // Create the changes on resize buffer.
            _cbChangesOnResize = new Buffer(_device, bufferDescription);

            // Create the changes per frame buffer.
            _cbChangesPerFrame = new Buffer(_device, bufferDescription);

            // Create the changes per object buffer.
            _cbChangesPerObject = new Buffer(_device, bufferDescription);

            // Send the Projection matrix into the changes on resize constant buffer.
            _dataStream = new DataStream(64, true, true)
            {
                Position = 0
            };
            _dataStream.Write(Matrix.Transpose(_projectionMatrix));
            _dataStream.Position = 0;
            _device.ImmediateContext.UpdateSubresource(new DataBox(0, 0, _dataStream), _cbChangesOnResize, 0);

            // Send the View matrix into the changes per frame buffer.
            _dataStream.Position = 0;
            _dataStream.Write(Matrix.Transpose(_viewMatrix));
            _dataStream.Position = 0;
            _device.ImmediateContext.UpdateSubresource(new DataBox(0, 0, _dataStream), _cbChangesPerFrame, 0);

            // Tell the VertexShader to use our constant buffers.
            _deviceContext.VertexShader.SetConstantBuffer(_cbChangesOnResize, 0);
            _deviceContext.VertexShader.SetConstantBuffer(_cbChangesPerFrame, 1);
            _deviceContext.VertexShader.SetConstantBuffer(_cbChangesPerObject, 2);
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
            if (_GraphicsMode == GraphicsMode.SolidBlue)
#pragma warning disable CS0162 // Unreachable code detected
                pixelShaderName = "Pixel_Shader_Blue";
            else if (_GraphicsMode == GraphicsMode.PerVertexColoring)
                pixelShaderName = "Pixel_Shader_Color";
#pragma warning restore CS0162 // Unreachable code detected
            else if (_GraphicsMode == GraphicsMode.Textured)
                pixelShaderName = "Pixel_Shader_Texture";

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

        public void InitScene()
        {
            // Create our projection matrix.
            _projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI * 0.5f, // is 90 degrees in radians
                FormObject.Width / (float)FormObject.Height,
                _ZNear,
                _Zfar);

            // Create our view matrix.
            _viewMatrix = Matrix.LookAtLH(_cameraPosition, _CameraTarget, _CameraUp);

            // Create the vertices of our cube.
            Vertex[] vertexData =
            {
                // Bottom face of the cube.
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },

                // Front face of the cube.
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(0.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                
                // Right face of the cube.
                new Vertex() { Position = new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 1) },

                // Back face of the cube.
                new Vertex() { Position = new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },

                // Left face of the cube.
                new Vertex() { Position = new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 1) },

                // Top face of the cube.
                new Vertex() { Position = new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 1.0f, 0.0f), TexCoord = new Vector2(0, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), Color = new Color4(1.0f, 1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), Color = new Color4(1.0f, 0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1, 1) },
            };

            // Create a DataStream object that we will use to put the vertices into the vertex buffer.
            var dataStream = new DataStream(40 * vertexData.Length, true, true)
            {
                Position = 0
            };
            foreach (var vertex in vertexData)
            {
                dataStream.Write(vertex);
            }
            dataStream.Position = 0;

            // Create a description for the vertex buffer.
            var bufferDescription = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = 40 * vertexData.Length,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 40
            };

            // Create the vertex buffer.
            _vertexBuffer = new Buffer(_device, dataStream, bufferDescription);

            // Dispose of the DataStream since we no longer need it.
            dataStream.Dispose();

            // Define the vertex format.
            // tells Direct3D what information we are storing for each vertex, and how it is stored.
            var inputElements = new InputElement[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR",    0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float,       InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0)
            };

            // Create the InputLayout using the vertex format we just created.
            _inputLayout = new InputLayout(_device, _vertexShaderSignature, inputElements);

            // Setup the InputAssembler stage of the Direct3D 11 graphics pipeline.
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, 40, 0));
            // Set the Primitive Topology.
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // Load the cube texture.
            _texture = ShaderResourceView.FromFile(_device, Application.StartupPath + "\\Brick.png");

            // Create a SamplerDescription
            var samplerDescription = new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            };

            // Create our SamplerState
            _textureSamplerState = SamplerState.FromDescription(_device, samplerDescription);
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
            _rotation += 0.00025f;
            if (_rotation > (2.0f * Math.PI))
            {
                _rotation = 0.0f;
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
            _viewMatrix = Matrix.LookAtLH(_cameraPosition, new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            // Send the updated view matrix into its constant buffer.
            _dataStream.Position = 0;
            _dataStream.Write(Matrix.Transpose(_viewMatrix));
            _dataStream.Position = 0;
            _device.ImmediateContext.UpdateSubresource(new DataBox(0, 0, _dataStream), _cbChangesPerFrame, 0);

            // Update the cube's rotation matrix.
            _rotationMatrix = Matrix.RotationAxis(new Vector3(0.0f, 1.0f, 0.0f), _rotation);

            // Update the cube's world matrix with the new translation and rotation matrices.
            _modelMatrix = _rotationMatrix;
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

            _deviceContext.PixelShader.SetShaderResource(_texture, 0);
            _deviceContext.PixelShader.SetSampler(_textureSamplerState, 0);

            // Send the cube's world matrix to the changes per object constant buffer.
            _dataStream.Position = 0;
            _dataStream.Write(Matrix.Transpose(_modelMatrix));
            _dataStream.Position = 0;
            _device.ImmediateContext.UpdateSubresource(new DataBox(0, 0, _dataStream), _cbChangesPerObject, 0);

            // Draw the triangle that we created in our vertex buffer.
            _deviceContext.Draw(36, 0);

            // Present the frame we just rendered to the user.
            _swapChain.Present(0, PresentFlags.None);
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

                    if (_vertexBuffer != null)
                        _vertexBuffer.Dispose();

                    if (_swapChain != null)
                        _swapChain.Dispose();

                    if (_renderTargetView != null)
                        _renderTargetView.Dispose();

                    if (_inputLayout != null)
                        _inputLayout.Dispose();

                    if (_cbChangesOnResize != null)
                        _cbChangesOnResize.Dispose();

                    if (_cbChangesPerFrame != null)
                        _cbChangesPerFrame.Dispose();

                    if (_cbChangesPerObject != null)
                        _cbChangesPerObject.Dispose();

                    if (_dataStream != null)
                        _dataStream.Dispose();

                    if (_depthStencilTexture != null)
                        _depthStencilTexture.Dispose();

                    if (_depthStencilView != null)
                        _depthStencilView.Dispose();

                    if (_texture != null)
                        _texture.Dispose();

                    if (_textureSamplerState != null)
                        _textureSamplerState.Dispose();

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
