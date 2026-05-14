using System;
using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.States
{
    public class CustomizationState : BaseState
    {
        private SpriteFont _fontMedium, _fontSmall;
        private Texture2D _pixel, _carTexture;
        private MouseState _currentMouse, _prevMouse;

        // PREVIEW (O que o jogador está a ver, mas ainda não comprou)
        private int _previewColor;
        private int _previewBodykit;
        private int _previewRim;

        // Custos
        private int _colorCost = 500;
        private int[] _kitCosts = { 0, 2500, 5500 };
        private int[] _rimCosts = { 0, 1200, 2500, 4000 };

        // Layout (calculado dinamicamente no LoadContent)
        private Rectangle[] _colorBtns = new Rectangle[8];
        private Rectangle[] _bodykitBtns = new Rectangle[3];
        private Rectangle[] _rimBtns = new Rectangle[4];
        private Rectangle _btnApply, _btnExit;

        private Color[] _colors = { 
            Color.OrangeRed, Color.Blue, Color.Lime, Color.Yellow, 
            Color.Purple, Color.White, Color.Black, Color.Cyan 
        };

        public override void LoadContent()
        {
            _fontMedium = ContentManager.Load<SpriteFont>("Fonts/FontMedium");
            try { _fontSmall = ContentManager.Load<SpriteFont>("Fonts/FontSmall"); } 
            catch { _fontSmall = _fontMedium; }

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            try { _carTexture = ContentManager.Load<Texture2D>("Sprites/CarSprites/car"); } 
            catch { _carTexture = null; }

            // Começa a preview com o que já tens equipado
            PlayerData pd = GameStateManager.Instance.PlayerData;
            _previewColor = pd.CarColorIndex;
            _previewBodykit = pd.BodykitIndex;
            _previewRim = pd.RimStyleIndex;

            int cx = GraphicsDevice.Viewport.Width / 2;
            int cy = GraphicsDevice.Viewport.Height / 2;

            // --- Geometria do Painel Esquerdo ---
            int leftX = cx - 450;
            int leftY = cy - 250;
            
            // Cores (2 linhas de 4)
            for (int i = 0; i < 8; i++) 
                _colorBtns[i] = new Rectangle(leftX + 30 + ((i % 4) * 95), leftY + 80 + ((i / 4) * 70), 80, 55);
            
            // Bodykits (1 linha de 3)
            for (int i = 0; i < 3; i++) 
                _bodykitBtns[i] = new Rectangle(leftX + 30 + (i * 125), leftY + 270, 115, 60);
            
            // Jantes (1 linha de 4)
            for (int i = 0; i < 4; i++) 
                _rimBtns[i] = new Rectangle(leftX + 30 + (i * 95), leftY + 410, 85, 60);

            // Botões Fixos
            _btnApply = new Rectangle(cx + 60, cy + 160, 300, 60);
            _btnExit = new Rectangle(20, 20, 120, 45);
        }

        public override void Update(GameTime gameTime)
        {
            _currentMouse = Mouse.GetState();
            PlayerData pd = GameStateManager.Instance.PlayerData;

            // Calcular Fatura
            int totalCost = 0;
            if (_previewColor != pd.CarColorIndex) totalCost += _colorCost;
            if (_previewBodykit != pd.BodykitIndex) totalCost += _kitCosts[_previewBodykit];
            if (_previewRim != pd.RimStyleIndex) totalCost += _rimCosts[_previewRim];

            if (_currentMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                // Mudar Previews (NÃO gasta dinheiro)
                for (int i = 0; i < 8; i++) if (_colorBtns[i].Contains(_currentMouse.Position)) _previewColor = i;
                for (int i = 0; i < 3; i++) if (_bodykitBtns[i].Contains(_currentMouse.Position)) _previewBodykit = i;
                for (int i = 0; i < 4; i++) if (_rimBtns[i].Contains(_currentMouse.Position)) _previewRim = i;

                // COMPRAR E APLICAR
                if (_btnApply.Contains(_currentMouse.Position))
                {
                    if (totalCost > 0 && pd.Money >= totalCost)
                    {
                        pd.SpendMoney(totalCost);
                        pd.SetCarColor(_previewColor);
                        pd.SetBodykit(_previewBodykit);
                        pd.SetRimStyle(_previewRim);
                    }
                    else if (totalCost == 0) // Restaurar preview para o que já estava equipado
                    {
                        pd.SetCarColor(_previewColor);
                        pd.SetBodykit(_previewBodykit);
                        pd.SetRimStyle(_previewRim);
                    }
                }

                if (_btnExit.Contains(_currentMouse.Position))
                    GameStateManager.Instance.ChangeState(new ExplorationState());
            }
            _prevMouse = _currentMouse;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            PlayerData pd = GameStateManager.Instance.PlayerData;
            int cx = GraphicsDevice.Viewport.Width / 2;
            int cy = GraphicsDevice.Viewport.Height / 2;

            int totalCost = 0;
            if (_previewColor != pd.CarColorIndex) totalCost += _colorCost;
            if (_previewBodykit != pd.BodykitIndex) totalCost += _kitCosts[_previewBodykit];
            if (_previewRim != pd.RimStyleIndex) totalCost += _rimCosts[_previewRim];

            // Fundo da Tela
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), new Color(15, 15, 25, 255));

            // ==========================================
            // PAINEL ESQUERDO: SELEÇÃO
            // ==========================================
            Rectangle leftPanel = new Rectangle(cx - 450, cy - 250, 430, 520);
            spriteBatch.Draw(_pixel, leftPanel, new Color(30, 30, 40, 240));
            DrawHollowRect(spriteBatch, leftPanel, 2, new Color(80, 80, 100));

            spriteBatch.DrawString(_fontMedium, "PECAS E PINTURA", new Vector2(leftPanel.X + 30, leftPanel.Y + 15), Color.Cyan);

            // 1. Cores (Apenas quadrados pintados para ficar limpo)
            DrawTitle(spriteBatch, "COR PRINCIPAL (500 EUR)", leftPanel.X + 30, leftPanel.Y + 50);
            for (int i = 0; i < 8; i++) 
            {
                spriteBatch.Draw(_pixel, _colorBtns[i], _colors[i]);
                if (_previewColor == i) DrawHollowRect(spriteBatch, _colorBtns[i], 4, Color.White); // Selecionado
                
                if (pd.CarColorIndex == i) // Marca visual para o que já está comprado
                {
                    Vector2 eqpSize = _fontSmall.MeasureString("EQP");
                    spriteBatch.DrawString(_fontSmall, "EQP", new Vector2(_colorBtns[i].X + (_colorBtns[i].Width / 2) - (eqpSize.X / 2), _colorBtns[i].Y + 20), Color.Black);
                }
            }

            // 2. Bodykits
            DrawTitle(spriteBatch, "KIT DE CARROCARIA", leftPanel.X + 30, leftPanel.Y + 240);
            for (int i = 0; i < 3; i++) DrawCenteredBtn(spriteBatch, _bodykitBtns[i], $"KIT {i+1}", _kitCosts[i], _previewBodykit == i, pd.BodykitIndex == i);

            // 3. Jantes
            DrawTitle(spriteBatch, "JANTES", leftPanel.X + 30, leftPanel.Y + 380);
            for (int i = 0; i < 4; i++) DrawCenteredBtn(spriteBatch, _rimBtns[i], $"MOD {i+1}", _rimCosts[i], _previewRim == i, pd.RimStyleIndex == i);


            // ==========================================
            // PAINEL DIREITO: PREVIEW E CHECKOUT
            // ==========================================
            Rectangle rightPanel = new Rectangle(cx + 10, cy - 250, 400, 520);
            spriteBatch.Draw(_pixel, rightPanel, new Color(20, 20, 30, 240));
            DrawHollowRect(spriteBatch, rightPanel, 2, new Color(150, 0, 150));

            spriteBatch.DrawString(_fontMedium, "GARAGEM", new Vector2(rightPanel.X + 30, rightPanel.Y + 15), Color.Magenta);
            spriteBatch.DrawString(_fontSmall, $"SALDO: {pd.Money} EUR", new Vector2(rightPanel.X + 30, rightPanel.Y + 45), Color.LimeGreen);

            // --- DESENHAR O CARRO (Mantendo as Proporções) ---
            if (_carTexture != null)
            {
                int maxPreviewSize = 220; // Espaço máximo disponível
                float scale = Math.Min((float)maxPreviewSize / _carTexture.Width, (float)maxPreviewSize / _carTexture.Height);
                int cW = (int)(_carTexture.Width * scale);
                int cH = (int)(_carTexture.Height * scale);
                
                Rectangle carPreviewRect = new Rectangle(rightPanel.X + (rightPanel.Width / 2) - (cW / 2), rightPanel.Y + 110, cW, cH);
                spriteBatch.Draw(_carTexture, carPreviewRect, _colors[_previewColor]);
            }

            // --- CHECKOUT BOTÃO ---
            bool canAfford = pd.Money >= totalCost;
            Color btnColor = (totalCost == 0) ? new Color(80, 80, 80) : (canAfford ? new Color(0, 150, 50) : new Color(180, 40, 40));
            if (_btnApply.Contains(_currentMouse.Position) && canAfford && totalCost > 0) btnColor = new Color(0, 200, 80);

            spriteBatch.Draw(_pixel, _btnApply, btnColor);
            DrawHollowRect(spriteBatch, _btnApply, 2, Color.White);

            string btnText = (totalCost == 0) ? "JA TENS TUDO EQUIPADO" : (canAfford ? "COMPRAR E APLICAR" : "DINHEIRO INSUFICIENTE");
            Vector2 textPos = new Vector2(_btnApply.X + (_btnApply.Width / 2) - (_fontSmall.MeasureString(btnText).X / 2), _btnApply.Y + 12);
            spriteBatch.DrawString(_fontSmall, btnText, textPos, Color.White);

            if (totalCost > 0)
            {
                string costText = $"FATURA: {totalCost} EUR";
                Vector2 costPos = new Vector2(_btnApply.X + (_btnApply.Width / 2) - (_fontMedium.MeasureString(costText).X / 2), _btnApply.Y + 32);
                spriteBatch.DrawString(_fontMedium, costText, costPos, canAfford ? Color.Yellow : new Color(255, 100, 100));
            }

            // ==========================================
            // BOTÃO SAIR
            // ==========================================
            bool hovExit = _btnExit.Contains(_currentMouse.Position);
            spriteBatch.Draw(_pixel, _btnExit, hovExit ? Color.White : new Color(100, 20, 20));
            DrawHollowRect(spriteBatch, _btnExit, 1, Color.White);
            
            string exitTxt = "VOLTAR";
            Vector2 exitPos = new Vector2(_btnExit.X + (_btnExit.Width / 2) - (_fontMedium.MeasureString(exitTxt).X / 2), _btnExit.Y + 12);
            spriteBatch.DrawString(_fontMedium, exitTxt, exitPos, hovExit ? Color.Black : Color.White);
        }

        private void DrawTitle(SpriteBatch sb, string text, int x, int y)
        {
            sb.DrawString(_fontSmall, text, new Vector2(x, y), new Color(180, 180, 180));
        }

        private void DrawCenteredBtn(SpriteBatch sb, Rectangle r, string name, int cost, bool isPreview, bool isEquipped) 
        {
            bool hov = r.Contains(_currentMouse.Position);
            
            Color bgColor = isPreview ? new Color(0, 100, 180) : (hov ? new Color(70, 70, 90) : new Color(40, 40, 60));
            sb.Draw(_pixel, r, bgColor);
            
            if (isPreview) DrawHollowRect(sb, r, 2, Color.White);
            
            // Centrar Nome
            Vector2 nameSize = _fontSmall.MeasureString(name);
            sb.DrawString(_fontSmall, name, new Vector2(r.X + (r.Width / 2) - (nameSize.X / 2), r.Y + 10), Color.White);
            
            // Centrar Preço ou "EQP"
            string subText = isEquipped ? "EQP" : $"{cost} EUR";
            Color subColor = isEquipped ? Color.Yellow : new Color(120, 255, 120);
            
            Vector2 subSize = _fontSmall.MeasureString(subText);
            sb.DrawString(_fontSmall, subText, new Vector2(r.X + (r.Width / 2) - (subSize.X / 2), r.Y + 30), subColor);
        }

        private void DrawHollowRect(SpriteBatch sb, Rectangle r, int t, Color c) 
        {
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, t), c);
            sb.Draw(_pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, t, r.Height), c);
            sb.Draw(_pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }
    }
}