using BurnoutCity.Core;
using BurnoutCity.Entities;
using BurnoutCity.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace BurnoutCity.States
{
    public class ExplorationState : BaseState
    {
      
        private Car _playerCar = null!;
        private Camera _camera = null!;
        private Texture2D _pixelTexture = null!;
        private Rectangle _worldBounds; // Define os limites do mundo para a camera
        private MapManager _mapManager = null!; // Gerenciador de mapas para carregar e desenhar os sprites do mundo do jogo
        private BuildingManager _buildingManager = null!; // Gerenciador de prédios para criar e gerenciar os prédios do mundo do jogo

  

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

            _mapManager = new MapManager(_worldBounds.Width, _worldBounds.Height);
            _mapManager.LoadContent(ContentManager); 
            _buildingManager = new BuildingManager(_mapManager);

            CreateStreetLayout(); 
        }

        public void CreateStreetLayout()
        {
            const float scaleRua = 0.5f; 
            const float scaleCruzamento = 0.485f;
            const int seg = 256;

            int worldW = _worldBounds.Width;
            int worldH = _worldBounds.Height;

            int avH = 768;
            int avV = 1792;
            int streetH1 = 256;
            int streetH2 = 1536;
            int streetV1 = 1024;
            int streetV2 = 2560;

            for (int x = 0; x < worldW; x += seg)
            {
                Vector2 ajuste = new Vector2(0, -9);

                Vector2 posAv = new Vector2(x, avH);
                if (x == avV || x == streetV1 || x == streetV2)
                    _mapManager.AddSprite("Road_Cruzamento", posAv + ajuste, layer: 2, scale: scaleCruzamento* 1.04f);
                else
                    _mapManager.AddSprite("Road_Horizontal", posAv, layer: 0, scale: scaleRua);

                Vector2 posH1 = new Vector2(x, streetH1);
                Vector2 posH2 = new Vector2(x, streetH2);
            

                if (x == avV || x == streetV1 || x == streetV2)
                {
                    _mapManager.AddSprite("Road_Cruzamento", posH1 + ajuste, layer: 1, scale: scaleCruzamento * 1.04f);
                    _mapManager.AddSprite("Road_Cruzamento", posH2 + ajuste, layer: 1, scale: scaleCruzamento * 1.04f);
                }
                else
                {
                    _mapManager.AddSprite("Road_Horizontal", posH1, layer: 0, scale: scaleRua);
                    _mapManager.AddSprite("Road_Horizontal", posH2, layer: 0, scale: scaleRua);
                }
            }

            for (int y = 0; y < worldH; y += seg)
            {
                if (y == avH || y == streetH1 || y == streetH2) continue;

                float rot = MathHelper.PiOver2;

                _mapManager.AddSprite("Road_Horizontal", new Vector2(avV, y), layer: 0, scale: scaleRua, rotation: rot);
                _mapManager.AddSprite("Road_Horizontal", new Vector2(streetV1, y), layer: 0, scale: scaleRua, rotation: rot);
                _mapManager.AddSprite("Road_Horizontal", new Vector2(streetV2, y), layer: 0, scale: scaleRua, rotation: rot);
            }

            System.Console.WriteLine($"[ExplorationState] {_mapManager._sprites.Count} sprites criados.");
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
            spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _worldBounds.Width, _worldBounds.Height), new Color(40,40,45)); // Desenha o fundo do mundo do jogo como um grande retângulo verde escuro
            DrawDebugGrid(spriteBatch); // Desenhar a grelha de debug para ajudar a visualizar o movimento e a posição dos objetos no mundo do jogo
            DrawWorldBounds(spriteBatch); // Desenhar os limites do mundo para ajudar a visualizar o espaço de jogo
            _mapManager.Draw(spriteBatch); // Desenhar os sprites do mapa (ainda vazio, mas pronto para ser implementado)
            _playerCar.Draw(spriteBatch, _pixelTexture);
            _buildingManager.Draw(spriteBatch, _pixelTexture, _playerCar.Position); // Desenhar os prédios do mundo do jogo e os highlights de interação quando o jogador estiver próximo   

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
