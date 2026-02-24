using BurnoutCity.Core;
using BurnoutCity.Entities;
using BurnoutCity.States;
using Microsoft.Xna.Framework;


using Microsoft.Xna.Framework.Graphics;
namespace BurnoutCity;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private GameStateManager _stateManager = null!;

    public static Texture2D PixelTexture { get; private set; } = null!;
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight,
            IsFullScreen = false
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Burnout City";
    }

    protected override void Initialize()
    {

        //Criar o GameStateManager e inicializar o estado inicial do jogo 
        _stateManager = new GameStateManager();
        _stateManager.Initialize(GraphicsDevice, Content);

        _stateManager.ChangeState(new ExplorationState()); //Definir o estado inicial do jogo como MenuState
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        PixelTexture = new Texture2D(GraphicsDevice, 1, 1); //Criar uma textura de 1x1 pixel para desenhar formas simples
        PixelTexture.SetData(new[] { Color.White }); //Definir a cor do pixel para branco, permitindo que seja tintada para outras cores ao desenhar
    }
    protected override void Update(GameTime gameTime)
    {
        _stateManager.Update(gameTime);
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 15, 20)); //Limpar a tela com uma cor de fundo escura
        _spriteBatch.Begin(); //Iniciar o SpriteBatch para desenhar os elementos do jogo
        _stateManager.Draw(_spriteBatch); //Desenhar o estado ativo do jogo usando o SpriteBatch
        _spriteBatch.End(); //Finalizar o SpriteBatch após desenhar os elementos do jogo
        base.Draw(gameTime); //Chamar o método base para garantir que outros elementos do jogo sejam desenhados corretamente
    }
    
}
