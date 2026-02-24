using Microsoft.Xna.Framework;
namespace BurnoutCity.Core
{
    public class Camera
    {
        public Vector2 Position {get; private set;} // Posicao da camera
        public Vector2 Target {get; private set;} // Alvo da camera

        private readonly int _viewportWidth; // Largura da viewport
        private readonly int _viewportHeight; // Altura da viewport

        public float SmoothingFactor { get; set; } = 0.12f; // Fator de suavizacao para o movimento da camera 

        public Rectangle WorldBounds { get; set; } // Limites do mundo para a camera


        public Camera(int viewportWidth, int viewportHeight, Rectangle? worldBounds = null) // Construtor da camera
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            WorldBounds = worldBounds ?? new Rectangle(0, 0, viewportWidth, viewportHeight); // Define os limites do mundo, se nao for fornecido
            Position = Vector2.Zero;
            Target = Vector2.Zero;
        }

        public void Update(Vector2 targetPosition) // Atualiza a posicao da camera com base no alvo
        {
            Target = targetPosition;
            Position = Vector2.Lerp(Position, Target, SmoothingFactor);
            Position = ClampToWorldBounds(Position); // Garante que a camera nao ultrapasse os limites do mundo
        }


        public Matrix GetTransform() // Retorna a matriz de transformacao da camera
        {
            return Matrix.CreateTranslation(
                -Position.X + _viewportWidth / 2f,
                -Position.Y + _viewportHeight / 2f,
                0f
            );
            
        }

        private Vector2 ClampToWorldBounds(Vector2 position) // Garante que a camera nao ultrapasse os limites do mundo
        {
            float halfWidth = _viewportWidth / 2f;
            float halfHeight = _viewportHeight / 2f;

            float minX = WorldBounds.Left + halfWidth;
            float maxX = WorldBounds.Right - halfWidth;
            float minY = WorldBounds.Top + halfHeight;
            float maxY = WorldBounds.Bottom - halfHeight;

            if(maxX < minX) minX = maxX = (WorldBounds.Left + WorldBounds.Right) / 2f; // Se o mundo for menor que a viewport, centraliza a camera
            if(maxY < minY) minY = maxY = (WorldBounds.Top + WorldBounds.Bottom) / 2f; // Se o mundo for menor que a viewport, centraliza a camera    

            return new Vector2(
                MathHelper.Clamp(position.X, minX, maxX),
                MathHelper.Clamp(position.Y, minY, maxY)
            ); 

    }
}}