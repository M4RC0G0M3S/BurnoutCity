using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace BurnoutCity.Map
{
    public class MapManager { 
        public int worldWidth { get; private set; }
        public int worldHeight { get; private set; }
        public List<MapSprite> _sprites = new();

        private readonly Dictionary<string, Texture2D> _textures = new();

        public MapManager(int worldWidth, int worldHeight) // Construtor do MapManager
        {
            this.worldWidth = worldWidth;
            this.worldHeight = worldHeight;
        }

        public void LoadContent(ContentManager content) // Carrega as texturas do mapa
        {
            LoadTexture(content, "Building_Garagem", "Sprites/World/Buildings/Building_Garagem");
            LoadTexture(content, "Building_Hotel", "Sprites/World/Buildings/Building_Hotel_Decorative");
            LoadTexture(content, "Building_Loja", "Sprites/World/Buildings/Building_Loja");
            LoadTexture(content, "Building_Predio", "Sprites/World/Buildings/Building_Predio_Decorative");
            LoadTexture(content, "Building_PredioResidencias", "Sprites/World/Buildings/Building_PredioResidencias_Decorative");
            LoadTexture(content, "Building_Restaurante", "Sprites/World/Buildings/Building_Restaurante_Decorative");


            LoadTexture(content, "Road_Cruzamento", "Sprites/World/Roads/Road_Cruzamento");
            LoadTexture(content, "Road_Curva", "Sprites/World/Roads/Road_Curva");
            LoadTexture(content, "Road_Horizontal", "Sprites/World/Roads/Road_Horizontal");
            LoadTexture(content, "Road_Vertical", "Sprites/World/Roads/Road_Vertical");
            LoadTexture(content, "Road_Avenida", "Sprites/World/Roads/Road_Avenida");
            LoadTexture(content, "Road_Avenida_Cruzamento", "Sprites/World/Roads/Road_Avenida_Cruzamento");

            Console.WriteLine($"[MapManager] {_textures.Count}Texturas do mapa carregadas com sucesso!");
        }


        public void LoadTexture(ContentManager content, string key, string assetPath) // Carrega uma textura e a armazena no dicionario
        {
            try
            {
                _textures[key] = content.Load<Texture2D>(assetPath);
                Console.WriteLine($"[MapManager] Loaded: {key}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapManager] Error loading texture '{key}' from '{assetPath}': {ex.Message}");
            }
    }
        public Texture2D? GetTexture(string textureName) // Retorna uma textura do dicionario
        {
            return _textures.TryGetValue(textureName, out var tex) ? tex : null;
        }

       public void AddSprite(string textureName, Vector2 position, int layer = 0, float scale = 1.0f)
        {
            Texture2D? texture = GetTexture(textureName);
            if (texture == null)
            {
                Console.WriteLine($"[MapManager] Warning: Texture '{textureName}' not found. Sprite not added.");
                return;
            }
            _sprites.Add(new MapSprite(texture, position, layer, scale));
        }


        public void AddSprite(string textureName, Vector2 position, int layer, float scale, float rotation)
        {
            Texture2D? texture = GetTexture(textureName);
            if (texture == null)
            {
                Console.WriteLine($"[MapManager] Warning: Texture '{textureName}' not found. Sprite not added.");
                return;
            }
            var sprite = new MapSprite(texture, position, layer, scale)
            {
                Rotation = rotation
            };
            _sprites.Add(sprite);
        }


        public void AddSprite(MapSprite sprite)
        {
            _sprites.Add(sprite);
        }

                public void Draw(SpriteBatch spriteBatch)
                {
                    var sortedSprites = _sprites.OrderBy(s => s.Layer).ToList(); // Ordena os sprites por camada para garantir a ordem correta de desenho
                    foreach (var sprite in sortedSprites)
                    {
                        sprite.Draw(spriteBatch);
                    }
                }

                public List<MapSprite> GetAllSprites() => _sprites; // Retorna uma lista de todos os sprites do mapa

                public void ClearSprites() => _sprites.Clear(); // Limpa todos os sprites do mapa, útil para resetar o estado do mapa ou carregar um novo mapa

        }




}