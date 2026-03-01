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

        public MapManager(int worldWidth, int worldHeight)
        {
            this.worldWidth = worldWidth;
            this.worldHeight = worldHeight;
        }

        public void LoadContent(ContentManager content)
        {
            LoadTexture(content, "Building_Loja",             "Sprites/World/Buildings/Building_Loja");
            LoadTexture(content, "Building_CustomShop",       "Sprites/World/Buildings/Building_CustomShop");
            LoadTexture(content, "Building_Garagem",          "Sprites/World/Buildings/Building_Garagem");
            LoadTexture(content, "Building_RaceTrack",        "Sprites/World/Buildings/Building_RaceTrack");
            LoadTexture(content, "Building_TestTrack",        "Sprites/World/Buildings/Building_TestTrack");

            LoadTexture(content, "Building_Hotel",            "Sprites/World/Buildings/Building_Hotel_Decorative");
            LoadTexture(content, "Building_Restaurante",      "Sprites/World/Buildings/Building_Restaurante_Decorative");
            LoadTexture(content, "Building_Predio",           "Sprites/World/Buildings/Building_Predio_Decorative");
            LoadTexture(content, "Building_PredioResidencias","Sprites/World/Buildings/Building_PredioResidencias_Decorative");
            LoadTexture(content, "Building_Residencias",      "Sprites/World/Buildings/Building_Residencias_Decorative");
            LoadTexture(content, "Building_PredioIndustrias", "Sprites/World/Buildings/Building_PredioIndustrias_Decorative");

            LoadTexture(content, "Road_Cruzamento",           "Sprites/World/Roads/Road_Cruzamento");
            LoadTexture(content, "Road_Horizontal",           "Sprites/World/Roads/Road_Horizontal");

            Console.WriteLine($"[MapManager] {_textures.Count} texturas carregadas com sucesso!");
        }

        public void LoadTexture(ContentManager content, string key, string assetPath)
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

        public Texture2D? GetTexture(string textureName)
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
            var sortedSprites = _sprites.OrderBy(s => s.Layer).ToList();
            foreach (var sprite in sortedSprites)
            {
                sprite.Draw(spriteBatch);
            }
        }

        public List<MapSprite> GetAllSprites() => _sprites;

        public void ClearSprites() => _sprites.Clear();
    }
}
