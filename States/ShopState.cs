using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BurnoutCity.States
{
    // Estado da loja de peças: permite comprar upgrades de Motor, Pneus, Turbo e Nitro.
    // Os efeitos dos upgrades são aplicados ao construir CarStats em ExplorationState
    // e ao calcular as velocidades por mudança no RaceState.
    public class ShopState : BaseState
    {
        private SpriteFont _fontMedium;
        private SpriteFont _fontSmall;
        private Texture2D _pixel;
        private MouseState _currentMouse;
        private MouseState _prevMouse;

        private Rectangle _btnEngine, _btnTires, _btnTurbo, _btnNitro, _btnExit;
        private Texture2D _iconsTexture;

        // Índice 0 = stock (não comprável); índices 1–4 = tiers T1 a T4
        private readonly int[] _costs = { 0, 2000, 5000, 12000, 25000 };  // custo em EUR por tier
        private readonly int[] _reqs  = { 1, 3, 7, 12, 18 };              // nível mínimo por tier

        // Carrega fontes, cria a textura de pixel e calcula as posições dos 4 botões de upgrade.
        public override void LoadContent()
        {
            _fontMedium = ContentManager.Load<SpriteFont>("Fonts/FontMedium");
            _fontSmall = ContentManager.Load<SpriteFont>("Fonts/FontSmall");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Layout centralizado
            int centerX = GraphicsDevice.Viewport.Width / 2;
            int startY = 180;
            int spacing = 85;

            _btnEngine = new Rectangle(centerX - 250, startY, 500, 70);
            _btnTires  = new Rectangle(centerX - 250, startY + spacing, 500, 70);
            _btnTurbo  = new Rectangle(centerX - 250, startY + (spacing * 2), 500, 70);
            _btnNitro  = new Rectangle(centerX - 250, startY + (spacing * 3), 500, 70);
            _btnExit   = new Rectangle(centerX - 100, startY + (spacing * 4) + 20, 200, 50);
            
            try {
                _iconsTexture = ContentManager.Load<Texture2D>("Sprites/Icons/ico");
            } catch {
                _iconsTexture = null;
            }
        }

        // Deteta cliques nos botões de upgrade e no botão de saída.
        public override void Update(GameTime gameTime)
        {
            _currentMouse = Mouse.GetState();
            PlayerData pd = GameStateManager.Instance.PlayerData;

            if (_currentMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                TryUpgrade(_currentMouse.Position, _btnEngine, pd.EngineLevel, (cost) => pd.UpgradeEngine(cost));
                TryUpgrade(_currentMouse.Position, _btnTires, pd.TiresLevel, (cost) => pd.UpgradeTires(cost));
                TryUpgrade(_currentMouse.Position, _btnTurbo, pd.TurboLevel, (cost) => pd.UpgradeTurbo(cost));
                TryUpgrade(_currentMouse.Position, _btnNitro, pd.NitroLevel, (cost) => pd.UpgradeNitro(cost));

                if (_btnExit.Contains(_currentMouse.Position))
                    GameStateManager.Instance.ChangeState(new ExplorationState());
            }
            _prevMouse = _currentMouse;
        }

        // Verifica se o botão foi clicado, o nível e o dinheiro são suficientes, e executa a compra.
        private void TryUpgrade(Point mousePos, Rectangle btn, int currentLvl, Action<int> upgradeAction)
        {
            PlayerData pd = GameStateManager.Instance.PlayerData;
            if (btn.Contains(mousePos) && currentLvl < 4)
            {
                int next = currentLvl + 1;
                if (pd.Level >= _reqs[next] && pd.Money >= _costs[next])
                    upgradeAction(_costs[next]);
            }
        }

        // Desenha o painel da loja: fundo, cabeçalho, saldo e os 4 botões de upgrade.
        public override void Draw(SpriteBatch spriteBatch)
        {
            PlayerData pd = GameStateManager.Instance.PlayerData;
            int viewW = GraphicsDevice.Viewport.Width;
            int viewH = GraphicsDevice.Viewport.Height;

            // 1. Fundo com gradiente/escurecimento
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewW, viewH), new Color(5, 5, 15, 230));
            
            // 2. Painel Central (Glassmorphism effect)
            spriteBatch.Draw(_pixel, new Rectangle(viewW/2 - 300, 50, 600, viewH - 100), new Color(20, 20, 40, 180));
            // Borda do painel
            DrawHollowRect(spriteBatch, new Rectangle(viewW/2 - 300, 50, 600, viewH - 100), 2, new Color(0, 255, 255, 50));

            // 3. Cabeçalho
            spriteBatch.DrawString(_fontMedium, "PERFORMANCE SHOP", new Vector2(viewW/2 - 140, 80), Color.Cyan);
            string moneyText = $"SALDO: {pd.Money} EUR";
            spriteBatch.DrawString(_fontMedium, moneyText, new Vector2(viewW/2 - 100, 120), Color.LimeGreen);

            // 4. Botões
            DrawPrettyBtn(spriteBatch, _btnEngine, "MOTOR", pd.EngineLevel, 0);
            DrawPrettyBtn(spriteBatch, _btnTires, "PNEUS", pd.TiresLevel, 1);
            DrawPrettyBtn(spriteBatch, _btnTurbo, "TURBO", pd.TurboLevel, 2);
            DrawPrettyBtn(spriteBatch, _btnNitro, "NITRO", pd.NitroLevel, 3);

            // 5. Botão Sair
            bool exitHover = _btnExit.Contains(_currentMouse.Position);
            spriteBatch.Draw(_pixel, _btnExit, exitHover ? Color.White : Color.DarkRed);
            spriteBatch.DrawString(_fontMedium, "VOLTAR", new Vector2(_btnExit.X + 55, _btnExit.Y + 12), exitHover ? Color.Black : Color.White);
        }

        // Desenha um botão de upgrade com fundo dinâmico (bloqueado/acessível/max),
        // ícone, nome, preço e 4 quadradinhos de progresso de tier.
        private void DrawPrettyBtn(SpriteBatch sb, Rectangle rect, string name, int currentLvl, int iconIndex)
        {
            PlayerData pd = GameStateManager.Instance.PlayerData;
            bool isHover = rect.Contains(_currentMouse.Position);
            
            int next = currentLvl + 1;
            bool isMax = currentLvl >= 4;
            bool locked = !isMax && pd.Level < _reqs[next];
            bool canAfford = !isMax && !locked && pd.Money >= _costs[next];

            // Cor de fundo dinâmica
            Color bgColor = new Color(30, 30, 50, 255);
            if (isMax) bgColor = new Color(50, 45, 10);
            else if (locked) bgColor = new Color(20, 10, 10);
            else if (isHover && canAfford) bgColor = new Color(40, 40, 80);

            // Desenhar fundo do botão
            sb.Draw(_pixel, rect, bgColor);
            
            // Desenhar borda neon se puder comprar
            if (canAfford && !isMax)
                DrawHollowRect(sb, rect, 2, isHover ? Color.Cyan : new Color(0, 200, 255, 100));

            // Ícone
            if (_iconsTexture != null)
            {
                int iconSize = _iconsTexture.Width / 2;
                Rectangle src = new Rectangle((iconIndex % 2) * iconSize, (iconIndex / 2) * iconSize, iconSize, iconSize);
                sb.Draw(_iconsTexture, new Rectangle(rect.X + 15, rect.Y + 10, 50, 50), src, Color.White);
            }

            // Textos
            string priceText = isMax ? "MAX OUT" : (locked ? $"BLOQUEADO (LVL {_reqs[next]})" : $"{_costs[next]:N0} EUR");
            Color priceColor = isMax ? Color.Gold : (locked ? Color.Red : Color.LimeGreen);

            sb.DrawString(_fontMedium, name, new Vector2(rect.X + 80, rect.Y + 10), Color.White);
            sb.DrawString(_fontSmall, priceText, new Vector2(rect.X + 80, rect.Y + 40), priceColor);

            // Barras de progresso (4 quadradinhos)
            for (int i = 1; i <= 4; i++)
            {
                Color barCol = (i <= currentLvl) ? Color.Cyan : new Color(50, 50, 50);
                sb.Draw(_pixel, new Rectangle(rect.Right - 110 + (i * 20), rect.Y + 25, 15, 15), barCol);
            }
        }

        // Desenha apenas a borda (4 lados) de um retângulo com a espessura indicada.
        private void DrawHollowRect(SpriteBatch sb, Rectangle rect, int thickness, Color color)
        {
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color); // Cima
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color); // Baixo
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color); // Esquerda
            sb.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color); // Direita
        }
    }
}