using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.States
{
    public class CustomizationState : BaseState
    {
        private SpriteFont _fontMedium;
        private Texture2D _pixel;
        private MouseState _prevMouse;

        private Rectangle[] _colorBtns = new Rectangle[8];
        private Color[] _colors = { 
            Color.OrangeRed, Color.Blue, Color.Lime, Color.Yellow, 
            Color.Purple, Color.White, Color.Black, Color.Cyan 
        };
        private Rectangle _btnExit;

        public override void LoadContent()
        {
            _fontMedium = ContentManager.Load<SpriteFont>("Fonts/FontMedium");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Grelha de botões de cor
            for (int i = 0; i < 8; i++)
            {
                int row = i / 4;
                int col = i % 4;
                _colorBtns[i] = new Rectangle(150 + (col * 110), 200 + (row * 110), 90, 90);
            }

            _btnExit = new Rectangle(150, 450, 200, 50);
        }

        public override void Update(GameTime gameTime)
        {
            MouseState mouse = Mouse.GetState();
            PlayerData pd = GameStateManager.Instance.PlayerData;

            if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (_colorBtns[i].Contains(mouse.Position))
                    {
                        pd.SetCarColor(i); // Muda a cor no PlayerData
                    }
                }

                if (_btnExit.Contains(mouse.Position))
                    GameStateManager.Instance.ChangeState(new ExplorationState());
            }
            _prevMouse = mouse;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            PlayerData pd = GameStateManager.Instance.PlayerData;

            spriteBatch.Draw(_pixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), new Color(30, 10, 30, 240));

            spriteBatch.DrawString(_fontMedium, "OFICINA DE PINTURA", new Vector2(150, 80), Color.White);

            for (int i = 0; i < 8; i++)
            {
                // Quadrado da cor
                spriteBatch.Draw(_pixel, _colorBtns[i], _colors[i]);
                
                // Desenha a borda a indicar a cor atualmente equipada
                if (pd.CarColorIndex == i)
                    spriteBatch.Draw(_pixel, new Rectangle(_colorBtns[i].X, _colorBtns[i].Bottom + 5, 90, 5), Color.White);
            }

            spriteBatch.Draw(_pixel, _btnExit, Color.Red);
            spriteBatch.DrawString(_fontMedium, "Voltar", new Vector2(_btnExit.X + 60, _btnExit.Y + 10), Color.White);

            spriteBatch.End();
        }
    }
}