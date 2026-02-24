using Microsoft.Xna.Framework;
namespace BurnoutCity.Core
{
    public class Camera
    {
        public Vector2 Position {get; private set;} // Posicao da camera
        public Vector2 Target {get; private set;} // Alvo da camera

        private readonly int _viewportWidth; // Largura da viewport
        private readonly int _viewportHeight; // Altura da viewport

        public Camera(int viewportWidth, int viewportHeight) // Construtor da camera
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            Position = Vector2.Zero;
            Target = Vector2.Zero;
        }

        public void Update(Vector2 targetPosition) // Atualiza a posicao da camera com base no alvo
        {
            Target = targetPosition;
            Position = Target;
        }


        public Matrix GetTransform() // Retorna a matriz de transformacao da camera
        {
            return Matrix.CreateTranslation(
                -Position.X + _viewportWidth / 2f,
                -Position.Y + _viewportHeight / 2f,
                0f
            );
            
    }

    }
}