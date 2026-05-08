using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Entities
{
    /// <summary>
    /// Gere uma animação baseada numa sprite sheet horizontal.
    /// Todos os frames têm o mesmo tamanho e estão lado a lado.
    /// </summary>
    public class SpriteAnimation
    {
        private Texture2D _sheet;
        private int _frameWidth;
        private int _frameHeight;
        private int _totalFrames;
        private float _fps;

        private float _timer = 0f;
        private int   _currentFrame = 0;

        public bool  IsPlaying  { get; private set; } = false;
        public bool  Loop       { get; set; }         = true;
        public float Alpha      { get; set; }         = 1f;
        public float Scale      { get; set; }         = 1f;
        public Color Tint       { get; set; }         = Color.White;

        public SpriteAnimation(Texture2D sheet, int frameWidth, int frameHeight, float fps)
        {
            _sheet       = sheet;
            _frameWidth  = frameWidth;
            _frameHeight = frameHeight;
            _fps         = fps;
            _totalFrames = sheet.Width / frameWidth;
        }

        public void Play()
        {
            IsPlaying     = true;
            _currentFrame = 0;
            _timer        = 0f;
        }

        public void Stop()
        {
            IsPlaying = false;
            _currentFrame = 0;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsPlaying) return;

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer >= 1f / _fps)
            {
                _timer -= 1f / _fps;
                _currentFrame++;

                if (_currentFrame >= _totalFrames)
                {
                    if (Loop)
                        _currentFrame = 0;
                    else
                    {
                        _currentFrame = _totalFrames - 1;
                        IsPlaying = false;
                    }
                }
            }
        }

        /// Devolve a textura base (para reutilizar noutras instâncias)
        public Texture2D GetTexture() => _sheet;

        public void Draw(SpriteBatch spriteBatch, Vector2 position, float rotation = 0f)
        {
            if (!IsPlaying && !Loop) return;

            Rectangle source = new Rectangle(
                _currentFrame * _frameWidth,
                0,
                _frameWidth,
                _frameHeight
            );

            Vector2 origin = new Vector2(_frameWidth / 2f, _frameHeight / 2f);

            spriteBatch.Draw(
                _sheet,
                position,
                source,
                Tint * Alpha,
                rotation,
                origin,
                Scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
