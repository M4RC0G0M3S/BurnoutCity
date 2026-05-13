using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace BurnoutCity
{
    public class HUD
    {
        private SpriteBatch _spriteBatch = null!;
        private GraphicsDevice _graphicsDevice;
        private Texture2D _pixelTexture;

        private SpriteFont _fontBig = null!;
        private SpriteFont _fontMedium = null!;
        private SpriteFont _fontSmall = null!;

        public float Speed         { get; set; } = 0f;
        public float MaxSpeed      { get; set; } = 300f;
        public float Nitro         { get; set; } = 100f;
        public float MaxNitro      { get; set; } = 100f;
        public bool  NitroActive   { get; set; } = false;
        public int   CurrentXP     { get; set; } = 0;
        public int   XPToNextLevel { get; set; } = 1000;
        public int   Level         { get; set; } = 1;
        public int   Money         { get; set; } = 0;

        public enum GameResult { None, Win, Lose }
        public GameResult Result { get; set; } = GameResult.None;
        public string ResultSubtext { get; set; } = "";

        private float _nitroFlashTimer = 0f;
        private float _xpAnimValue     = 0f;
        private float _moneyDisplayed  = 0f;
        private float _overlayAlpha    = 0f;

        private int _screenW;
        private int _screenH;

        public HUD(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            _spriteBatch    = spriteBatch;
            _graphicsDevice = graphicsDevice;
            _screenW        = graphicsDevice.Viewport.Width;
            _screenH        = graphicsDevice.Viewport.Height;

            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        // Atribui as fontes carregadas externamente pelo estado que usa este HUD.
        public void LoadContent(SpriteFont fontBig, SpriteFont fontMedium, SpriteFont fontSmall)
        {
            _fontBig    = fontBig;
            _fontMedium = fontMedium;
            _fontSmall  = fontSmall;
        }

        // Anima os valores do HUD: suaviza a barra de XP, o contador de dinheiro
        // e o fade-in do overlay de resultado (vitória/derrota).
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _nitroFlashTimer += dt * 8f;

            float xpRatio = (XPToNextLevel > 0) ? (float)CurrentXP / XPToNextLevel : 0f;
            _xpAnimValue = MathHelper.Lerp(_xpAnimValue, xpRatio, dt * 5f);

            _moneyDisplayed = MathHelper.Lerp(_moneyDisplayed, Money, dt * 8f);

            float targetAlpha = (Result != GameResult.None) ? 1f : 0f;
            _overlayAlpha = MathHelper.Lerp(_overlayAlpha, targetAlpha, dt * 3f);
        }

        // Recebe o SpriteBatch principal que já tem Begin() chamado
        public void Draw(SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;

            DrawSpeedometer();
            DrawNitroBar();
            DrawXPBar();
            DrawMoneyCounter();

            if (_overlayAlpha > 0.01f)
                DrawResultOverlay();
        }

        // Painel de velocidade no canto inferior direito. A borda muda de cor (verde→amarelo→vermelho) com a velocidade.
        private void DrawSpeedometer()
        {
            int kmh     = (int)Math.Abs(Speed);
            string text = $"{kmh}";
            string unit = "KM/H";

            int panelW = 140, panelH = 70;
            int panelX = _screenW - panelW - 20;
            int panelY = _screenH - panelH - 120;

            DrawPanel(panelX, panelY, panelW, panelH, new Color(0, 0, 0, 180));
            DrawBorder(panelX, panelY, panelW, panelH, 2, SpeedColor(Math.Abs(Speed) / MaxSpeed));

            if (_fontBig != null)
            {
                Vector2 textSize = _fontBig.MeasureString(text);
                Vector2 pos = new Vector2(
                    panelX + (panelW - textSize.X) / 2f,
                    panelY + 6
                );
                _spriteBatch.DrawString(_fontBig, text, pos + Vector2.One, Color.Black * 0.6f);
                _spriteBatch.DrawString(_fontBig, text, pos, Color.White);
            }

            if (_fontSmall != null)
            {
                Vector2 unitSize = _fontSmall.MeasureString(unit);
                Vector2 unitPos = new Vector2(
                    panelX + (panelW - unitSize.X) / 2f,
                    panelY + panelH - 18
                );
                _spriteBatch.DrawString(_fontSmall, unit, unitPos, new Color(180, 180, 180));
            }
        }

        // Barra de nitro abaixo do velocímetro. Pisca a branco quando o nitro está ativo;
        // fica laranja quando o nível desce abaixo de 30%.
        private void DrawNitroBar()
        {
            int barW = 140, barH = 22;
            int barX = _screenW - barW - 20;
            int barY = _screenH - barH - 20;

            DrawPanel(barX, barY, barW, barH, new Color(0, 0, 0, 180));

            float ratio = MathHelper.Clamp(Nitro / MaxNitro, 0f, 1f);
            int fillW = (int)((barW - 4) * ratio);

            Color nitroColor;
            if (NitroActive)
            {
                float flash = (float)(Math.Sin(_nitroFlashTimer) * 0.5 + 0.5);
                nitroColor = Color.Lerp(new Color(0, 180, 255), Color.White, flash);
            }
            else
            {
                nitroColor = ratio > 0.3f ? new Color(0, 140, 255) : new Color(255, 80, 0);
            }

            if (fillW > 0)
                DrawRect(barX + 2, barY + 2, fillW, barH - 4, nitroColor);

            DrawBorder(barX, barY, barW, barH, 2, new Color(60, 60, 60));

            if (_fontSmall != null)
            {
                string label = $"NITRO  {(int)Nitro}%";
                Vector2 labelSize = _fontSmall.MeasureString(label);
                Vector2 labelPos = new Vector2(
                    barX + (barW - labelSize.X) / 2f,
                    barY + (barH - labelSize.Y) / 2f
                );
                _spriteBatch.DrawString(_fontSmall, label, labelPos, Color.White);
            }
        }

        // Barra de XP no canto inferior esquerdo, com o nível atual num painel separado à esquerda.
        // A cor da barra interpola de azul a dourado conforme o progresso dentro do nível.
        private void DrawXPBar()
        {
            int barW = 200, barH = 22;
            int barX = 20;
            int barY = _screenH - barH - 20;

            int lvlPanelW = 50, lvlPanelH = 22;
            DrawPanel(barX, barY, lvlPanelW, lvlPanelH, new Color(0, 0, 0, 180));
            DrawBorder(barX, barY, lvlPanelW, lvlPanelH, 2, new Color(255, 200, 0, 180));

            if (_fontSmall != null)
            {
                string lvlText = $"LV {Level}";
                Vector2 lvlSize = _fontSmall.MeasureString(lvlText);
                Vector2 lvlPos = new Vector2(
                    barX + (lvlPanelW - lvlSize.X) / 2f,
                    barY + (lvlPanelH - lvlSize.Y) / 2f
                );
                _spriteBatch.DrawString(_fontSmall, lvlText, lvlPos, new Color(255, 200, 0));
            }

            int xpBarX = barX + lvlPanelW + 4;
            int xpBarW = barW - lvlPanelW - 4;

            DrawPanel(xpBarX, barY, xpBarW, barH, new Color(0, 0, 0, 180));

            int fillW = (int)((xpBarW - 4) * MathHelper.Clamp(_xpAnimValue, 0f, 1f));
            if (fillW > 0)
            {
                Color xpColor = Color.Lerp(new Color(80, 80, 255), new Color(255, 200, 0), _xpAnimValue);
                DrawRect(xpBarX + 2, barY + 2, fillW, barH - 4, xpColor);
            }

            DrawBorder(xpBarX, barY, xpBarW, barH, 2, new Color(60, 60, 60));

            if (_fontSmall != null)
            {
                string xpText = $"XP  {CurrentXP}/{XPToNextLevel}";
                Vector2 xpSize = _fontSmall.MeasureString(xpText);
                Vector2 xpPos = new Vector2(
                    xpBarX + (xpBarW - xpSize.X) / 2f,
                    barY + (barH - xpSize.Y) / 2f
                );
                _spriteBatch.DrawString(_fontSmall, xpText, xpPos, Color.White);
            }
        }

        // Contador de dinheiro no canto superior direito. O valor animado suaviza para o valor real.
        private void DrawMoneyCounter()
        {
            int panelW = 160, panelH = 40;
            int panelX = _screenW - panelW - 20;
            int panelY = 20;

            DrawPanel(panelX, panelY, panelW, panelH, new Color(0, 0, 0, 180));
            DrawBorder(panelX, panelY, panelW, panelH, 2, new Color(0, 200, 80, 200));

            if (_fontMedium != null)
            {
                string moneyText = $"$ {(int)_moneyDisplayed}";
                Vector2 textSize = _fontMedium.MeasureString(moneyText);
                Vector2 pos = new Vector2(
                    panelX + (panelW - textSize.X) / 2f,
                    panelY + (panelH - textSize.Y) / 2f
                );
                _spriteBatch.DrawString(_fontMedium, moneyText, pos + Vector2.One, Color.Black * 0.5f);
                _spriteBatch.DrawString(_fontMedium, moneyText, pos, new Color(0, 230, 100));
            }
        }

        // Overlay centrado de vitória/derrota com fade-in. Só aparece quando Result != None.
        private void DrawResultOverlay()
        {
            DrawRect(0, 0, _screenW, _screenH, new Color(0, 0, 0) * (_overlayAlpha * 0.65f));

            bool isWin = (Result == GameResult.Win);

            int panelW = 420, panelH = 200;
            int panelX = (_screenW - panelW) / 2;
            int panelY = (_screenH - panelH) / 2;

            Color accentColor = isWin ? new Color(0, 220, 100) : new Color(220, 40, 40);

            DrawPanel(panelX, panelY, panelW, panelH, new Color(10, 10, 10) * _overlayAlpha);
            DrawBorder(panelX, panelY, panelW, panelH, 3, accentColor * _overlayAlpha);

            if (_fontBig != null)
            {
                string title = isWin ? "VITORIA!" : "FIM DE JOGO";
                Vector2 titleSize = _fontBig.MeasureString(title);
                Vector2 titlePos = new Vector2(
                    panelX + (panelW - titleSize.X) / 2f,
                    panelY + 30
                );
                _spriteBatch.DrawString(_fontBig, title, titlePos + new Vector2(2, 2), Color.Black * _overlayAlpha);
                _spriteBatch.DrawString(_fontBig, title, titlePos, accentColor * _overlayAlpha);
            }

            if (_fontMedium != null && !string.IsNullOrEmpty(ResultSubtext))
            {
                Vector2 subSize = _fontMedium.MeasureString(ResultSubtext);
                Vector2 subPos = new Vector2(
                    panelX + (panelW - subSize.X) / 2f,
                    panelY + panelH - 70
                );
                _spriteBatch.DrawString(_fontMedium, ResultSubtext, subPos, Color.White * _overlayAlpha);
            }

            if (_fontSmall != null)
            {
                string hint = "Prima ENTER para continuar";
                Vector2 hintSize = _fontSmall.MeasureString(hint);
                Vector2 hintPos = new Vector2(
                    panelX + (panelW - hintSize.X) / 2f,
                    panelY + panelH - 30
                );
                float blink = (float)(Math.Sin(_nitroFlashTimer * 0.5f) * 0.5 + 0.5);
                _spriteBatch.DrawString(_fontSmall, hint, hintPos, Color.White * _overlayAlpha * blink);
            }
        }

        // Desenha um retângulo sólido colorido (helper base de todos os elementos do HUD).
        private void DrawRect(int x, int y, int w, int h, Color color)
        {
            if (w <= 0 || h <= 0) return;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, w, h), color);
        }

        // Desenha o fundo de um painel (alias de DrawRect para clareza semântica).
        private void DrawPanel(int x, int y, int w, int h, Color bgColor)
        {
            DrawRect(x, y, w, h, bgColor);
        }

        // Desenha os 4 lados de um retângulo com a espessura indicada (borda oca).
        private void DrawBorder(int x, int y, int w, int h, int thickness, Color color)
        {
            DrawRect(x, y, w, thickness, color);
            DrawRect(x, y + h - thickness, w, thickness, color);
            DrawRect(x, y, thickness, h, color);
            DrawRect(x + w - thickness, y, thickness, h, color);
        }

        // Devolve verde→amarelo→vermelho conforme o rácio de velocidade (0 = parado, 1 = max).
        private Color SpeedColor(float ratio)
        {
            ratio = MathHelper.Clamp(ratio, 0f, 1f);
            if (ratio < 0.5f)
                return Color.Lerp(new Color(0, 200, 80), new Color(255, 200, 0), ratio * 2f);
            else
                return Color.Lerp(new Color(255, 200, 0), new Color(255, 40, 40), (ratio - 0.5f) * 2f);
        }

        public void Dispose()
        {
            _pixelTexture?.Dispose();
        }
    }
}
