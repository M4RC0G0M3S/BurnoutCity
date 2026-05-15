using BurnoutCity.Core;
using BurnoutCity.Entities;
using BurnoutCity.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private GameStateManager _stateManager = null!;

    public static Texture2D PixelTexture { get; private set; } = null!;
    public static System.Action? QuitGame { get; private set; }

    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    private KeyboardState _previousKeyboardState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight,
            IsFullScreen = true
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        Window.Title = "Burnout City";

        QuitGame = Exit;

        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _stateManager = new GameStateManager();
        _stateManager.Initialize(GraphicsDevice, Content);
        _stateManager.ChangeState(new MenuState());

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        PixelTexture = new Texture2D(GraphicsDevice, 1, 1);

        PixelTexture.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        // Toggle fullscreen com F11
        if (keyboardState.IsKeyDown(Keys.F11) &&
            !_previousKeyboardState.IsKeyDown(Keys.F11))
        {
            ToggleFullscreen();
        }

        _previousKeyboardState = keyboardState;

        _stateManager.Update(gameTime);

        base.Update(gameTime);
    }

    private void ToggleFullscreen()
    {
        _graphics.IsFullScreen = !_graphics.IsFullScreen;
        _graphics.ApplyChanges();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 15, 20));

        _spriteBatch.Begin();

        _stateManager.Draw(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}