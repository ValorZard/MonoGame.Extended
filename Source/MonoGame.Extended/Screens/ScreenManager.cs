using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.Extended.Screens
{
    public class Transition : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Color _color;
        private readonly float _halfDuration;
        private float _currentValue;
        private TransitionState _state = TransitionState.Out;

        private enum TransitionState { In, Out }
        
        public Transition(GraphicsDevice graphicsDevice, Color color, float duration = 1.0f)
        {
            _graphicsDevice = graphicsDevice;
            _color = color;
            _halfDuration = duration / 2f;
            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public event EventHandler StateChanged;
        public event EventHandler Completed;

        public void Dispose()
        {
        }

        public void Update(GameTime gameTime)
        {
            var elapsedSeconds = gameTime.GetElapsedSeconds();

            switch (_state)
            {
                case TransitionState.Out:
                    _currentValue += elapsedSeconds;

                    if (_currentValue >= _halfDuration)
                    {
                        _state = TransitionState.In;
                        StateChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case TransitionState.In:
                    _currentValue -= elapsedSeconds;

                    if (_currentValue <= 0.0f)
                    {
                        Completed?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.FillRectangle(_graphicsDevice.Viewport.Bounds, _color * MathHelper.Clamp(_currentValue / _halfDuration, 0, 1));
            _spriteBatch.End();
        }
    }

    public class ScreenManager : SimpleDrawableGameComponent
    {
        public ScreenManager()
        {
        }

        private Screen _activeScreen;
        private bool _isInitialized;
        private bool _isLoaded;
        private Transition _activeTransition;

        public bool UpdateDuringTransitions { get; set; } = false;

        public void LoadScreen(Screen screen, Transition transition)
        {
            _activeTransition = transition;
            _activeTransition.StateChanged += (sender, args) => LoadScreen(screen);
            _activeTransition.Completed += (sender, args) =>
            {
                _activeTransition.Dispose();
                _activeTransition = null;
            };
        }

        public void LoadScreen(Screen screen)
        {
            _activeScreen?.UnloadContent();
            _activeScreen?.Dispose();

            screen.ScreenManager = this;

            if (_isInitialized)
                screen.Initialize();

            if (_isLoaded)
                screen.LoadContent();

            _activeScreen = screen;
        }

        public override void Initialize()
        {
            base.Initialize();
            _activeScreen?.Initialize();
            _isInitialized = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            _activeScreen?.LoadContent();
            _isLoaded = true;
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
            _activeScreen?.UnloadContent();
            _isLoaded = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (_activeTransition != null)
                _activeTransition.Update(gameTime);
            else
                _activeScreen?.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _activeScreen?.Draw(gameTime);
            _activeTransition?.Draw(gameTime);
        }
    }
}