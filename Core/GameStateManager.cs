using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BurnoutCity.Data;

namespace BurnoutCity.Core
{
    public class GameStateManager
    {
        public static GameStateManager  Instance { get; private set; } = null!; //Singleton para garantir que haja apenas um estado ativo no GameStateManager

        public PlayerData PlayerData { get; private set; } = new PlayerData();
        private GraphicsDevice _graphicsDevice = null!; // variável para armazenar o dispositivo gráfico, necessário para renderizar os gráficos do jogo
        private ContentManager _contentManager = null!; // variável para armazenar o gerenciador de conteúdo, necessário para carregar os recursos do jogo, como texturas, sons e outros ativos7

        private IGameState? _currentState; // variável para armazenar o estado atual do jogo, que implementa a interface IGameState
        private IGameState? _pendingState; // variável para armazenar o próximo estado do jogo, que implementa a interface IGameState

        private IGameState? CurrentState =>_currentState; // propriedade para acessar o estado atual do jogo

        public GameStateManager()
        {
            Instance = this; // atribui a instância atual do GameStateManager à propriedade Instance, garantindo que haja apenas um estado ativo no jogo
        }

        public void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            _graphicsDevice = graphicsDevice; // inicializa o dispositivo gráfico
            _contentManager = contentManager; // inicializa o gerenciador de conteúdo
            PlayerData.LoadFrom(SaveManager.Instance.CurrentSave);
        }

        public void ChangeState(IGameState newState)
        {
            _pendingState = newState; // define o próximo estado do jogo
        }   

        public void Update(GameTime gameTime)
        {
            if (_pendingState != null) // verifica se há um próximo estado definido
            {
              AplyStateChange(_pendingState); // aplica a mudança de estado, carregando o novo estado e liberando os recursos do estado anterior
              _pendingState = null; // redefine o próximo estado para null após a mudança
            }


            _currentState?.Update(gameTime); // atualiza o estado atual do jogo, se houver um estado ativo

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _currentState?.Draw(spriteBatch); // desenha o estado atual do jogo, se houver um estado ativo
        }


        private void AplyStateChange(IGameState newState)
        {
            _currentState?.UnloadContent(); // libera os recursos do estado atual, se houver um estado ativo
            _currentState = newState; // define o novo estado como o estado atual
            _currentState.Initialize(_graphicsDevice, _contentManager); // inicializa o novo estado com o dispositivo gráfico e o gerenciador de conteúdo
            _currentState.LoadContent(); // carrega os recursos do novo estado
        }
    }
}