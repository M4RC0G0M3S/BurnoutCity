using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BurnoutCity.Core;

namespace BurnoutCity.States
{
    public class SettingsState : BaseState
    {
        private SpriteFont _fMed;

        public override void LoadContent() 
        { 
            _fMed = ContentManager.Load<SpriteFont>("Fonts/FontMedium"); 
        }

        public override void Update(GameTime gt)
        {
            // Verifica se a tecla ESC foi pressionada para voltar ao menu
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) 
            {
                GameStateManager.Instance.ChangeState(new MenuState());
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            string msg = "DEFINIÇÕES (ESC PARA VOLTAR)";
            Vector2 size = _fMed.MeasureString(msg);
            sb.DrawString(_fMed, msg, new Vector2(640 - size.X/2, 300), Color.White);
            
            sb.DrawString(_fMed, "Volume: 100%", new Vector2(500, 400), Color.Gray);
        }
    }
}