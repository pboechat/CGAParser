using SlimDX;
using SlimDX.DirectInput;
using SlimDX.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace CGARenderer
{
    public class BaseApp : IDisposable
    {
        private bool _isDisposed = false; // Indicates whether or not the game window has been disposed.
        private bool _isInitialized = false; // Indicates whether or not the game window has been initialized yet.
        private bool _isFullscreen = false; // Indicates whether or not the game window is running in fullscreen mode.        
        private bool _isPaused = false;     // Indicates whether the game is paused.
        private RenderForm _form; // The SlimDX form that will be our game window.
        private Color4 _clearColor; // The color to use when clearing the screen.
        private UserInput _userInput;
        // These are protected in case a subclass wants to override the GameLoop() method to create a different type
        // of game loop.
        private long _currFrameTime;    // Stores the time for the current frame.
        private long _lastFrameTime;    // Stores the time of the last frame.
        protected readonly int _frameCount;    // Stores the number of frames completed so far during the current second.
        private int m_FPS;           // Stores the number of frames we rendered during the previous second.

        public BaseApp(string title, int width, int height, bool fullscreen)
        {
            // Store parameters in member variables.
            _isFullscreen = fullscreen;
            _clearColor = new Color4(1.0f, 0.0f, 0.0f, 0.0f);

            // Create the game window that will display the game.
            _form = new RenderForm(title);
            _form.ClientSize = new Size(width, height);

            // Hook up event handlers so we can recieve events from the RenderForm object.
            _form.FormClosed += FormClosed;

            _userInput = new UserInput();
        }

        public virtual void GameLoop()
        {
            _lastFrameTime = _currFrameTime;
            _currFrameTime = Stopwatch.GetTimestamp();

            UpdateScene((double)(_currFrameTime - _lastFrameTime) / Stopwatch.Frequency);

            RenderScene();

            // This code tracks our frame rate.
            m_FPS = (int)(Stopwatch.Frequency / ((float)(_currFrameTime - _lastFrameTime)));
        }

        public void Start()
        {
            // If initialization is already finished, then simply return.
            if (_isInitialized)
                return;

            _isInitialized = true;

            // Start the message pump.
            MessagePump.Run(_form, GameLoop);
        }

        public virtual void UpdateScene(double frameTime)
        {
            // Get the latest user input.
            _userInput.Update();

            if (_userInput.IsKeyPressed(Key.Return) && (_userInput.IsKeyPressed(Key.LeftAlt) || _userInput.IsKeyPressed(Key.RightAlt)))
            {
                // Toggle fullscreen mode.
                ToggleFullscreen();
            }
            else if (_userInput.IsKeyPressed(Key.Escape))
            {
                // Close the program.
                _form.Close();
            }
        }

        public virtual void RenderScene()
        {
            if ((!IsInitialized) || IsDisposed)
            {
                return;
            }
        }

        public virtual void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;
        }

        public void Dispose()
        {
            Dispose(true);

            // Since this Dispose() method already cleaned up the resources used by this object, there's no need for the
            // Garbage Collector to call this class's Finalizer, so we tell it not to.
            // We did not implement a Finalizer for this class as in our case we don't need to implement it.
            // The Finalize() method is used to give the object a chance to clean up its unmanaged resources before it
            // is destroyed by the Garbage Collector.  Since we are only using managed code, we do not need to
            // implement the Finalize() method.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
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
                    _form.FormClosed -= FormClosed;
                    // get rid of managed resources
                    _userInput.Dispose();
                }
                // get rid of unmanaged resources

            }
            _isDisposed = true;
        }

        public Color4 ClearColor
        {
            get
            {
                return _clearColor;
            }
            protected set
            {
                _clearColor = value;
            }
        }

        public long CurrentFrameTime
        {
            get
            {
                return _currFrameTime;
            }
            protected set
            {
                _currFrameTime = value;
            }
        }

        public RenderForm FormObject
        {
            get
            {
                return _form;
            }
        }

        public int FramesPerSecond
        {
            get
            {
                return m_FPS;
            }
            protected set
            {
                m_FPS = value;
            }
        }

        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        public bool IsFullscreen
        {
            get
            {
                return _isFullscreen;
            }
            protected set
            {
                _isFullscreen = value;
            }
        }

        public virtual bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }
        }


        public bool IsPaused
        {
            get
            {
                return _isPaused;
            }
            protected set
            {
                _isPaused = value;
            }
        }

        public long LastFrameTime
        {
            get
            {
                return _lastFrameTime;
            }
            protected set
            {
                _lastFrameTime = value;
            }
        }

        public UserInput UserInput
        {
            get
            {
                return _userInput;
            }
        }

        public virtual void FormClosed(object o, FormClosedEventArgs e)
        {
            if (!_isDisposed)
                Dispose();
        }
    }
}
