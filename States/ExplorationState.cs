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

  

        public override void LoadContent()
        {
          
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

         
            Vector2 spawnPosition = new Vector2(640f, 360f);
            _playerCar = new Car(spawnPosition);

           
            _camera = new Camera(
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height
            );
        }



        public override void Update(GameTime gameTime)
        {
     
            _playerCar.Update(gameTime);

          
            _camera.Update(_playerCar.Position);
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

        public override void Draw(SpriteBatch spriteBatch)
        {
  
            spriteBatch.End();

            spriteBatch.Begin(transformMatrix: _camera.GetTransform());
            DrawDebugGrid(spriteBatch); // Desenhar a grelha de debug para ajudar a visualizar o movimento e a posição dos objetos no mundo do jogo
 
            _playerCar.Draw(spriteBatch, _pixelTexture);

            spriteBatch.End();

            spriteBatch.Begin();
        }

        
    }
}
