using BurnoutCity.Core;
using BurnoutCity.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.States
{
    public class ExplorationState : BaseState
    {
      
        private Car _playerCar = null!;
        private Camera _camera = null!;
        private Texture2D _pixelTexture = null!;
        private Rectangle _worldBounds; // Define os limites do mundo para a camera

  

        public override void LoadContent()
        {
          
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            int viewportWidth = GraphicsDevice.Viewport.Width;
            int viewportHeight = GraphicsDevice.Viewport.Height;
            _worldBounds = new Rectangle(0, 0, viewportWidth * 3, viewportHeight * 3); // Define um mundo maior que a viewport para permitir a movimentacao da camera

            Vector2 spawnpoint = new Vector2(
                _worldBounds.Width / 2f, 
                _worldBounds.Height / 2f
            );
            _playerCar = new Car(spawnpoint);
            _camera = new Camera(viewportWidth, viewportHeight, _worldBounds);
            _camera.Update(_playerCar.Position); // Inicializa a posicao da camera para o spawn do carro do jogador
        }



        public override void Update(GameTime gameTime)
        {
     
            _playerCar.Update(gameTime);

          
            _camera.Update(_playerCar.Position);
        }

              

        public override void Draw(SpriteBatch spriteBatch)
        {
  
            spriteBatch.End();

            spriteBatch.Begin(transformMatrix: _camera.GetTransform());
            DrawDebugGrid(spriteBatch); // Desenhar a grelha de debug para ajudar a visualizar o movimento e a posição dos objetos no mundo do jogo
            DrawWorldBounds(spriteBatch); // Desenhar os limites do mundo para ajudar a visualizar o espaço de jogo
 
            _playerCar.Draw(spriteBatch, _pixelTexture);

            spriteBatch.End();

            spriteBatch.Begin();
        }
        private void DrawDebugGrid(SpriteBatch spriteBatch)
        {
            int gridSize = 100; // tamanho de cada quadrado da grelha
            Color gridColor = new Color(30, 30, 40); // cinzento escuro

            // Desenha linhas verticais
            for (int x = 0; x < 5000; x += gridSize)
            {
                spriteBatch.Draw(_pixelTexture, 
                new Rectangle(x, 0, 2, 5000), 
                gridColor);
            }

            // Desenha linhas horizontais
            for (int y = 0; y < 5000; y += gridSize)
            {
                spriteBatch.Draw(_pixelTexture, 
                new Rectangle(0, y, 5000, 2), 
                gridColor);
            }
        } 

        private void DrawWorldBounds(SpriteBatch spriteBatch)
        {
            int thickness = 8; // espessura da borda
            Color boundaryColor =  Color.Red; // vermelho forte

            spriteBatch.Draw(_pixelTexture, 
                new Rectangle(_worldBounds.Left, _worldBounds.Top, _worldBounds.Width, thickness), // borda superior
                boundaryColor);
            spriteBatch.Draw(_pixelTexture, 
                new Rectangle(_worldBounds.Left, _worldBounds.Bottom - thickness, _worldBounds.Width, thickness), // borda inferior
                boundaryColor);
            spriteBatch.Draw(_pixelTexture, 
                new Rectangle(_worldBounds.Left, _worldBounds.Top, thickness, _worldBounds.Height), // borda esquerda
                boundaryColor);
            spriteBatch.Draw(_pixelTexture, 
                new Rectangle(_worldBounds.Right - thickness, _worldBounds.Top, thickness, _worldBounds.Height), // borda direita
                boundaryColor);
        }

        
    }
}
