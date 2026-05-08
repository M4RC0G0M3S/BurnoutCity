using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.States
{
    public class GarageState : BaseState
    {
        private SpriteFont _fontMedium;
        private Texture2D _pixel;
        private MouseState _prevMouse;

        private Rectangle _btnRepair;
        private Rectangle _btnExit;

        public override void LoadContent()
        {
            _fontMedium = ContentManager.Load<SpriteFont>("Fonts/FontMedium");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int cx = GraphicsDevice.Viewport.Width / 2;
            _btnRepair = new Rectangle(cx - 150, 300, 300, 60);
            _btnExit = new Rectangle(cx - 150, 400, 300, 60);
        }

        public override void Update(GameTime gameTime)
        {
            MouseState mouse = Mouse.GetState();
            PlayerData pd = GameStateManager.Instance.PlayerData;

            // 10€ por cada 1% de dano
            int repairCost = (int)(pd.CarDamage * 10); 

            if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                if (_btnRepair.Contains(mouse.Position))
                {
                    if (pd.CarDamage > 0 && pd.Money >= repairCost)
                    {
                        pd.SpendMoney(repairCost);
                        pd.RepairCar();
                    }
                }
                else if (_btnExit.Contains(mouse.Position))
                {
                    GameStateManager.Instance.ChangeState(new ExplorationState());
                }
            }
            _prevMouse = mouse;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            PlayerData pd = GameStateManager.Instance.PlayerData;
            int repairCost = (int)(pd.CarDamage * 10);

            spriteBatch.Draw(_pixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), new Color(20, 20, 20, 240));

            spriteBatch.DrawString(_fontMedium, "GARAGEM - REPARACAO", new Vector2(GraphicsDevice.Viewport.Width / 2 - 120, 100), Color.White);
            spriteBatch.DrawString(_fontMedium, $"Dano: {pd.CarDamage:F1}%", new Vector2(GraphicsDevice.Viewport.Width / 2 - 60, 180), Color.Red);
            spriteBatch.DrawString(_fontMedium, $"Dinheiro: {pd.Money} EUR", new Vector2(GraphicsDevice.Viewport.Width / 2 - 80, 220), Color.LimeGreen);

            Color repairColor = (pd.CarDamage > 0 && pd.Money >= repairCost) ? Color.Orange : Color.DimGray;
            spriteBatch.Draw(_pixel, _btnRepair, repairColor);
            spriteBatch.DrawString(_fontMedium, $"Reparar ({repairCost} EUR)", new Vector2(_btnRepair.X + 40, _btnRepair.Y + 15), Color.Black);

            spriteBatch.Draw(_pixel, _btnExit, Color.DarkRed);
            spriteBatch.DrawString(_fontMedium, "Sair", new Vector2(_btnExit.X + 120, _btnExit.Y + 15), Color.White);

            spriteBatch.End();
        }
    }
}