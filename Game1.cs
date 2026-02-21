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

    private Car _playerCar = null!;
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
        //Criar o Carro do Jogador no centro do ecra

        Vector2 spawnPosition = new Vector2(ScreenWidth / 2f, ScreenHeight / 2f);
        _playerCar = new Car(spawnPosition);

        //Criar o GameStateManager e inicializar o estado inicial do jogo 
        _stateManager = new GameStateManager();
        _stateManager.Initialize(GraphicsDevice, Content);

        //_stateManager.ChangeState(new MenuState()); //Definir o estado inicial do jogo como MenuState
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
        _playerCar.Update(gameTime); //Atualizar o estado do carro do jogador a cada frame, permitindo que ele responda a entradas e se mova no jogo
        _stateManager.Update(gameTime);
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 15, 20)); //Limpar a tela com uma cor de fundo escura
        _spriteBatch.Begin(); //Iniciar o SpriteBatch para desenhar os elementos do jogo
        _playerCar.Draw(_spriteBatch, PixelTexture); //Desenhar o carro do jogador usando o SpriteBatch e a textura de pixel para criar uma representação visual do carro no jogo
        _stateManager.Draw(_spriteBatch); //Desenhar o estado ativo do jogo usando o SpriteBatch
        _spriteBatch.End(); //Finalizar o SpriteBatch após desenhar os elementos do jogo
        base.Draw(gameTime); //Chamar o método base para garantir que outros elementos do jogo sejam desenhados corretamente
    }
    
}
