using SlimDX;
using SlimDX.DirectInput;
using SlimDX.XInput;
using System;
using System.Drawing;
using System.Linq;

namespace CGARenderer
{
    public class Input : IDisposable
    {
        private bool _isDisposed = false;
        private DirectInput _directInput;
        private Keyboard _keyboard;
        private KeyboardState _keyboardStateCurrent;
        private KeyboardState _keyboardStateLast;
        private Mouse _mouse;
        private MouseState _mouseStateCurrent;
        private MouseState _mouseStateLast;
        private Joystick _joystick1;
        private JoystickState _joy1StateCurrent;
        private JoystickState _joy1StateLast;
        private Controller _controller1;
        private Gamepad _controller1StateCurrent;
        private Gamepad _controller1StateLast;

        public Input()
        {
            InitDirectInput();
            InitXInput();
            // We need to intiailize these because otherwise we will get a null reference error
            // if the program tries to access these on the first frame.
            _keyboardStateCurrent = new KeyboardState();
            _keyboardStateLast = new KeyboardState();
            _mouseStateCurrent = new MouseState();
            _mouseStateLast = new MouseState();
            _joy1StateCurrent = new JoystickState();
            _joy1StateLast = new JoystickState();
            _controller1StateCurrent = new Gamepad();
            _controller1StateLast = new Gamepad();
        }

        private void InitDirectInput()
        {
            _directInput = new DirectInput();
            if (_directInput == null)
            {
                throw new Exception("DirectInput initialization failed");
            }
            _keyboard = new Keyboard(_directInput);
            if (_keyboard == null)
            {
                throw new Exception("Keyboard creation failed");
            }
            _mouse = new Mouse(_directInput);
            if (_mouse == null)
            {
                throw new Exception("Mouse creation failed");
            }
            var firstDevice = _directInput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault();
            if (firstDevice == null)
            {
                return;
            }
            _joystick1 = new Joystick(_directInput, firstDevice.InstanceGuid);
            if (_joystick1 == null)
            {
                throw new Exception("Joystick creation failed");
            }
            _joystick1.Properties.SetRange(-1000, 1000);
        }

        private void InitXInput()
        {
            _controller1 = new Controller(UserIndex.One);
        }

        public void Update()
        {
            if (_keyboard.Acquire().IsFailure || _mouse.Acquire().IsFailure || _joystick1.Acquire().IsFailure)
            {
                return;
            }

            _keyboardStateLast = _keyboardStateCurrent;
            _keyboardStateCurrent = _keyboard.GetCurrentState();

            _mouseStateLast = _mouseStateCurrent;
            _mouseStateCurrent = _mouse.GetCurrentState();

            _joy1StateLast = _joy1StateCurrent;
            _joy1StateCurrent = _joystick1.GetCurrentState();

            _controller1StateLast = _controller1StateCurrent;
            _controller1StateCurrent = _controller1.GetState().Gamepad;
        }

        public bool IsKeyPressed(Key key)
        {
            return _keyboardStateCurrent.IsPressed(key);
        }

        public bool WasKeyPressed(Key key)
        {
            return _keyboardStateLast.IsPressed(key);
        }

        public bool IsKeyReleased(Key key)
        {
            return _keyboardStateCurrent.IsReleased(key);
        }

        public bool WasKeyReleased(Key key)
        {
            return _keyboardStateLast.IsReleased(key);
        }

        public bool IsKeyHeldDown(Key key)
        {
            return _keyboardStateCurrent.IsPressed(key) && _keyboardStateLast.IsPressed(key);
        }

        public bool IsKeyJustPressed(Key key)
        {
            return _keyboardStateCurrent.IsPressed(key) && !_keyboardStateLast.IsPressed(key);
        }

        public bool IsMouseButtonPressed(int button)
        {
            return _mouseStateCurrent.IsPressed(button);
        }

        public bool WasMouseButtonPressed(int button)
        {
            return _mouseStateLast.IsPressed(button);
        }

        public bool IsMouseButtonReleased(int button)
        {
            return _mouseStateCurrent.IsReleased(button);
        }

        public bool WasMouseButtonReleased(int button)
        {
            return _mouseStateLast.IsReleased(button);
        }

        public bool IsMouseButtonHeldDown(int button)
        {
            return _mouseStateCurrent.IsPressed(button) && _mouseStateLast.IsPressed(button);
        }

