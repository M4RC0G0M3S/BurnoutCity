using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Map
{
    // Sprite estático do mundo (estrada, decoração, etc.) com posição, rotação e camada.
    // A posição representa o CENTRO do sprite (não o canto superior esquerdo).
    // O Draw é chamado pelo MapManager ordenado por Layer para controlar sobreposição.
    public class MapSprite
    {
        public Texture2D Texture { get;  set; }
        public Vector2 Position { get; set; }
        public Rectangle SourceRectangle { get; set; }
        public int Layer { get; set; } 
        public float Scale { get; set; }
        public Color Tint { get; set; }
        public float Rotation { get; set; } = 0f;

        public MapSprite(Texture2D texture, Vector2 position, int layer = 0, float scale = 1.0f)// Construtor para sprites que usam a textura inteira
        {
            Texture = texture;
            Position = position;
            SourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Layer = layer;
            Scale = scale;
            Tint = Color.White;
        }

        public MapSprite(Texture2D texture, Vector2 position, Rectangle sourceRectangle, int layer = 0, float scale = 1.0f) // Construtor para sprites que usam apenas uma parte da textura
        {
            Texture = texture;
            Position = position;
            SourceRectangle = sourceRectangle;
            Layer = layer;
            Scale = scale;
            Tint = Color.White;
        }

        // Devolve o retângulo do sprite no espaço do mundo (posição + dimensões escaladas).
        public Rectangle GetBounds()
        {
            return new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                (int)(SourceRectangle.Width * Scale),
                (int)(SourceRectangle.Height * Scale)
            );
        }
        // Desenha o sprite centrado em Position, com rotação e escala aplicadas.
        public void Draw(SpriteBatch spriteBatch)
        {
            // Origem no centro para que a rotação e a escala girem/cresçam à volta do ponto central
            Vector2 origin = new Vector2(SourceRectangle.Width / 2f, SourceRectangle.Height / 2f);
            
            spriteBatch.Draw(
                texture: Texture,
                position: Position, // A posição agora representa o CENTRO do sprite
                sourceRectangle: SourceRectangle,
                color: Tint,
                rotation: Rotation,
                origin: origin,
                scale: Scale,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }

        
    }
}