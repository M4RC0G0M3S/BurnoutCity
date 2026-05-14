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
        private SpriteFont _fontSmall;
        private Texture2D _pixel;
        private MouseState _currentMouse;
        private MouseState _prevMouse;

        private Rectangle _btnRepair;
        private Rectangle _btnExit;

        public override void LoadContent()
        {
            _fontMedium = ContentManager.Load<SpriteFont>("Fonts/FontMedium");
            try { _fontSmall = ContentManager.Load<SpriteFont>("Fonts/FontSmall"); }
            catch { _fontSmall = _fontMedium; }

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int cx = GraphicsDevice.Viewport.Width / 2;
            int cy = GraphicsDevice.Viewport.Height / 2;

            _btnRepair = new Rectangle(cx - 150, cy + 40, 300, 60);
            _btnExit = new Rectangle(cx - 100, cy + 130, 200, 50);
        }

        public override void Update(GameTime gameTime)
        {
            _currentMouse = Mouse.GetState();
            PlayerData pd = GameStateManager.Instance.PlayerData;

            int repairCost = (int)(pd.CarDamage * 10); 

            if (_currentMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                if (_btnRepair.Contains(_currentMouse.Position))
                {
                    if (pd.CarDamage > 0 && pd.Money >= repairCost)
                    {
                        pd.SpendMoney(repairCost);
                        pd.RepairCar();
                    }
                }
                else if (_btnExit.Contains(_currentMouse.Position))
                {
                    GameStateManager.Instance.ChangeState(new ExplorationState());
                }
            }
            _prevMouse = _currentMouse;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            PlayerData pd = GameStateManager.Instance.PlayerData;
            int repairCost = (int)(pd.CarDamage * 10);
            int viewW = GraphicsDevice.Viewport.Width;
            int viewH = GraphicsDevice.Viewport.Height;
            int cx = viewW / 2;
            int cy = viewH / 2;

            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewW, viewH), new Color(5, 5, 15, 230));
            Rectangle panel = new Rectangle(cx - 250, cy - 200, 500, 420);
            spriteBatch.Draw(_pixel, panel, new Color(20, 20, 40, 180));
            DrawHollowRect(spriteBatch, panel, 2, new Color(0, 255, 255, 50));

            spriteBatch.DrawString(_fontMedium, "GARAGEM DE REPARACAO", new Vector2(cx - 160, cy - 160), Color.Cyan);
            spriteBatch.DrawString(_fontMedium, $"SALDO: {pd.Money} EUR", new Vector2(cx - 90, cy - 110), Color.LimeGreen);

            int barWidth = 360;
            int damageWidth = (int)(barWidth * (pd.CarDamage / 100f));
            Rectangle barRect = new Rectangle(cx - 180, cy - 30, barWidth, 25);
            
            spriteBatch.DrawString(_fontSmall, $"ESTADO DO CARRO: {100 - pd.CarDamage:F0}%", new Vector2(barRect.X, barRect.Y - 25), Color.White);
            
            spriteBatch.Draw(_pixel, barRect, Color.LimeGreen); 
            if (damageWidth > 0)
                spriteBatch.Draw(_pixel, new Rectangle(barRect.Right - damageWidth, barRect.Y, damageWidth, barRect.Height), Color.DarkRed);
            DrawHollowRect(spriteBatch, barRect, 2, Color.White);

            bool canRepair = pd.CarDamage > 0 && pd.Money >= repairCost;
            bool hoverRepair = _btnRepair.Contains(_currentMouse.Position);
            Color repairColor = canRepair ? (hoverRepair ? new Color(60, 180, 60) : Color.LimeGreen) : new Color(50, 50, 50);
            
            spriteBatch.Draw(_pixel, _btnRepair, repairColor);
            if (canRepair) DrawHollowRect(spriteBatch, _btnRepair, 2, Color.White);
            
            string repairText = pd.CarDamage > 0 ? $"REPARAR ({repairCost} EUR)" : "CARRO IMPECAVEL";
            Vector2 textPos = new Vector2(_btnRepair.X + (_btnRepair.Width / 2) - (_fontMedium.MeasureString(repairText).X / 2), _btnRepair.Y + 15);
            spriteBatch.DrawString(_fontMedium, repairText, textPos, canRepair ? Color.Black : Color.Gray);

            bool hoverExit = _btnExit.Contains(_currentMouse.Position);
            spriteBatch.Draw(_pixel, _btnExit, hoverExit ? Color.White : Color.DarkRed);
            spriteBatch.DrawString(_fontMedium, "VOLTAR", new Vector2(_btnExit.X + 55, _btnExit.Y + 12), hoverExit ? Color.Black : Color.White);
        }

        private void DrawHollowRect(SpriteBatch sb, Rectangle rect, int thickness, Color color)
        {
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}