        public bool MouseHasMoved()
        {
            if ((_mouseStateCurrent.X != _mouseStateLast.X) || (_mouseStateCurrent.Y != _mouseStateLast.Y))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Vector2 MousePosition()
        {
            return new Vector2(_mouseStateCurrent.X, _mouseStateCurrent.Y);
        }

        public Vector2 LastMousePosition()
        {
            return new Vector2(_mouseStateLast.X, _mouseStateLast.Y);
        }

        public int MouseWheelMovement()
        {
            return _mouseStateCurrent.Z;
        }

        public bool DI_IsButtonPressed(int button)
        {
            return _joy1StateCurrent.IsPressed(button);
        }

        public bool DI_WasButtonPressed(int button)
        {
            return _joy1StateLast.IsPressed(button);
        }

        public bool DI_IsButtonReleased(int button)
        {
            return _joy1StateCurrent.IsReleased(button);
        }

        public bool DI_WasButtonReleased(int button)
        {
            return _joy1StateLast.IsReleased(button);
        }

        public Point DI_LeftStickPosition()
        {
            return new Point(_joy1StateCurrent.X, _joy1StateCurrent.Y);
        }

        public Point DI_RightStickPosition()
        {
            return new Point(_joy1StateCurrent.RotationX, _joy1StateCurrent.RotationY);
        }

        public int DI_TriggersAxis()
        {
            return _joy1StateCurrent.Z;
        }

        public bool XI_IsButtonPressed(GamepadButtonFlags button)
        {
            return _controller1StateCurrent.Buttons.HasFlag(button);
        }

        public bool XI_WasButtonPressed(GamepadButtonFlags button)
        {
            return _controller1StateLast.Buttons.HasFlag(button);
        }

        public bool XI_IsButtonReleased(GamepadButtonFlags button)
        {
            return !(_controller1StateCurrent.Buttons.HasFlag(button));
        }

        public bool XI_WasButtonReleased(GamepadButtonFlags button)
        {
            return !(_controller1StateLast.Buttons.HasFlag(button));
        }

        public Point XI_LeftStickPosition()
        {
            return new Point(_controller1StateCurrent.LeftThumbX, _controller1StateCurrent.LeftThumbY);
        }

        public Point XI_RightStickPosition()
        {
            return new Point(_controller1StateCurrent.RightThumbX, _controller1StateCurrent.RightThumbY);
        }

        public int XI_LeftTrigger()
        {
            return _controller1StateCurrent.LeftTrigger;
        }

        public int XI_RightTrigger()
        {
            return _controller1StateCurrent.RightTrigger;
        }

        public void Dispose()
        {
            Dispose(true);
            // Since Dispose() method already cleaned up the resources used by object, there's no need for the
            // Garbage Collector to call class's Finalizer, so we tell it not to.
            // We did not implement a Finalizer for class as in our case we don't need to implement it.
            // The Finalize() method is used to give the object a chance to clean up its unmanaged resources before it
            // is destroyed by the Garbage Collector.  Since we are only using managed code, we do not need to
            // implement the Finalize() method.
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                /*
                * The following text is from MSDN  (http://msdn.microsoft.com/en-us/library/fs2xkftw%28VS.80%29.aspx)
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
                    if (_directInput != null)
                        _directInput.Dispose();

                    if (_keyboard != null)
                        _keyboard.Dispose();

                    if (_mouse != null)
                        _mouse.Dispose();

                    if (_joystick1 != null)
                        _joystick1.Dispose();
                }
                // get rid of unmanaged resources
            }
        }

        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        public Keyboard Keyboard
        {
            get
            {
                return _keyboard;
            }
        }

        public KeyboardState KeyboardState_Current
        {
            get
            {
                return _keyboardStateCurrent;
            }
        }

        public KeyboardState KeyboardState_Previous
        {
            get
            {
                return _keyboardStateLast;
            }
        }

        public Joystick Joystick1
        {
            get
            {
                return _joystick1;
            }
        }

        public JoystickState Joy1State_Current
        {
            get
            {
                return _joy1StateCurrent;
            }
        }

        public JoystickState Joy1State_Last
        {
            get
            {
                return _joy1StateLast;
            }
        }

        public Mouse Mouse
        {
            get
            {
                return _mouse;
            }
        }

        public MouseState MouseState_Current
        {
            get
            {
                return _mouseStateCurrent;
            }
        }

        public MouseState MouseState_Previous
        {
            get
            {
                return _mouseStateLast;
            }
        }

        public Controller XInputController1
        {
            get
            {
                return _controller1;
            }
        }

        public Gamepad XInput_Controller1State_Curr
        {
            get
            {
                return _controller1StateCurrent;
            }
        }

        public Gamepad XInput_Controller1State_Last
        {
            get
            {
                return _controller1StateLast;
            }
        }
    }
}
