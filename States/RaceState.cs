using System;
using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.States
{
    public class RaceState : BaseState
    {
        // ── Física ────────────────────────────────────────────────────
        private const int MaxGears = 6;
        private const float GearRevCycle = 1.8f;
        private const float NitroMaxCharge = 100f;
        private const float NitroDrainRate = 40f;
        private const float NitroRechargeRate = 12f;
        private const float NitroBoostMult = 1.55f;
        private const float PixelsToProgress = 1f / 1280f;

        private static readonly float[] GearTopSpeeds = { 0f, 120f, 210f, 290f, 360f, 420f, 480f };
        private static readonly float[] GearAccel = { 0f, 280f, 220f, 170f, 130f, 95f, 70f };

        // ── Pista (dimensões reais em píxeis) ─────────────────────────
        private const int TrackRealW = 2000;
        private const int TrackRealH = 222;
        private const int TrackY = 249;  // (720/2) - (222/2) = pista centrada verticalmente

        private const int RivalLaneY = TrackY + 55;
        private const int PlayerLaneY = TrackY + 167;

        // ── Carro horizontal (W = comprimento, H = largura) ───────────
        private const int CarW = 54;
        private const int CarH = 28;

        // ── Fases ─────────────────────────────────────────────────────
        private enum RacePhase { Preview, Countdown, Racing, Finished }
        private RacePhase _phase = RacePhase.Preview;

        // ── Câmara ────────────────────────────────────────────────────
        private Vector2 _camPos = Vector2.Zero;

        // ── Jogador ───────────────────────────────────────────────────
        private float _playerProgress = 0f;
        private float _playerSpeed = 0f;
        private int _currentGear = 1;
        private float _revTimer = 0f;
        private float _nitroCharge = NitroMaxCharge;
        private bool _nitroActive = false;
        private bool _finished = false;
        private bool _playerWon = false;

        // ── Shift feedback ────────────────────────────────────────────
        private float _shiftFeedbackTimer = 0f;
        private string _shiftFeedbackText = "";
        private Color _shiftFeedbackColor = Color.White;

        // ── Rival ─────────────────────────────────────────────────────
        private float _rivalProgress = 0f;
        private float _rivalSpeed = 0f;
        private float _rivalMaxSpeed = 350f;
        private float _rivalAccel = 140f;
        private string _rivalName = "Rival";
        private Color _rivalColor = Color.DeepSkyBlue;
        private float _rivalGearTimer = 0f;
        private int _rivalGear = 1;
        private float _rivalNoise = 0f;
        private readonly Random _rng = new();


        // ── Countdown ─────────────────────────────────────────────────
        private float _countdownTimer = 1.0f;
        private int _countdownStep = 0;
        private float _countdownScale = 1f;

        // ── Preview ───────────────────────────────────────────────────
        private float _previewTimer = 2.5f;

        // ── Resultado ─────────────────────────────────────────────────
        private float _resultTimer = 0f;
        private const float ResultDisplayTime = 4f;
        private LevelUpInfo _levelUpInfo;

        // ── Input ─────────────────────────────────────────────────────
        private KeyboardState _prevKb;

        // ── Texturas ──────────────────────────────────────────────────
        private Texture2D _pixel = null!;
        private Texture2D _texPista = null!;
        private Texture2D _texBancCima = null!;
        private Texture2D _texBancBaixo = null!;
        private Texture2D _texSemVerm = null!;
        private Texture2D _texSemAm1 = null!;
        private Texture2D _texSemAm2 = null!;
        private Texture2D _texSemAm3 = null!;
        private Texture2D _texSemVerde = null!;
        private SpriteFont? _font = null;

        // ── PlayerData ────────────────────────────────────────────────
        private PlayerData? _playerData;

        // ═════════════════════════════════════════════════════════════
        public RaceState(PlayerData? playerData = null,
                         string rivalName = "Rival",
                         Color? rivalColor = null,
                         float rivalMaxSpeed = 350f,
                         float rivalAccel = 140f)
        {
            _playerData = GameStateManager.Instance.PlayerData;
            _rivalName = rivalName;
            _rivalColor = rivalColor ?? Color.DeepSkyBlue;
            _rivalMaxSpeed = rivalMaxSpeed;
            _rivalAccel = rivalAccel;
        }

        // ═════════════════════════════════════════════════════════════
        //  LOAD CONTENT
        // ═════════════════════════════════════════════════════════════
        public override void LoadContent()
        {
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _texPista = TryLoad("Sprites/World/Track/pista_completa", "Sprites/World/Track/pista completa");
            _texBancCima = TryLoad("Sprites/World/Track/Bancada_Cima", "Sprites/World/Track/Bancada Cima");
            _texBancBaixo = TryLoad("Sprites/World/Track/Bancada_Baixo", "Sprites/World/Track/Bancada Baixo");
            _texSemVerm = TryLoad("Sprites/World/Track/vermelha", "Sprites/World/Track/vermelha");
            _texSemAm1 = TryLoad("Sprites/World/Track/amarelo_1", "Sprites/World/Track/amarelo 1");
            _texSemAm2 = TryLoad("Sprites/World/Track/amarelo_2", "Sprites/World/Track/amarelo 2");
            _texSemAm3 = TryLoad("Sprites/World/Track/amarelo_3", "Sprites/World/Track/amarelo 3");
            _texSemVerde = TryLoad("Sprites/World/Track/verde", "Sprites/World/Track/verde");

            Console.WriteLine($"[Race] pista={(_texPista.Width > 1 ? "OK" : "FAIL")} " +
                              $"bancCima={(_texBancCima.Width > 1 ? "OK" : "FAIL")} " +
                              $"semVerm={(_texSemVerm.Width > 1 ? "OK" : "FAIL")}");

            try { _font = ContentManager.Load<SpriteFont>("Fonts/RaceFont"); }
            catch { _font = null; }

            // Câmara começa no início da pista — clamped ao limite esquerdo
            // Com viewport 1280, halfView=640, minX=640 → câmara começa em x=640
            _camPos.X = 640f;   // = viewW/2, mostra o início da pista sem sair para a esquerda
            _camPos.Y = TrackY + TrackRealH / 2f;
        }

        private Texture2D TryLoad(string p1, string p2)
        {
            try { var t = ContentManager.Load<Texture2D>(p1); Console.WriteLine($"[Race] OK: {p1}"); return t; }
            catch { }
            try { var t = ContentManager.Load<Texture2D>(p2); Console.WriteLine($"[Race] OK: {p2}"); return t; }
            catch { }
            Console.WriteLine($"[Race] FAIL: {p1}");
            var err = new Texture2D(GraphicsDevice, 1, 1);
            err.SetData(new[] { Color.Magenta });
            return err;
        }

        // ═════════════════════════════════════════════════════════════
        //  UPDATE
        // ═════════════════════════════════════════════════════════════
        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var kb = Keyboard.GetState();

            switch (_phase)
            {
                case RacePhase.Preview: UpdatePreview(dt, kb); break;
                case RacePhase.Countdown: UpdateCountdown(dt, kb); break;
                case RacePhase.Racing: UpdateRacing(dt, kb); break;
                case RacePhase.Finished: UpdateFinished(dt, kb); break;
            }
            _prevKb = kb;
        }

        private void UpdatePreview(float dt, KeyboardState kb)
        {
            _previewTimer -= dt;
            bool go = (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space)) ||
                      (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter));
            if (_previewTimer <= 0f || go)
            { _phase = RacePhase.Countdown; _countdownStep = 0; _countdownTimer = 3.0f; }
        }

        private void UpdateCountdown(float dt, KeyboardState kb)
        {
            _countdownTimer -= dt;
            _countdownScale = 1f + (1f - (_countdownTimer % 1f)) * 0.5f;
            if (_countdownTimer <= 0f)
            {
                _countdownStep++;
                _countdownTimer = _countdownStep switch
                {
                    1 or 2 or 3 => 0.8f,
                    4 => 0.8f,
                    _ => 0f
                };
                if (_countdownStep >= 5) _phase = RacePhase.Racing;
            }
        }

        private void UpdateRacing(float dt, KeyboardState kb)
        {
            if (_finished) return;
            UpdatePlayerPhysics(dt, kb);
            UpdateRivalAI(dt);
            if (_shiftFeedbackTimer > 0f) _shiftFeedbackTimer -= dt;
            UpdateCam();

            if (_playerProgress >= 1f && !_finished)
            { _finished = true; _playerWon = true; FinishRace(); }
            else if (_rivalProgress >= 1f && !_finished)
            { _finished = true; _playerWon = false; FinishRace(); }
        }

        private void UpdateCam()
        {
            int viewW = GraphicsDevice.Viewport.Width;  // 1280
            float halfView = viewW / 2f;

            // Posição X do carro no mundo
            float px = 60f + _playerProgress * (TrackRealW - 120f);

            // Clamp: câmara nunca mostra fora dos limites da pista (x=0 .. x=TrackRealW)
            float minX = halfView;                // nunca vai à esquerda do início
            float maxX = TrackRealW - halfView;   // nunca vai à direita do fim
            float target = MathHelper.Clamp(px, minX, maxX);

            _camPos.X = MathHelper.Lerp(_camPos.X, target, 0.15f);
            _camPos.Y = TrackY + TrackRealH / 2f;
        }

        private void UpdatePlayerPhysics(float dt, KeyboardState kb)
        {
            bool w = kb.IsKeyDown(Keys.W);
            bool sp = kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space);
            bool n = kb.IsKeyDown(Keys.N);

            if (w && _currentGear > 0)
            {
                float top = GearTopSpeeds[_currentGear] * (_nitroActive ? NitroBoostMult : 1f);
                _playerSpeed = _playerSpeed < top
                    ? (float)Math.Min(_playerSpeed + GearAccel[_currentGear] * dt, top)
                    : MathHelper.Lerp(_playerSpeed, top, 0.1f);
            }
            else _playerSpeed = (float)Math.Max(0f, _playerSpeed - 60f * dt);

            _revTimer = (float)Math.Min(_revTimer + dt, GearRevCycle - 0.01f);

            if (sp && _currentGear < MaxGears) ProcessGearShift(_revTimer / GearRevCycle);

            _nitroActive = n && _nitroCharge > 0f;
            _nitroCharge = _nitroActive
                ? (float)Math.Max(0f, _nitroCharge - NitroDrainRate * dt)
                : (float)Math.Min(NitroMaxCharge, _nitroCharge + NitroRechargeRate * dt);

            float spd = _playerSpeed * (_nitroActive ? NitroBoostMult : 1f);
            _playerProgress = (float)Math.Min(1f, _playerProgress + spd * PixelsToProgress * dt);
        }

        private void ProcessGearShift(float ratio)
        {
            if (ratio >= 0.55f && ratio <= 0.80f)
            { _playerSpeed *= 1.08f; _shiftFeedbackText = "PERFECT!"; _shiftFeedbackColor = new Color(50, 255, 80); }
            else if ((ratio >= 0.40f && ratio < 0.55f) || (ratio > 0.80f && ratio <= 0.92f))
            { _shiftFeedbackText = "GOOD"; _shiftFeedbackColor = new Color(255, 200, 50); }
            else
            { _playerSpeed *= 0.82f; _shiftFeedbackText = "MISS!"; _shiftFeedbackColor = new Color(255, 60, 60); }

            _currentGear++; _revTimer = 0f; _shiftFeedbackTimer = 1.2f;
            Console.WriteLine($"[Race] Gear {_currentGear} ratio:{ratio:F2} {_shiftFeedbackText}");
        }

        private void UpdateRivalAI(float dt)
        {
            _rivalNoise = MathHelper.Lerp(_rivalNoise, (float)(_rng.NextDouble() * 20 - 10), 0.05f);
            _rivalGearTimer += dt;
            if (_rivalGearTimer >= 1.5f && _rivalGear < MaxGears) { _rivalGear++; _rivalGearTimer = 0f; }
            float top = GearTopSpeeds[Math.Min(_rivalGear, MaxGears)] * (_rivalMaxSpeed / GearTopSpeeds[MaxGears]) + _rivalNoise;
            _rivalSpeed = _rivalSpeed < top
                ? (float)Math.Min(_rivalSpeed + _rivalAccel * dt, top)
                : MathHelper.Lerp(_rivalSpeed, top, 0.1f);
            _rivalSpeed = (float)Math.Max(0f, _rivalSpeed);
            _rivalProgress = (float)Math.Min(1f, _rivalProgress + _rivalSpeed * PixelsToProgress * dt);
        }

        private void FinishRace()
        {
            _phase = RacePhase.Finished;
            _resultTimer = ResultDisplayTime;

            if (_playerData != null)
            {
                _levelUpInfo = _playerData.RegisterRaceResult(_playerWon);
                SaveManager.Instance.AutoSaveAfterRace(_playerData, 0f, 0f);
            }

            Console.WriteLine($"[Race] {(_playerWon ? "VITORIA" : "DERROTA")}");
        }   

        private void UpdateFinished(float dt, KeyboardState kb)
        {
            _resultTimer -= dt;
            bool any = (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)) ||
                       (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space));
            if (_resultTimer <= 0f || any)
                GameStateManager.Instance.ChangeState(new ExplorationState());
        }

        // ═════════════════════════════════════════════════════════════
        //  DRAW
        // ═════════════════════════════════════════════════════════════
        public override void Draw(SpriteBatch sb)
        {
            int W = GraphicsDevice.Viewport.Width;
            int H = GraphicsDevice.Viewport.Height;

            // Fundo escuro (sem câmara)
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(8, 8, 14));

            // ── Espaço mundo com câmara ────────────────────────────────
            Matrix cam = Matrix.CreateTranslation(-_camPos.X + W / 2f, -_camPos.Y + H / 2f, 0f);
            sb.End();
            sb.Begin(transformMatrix: cam);

            // Bancada cima
            if (_texBancCima.Width > 1)
            {
                float sx = (float)TrackRealW / _texBancCima.Width;
                int bh = (int)(_texBancCima.Height * sx);
                sb.Draw(_texBancCima, new Rectangle(0, TrackY - bh, TrackRealW, bh), Color.White);
            }

            // Pista
            if (_texPista.Width > 1)
                sb.Draw(_texPista, new Rectangle(0, TrackY, TrackRealW, TrackRealH), Color.White);
            else
            {
                sb.Draw(_pixel, new Rectangle(0, TrackY, TrackRealW, TrackRealH), new Color(28, 28, 36));
                sb.Draw(_pixel, new Rectangle(0, TrackY, TrackRealW, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackY + TrackRealH - 4, TrackRealW, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackY + TrackRealH / 2 - 2, TrackRealW, 4), new Color(200, 200, 200, 80));
            }

            // Bancada baixo
            if (_texBancBaixo.Width > 1)
            {
                float sx = (float)TrackRealW / _texBancBaixo.Width;
                int bh = (int)(_texBancBaixo.Height * sx);
                sb.Draw(_texBancBaixo, new Rectangle(0, TrackY + TrackRealH, TrackRealW, bh), Color.White);
            }

            // Carros (horizontais na pista)
            float usable = TrackRealW - 120f;
            int pCarX = 60 + (int)(_playerProgress * usable);
            int rCarX = 60 + (int)(_rivalProgress * usable);

            // Rival (pista de cima)
            DrawCar(sb, rCarX, RivalLaneY, _rivalColor, isRival: true);
            // Jogador (pista de baixo)
            DrawCar(sb, pCarX, PlayerLaneY, Color.OrangeRed, isRival: false);

            // Chama do nitro
            if (_nitroActive)
            {
                float ft = (float)(DateTime.Now.Millisecond % 180) / 180f;
                int fl = 28 + (int)(ft * 16);
                int fx = pCarX - CarW / 2 - 2;
                sb.Draw(_pixel, new Rectangle(fx - fl, PlayerLaneY - 5, fl, 10), new Color(255, 70, 0, 200));
                sb.Draw(_pixel, new Rectangle(fx - fl + 7, PlayerLaneY - 3, fl - 7, 6), new Color(255, 170, 0, 180));
                sb.Draw(_pixel, new Rectangle(fx - fl + 14, PlayerLaneY - 2, fl - 14, 4), new Color(255, 255, 160, 150));
            }

            sb.End();
            sb.Begin(); // HUD — espaço de ecrã

            // ── HUD ────────────────────────────────────────────────────
            if (_phase == RacePhase.Preview) { DrawRivalCard(sb, W, H); return; }

            DrawSemaforo(sb, W, H);
            if (_phase == RacePhase.Countdown) DrawGoText(sb, W, H);

            if (_phase == RacePhase.Racing || _phase == RacePhase.Finished)
            {
                DrawGearBar(sb, W, H);
                DrawSpeedometer(sb, W, H);
                DrawNitroBar(sb, W, H);
                DrawProgressBar(sb, W, H);
                DrawGearIndicator(sb, W, H);
                DrawShiftFeedback(sb, W, H);
            }

            if (_phase == RacePhase.Finished) DrawResultScreen(sb, W, H);
        }

        // ── Carro top-down a andar para a direita ─────────────────────
        // CarW = comprimento (eixo X), CarH = largura (eixo Y)
        // Vista de cima: carro mais comprido do que largo, nariz para a direita
        private void DrawCar(SpriteBatch sb, int cx, int cy, Color color, bool isRival)
        {
            int hw = CarW / 2;   // metade do comprimento
            int hh = CarH / 2;   // metade da largura

            // Sombra
            sb.Draw(_pixel, new Rectangle(cx - hw + 4, cy - hh + 4, CarW, CarH), Color.Black * 0.4f);

            // Carroçaria principal
            sb.Draw(_pixel, new Rectangle(cx - hw, cy - hh, CarW, CarH), color);

            // Capot (quarto dianteiro — lado direito, mais claro)
            Color hood = new Color(
                Math.Min(color.R + 30, 255),
                Math.Min(color.G + 30, 255),
                Math.Min(color.B + 30, 255));
            sb.Draw(_pixel, new Rectangle(cx + hw - 16, cy - hh, 16, CarH), hood);

            // Para-brisas (zona escura atrás do capot)
            sb.Draw(_pixel, new Rectangle(cx + hw - 28, cy - hh + 3, 10, CarH - 6), Color.Black * 0.55f);

            // Tejadilho (faixa central mais escura)
            sb.Draw(_pixel, new Rectangle(cx - hw + 14, cy - hh + 4, CarW - 30, CarH - 8), Color.Black * 0.2f);

            // Faróis dianteiros (nariz — lado direito)
            Color lights = isRival ? Color.Yellow : Color.Orange;
            sb.Draw(_pixel, new Rectangle(cx + hw, cy - hh, 4, 5), lights);  // farol cima
            sb.Draw(_pixel, new Rectangle(cx + hw, cy + hh - 5, 4, 5), lights);  // farol baixo

            // Farolins traseiros (lado esquerdo, vermelho)
            sb.Draw(_pixel, new Rectangle(cx - hw - 3, cy - hh, 3, 5), new Color(200, 20, 20));
            sb.Draw(_pixel, new Rectangle(cx - hw - 3, cy + hh - 5, 3, 5), new Color(200, 20, 20));

            // Rodas (4 cantos)
            Color wheel = new Color(30, 30, 30);
            sb.Draw(_pixel, new Rectangle(cx - hw + 4, cy - hh - 3, 10, 4), wheel); // roda traseira esq cima
            sb.Draw(_pixel, new Rectangle(cx - hw + 4, cy + hh - 1, 10, 4), wheel); // roda traseira esq baixo
            sb.Draw(_pixel, new Rectangle(cx + hw - 14, cy - hh - 3, 10, 4), wheel); // roda dianteira dir cima
            sb.Draw(_pixel, new Rectangle(cx + hw - 14, cy + hh - 1, 10, 4), wheel); // roda dianteira dir baixo
        }

        // ── Semáforo (HUD) ────────────────────────────────────────────
        private void DrawSemaforo(SpriteBatch sb, int W, int H)
        {
            Texture2D tex = _countdownStep switch
            {
                0 => _texSemVerm,
                1 => _texSemAm1,
                2 => _texSemAm2,
                3 => _texSemAm3,
                _ => _texSemVerde
            };
            if (tex.Width <= 1) return;

            int sW = 400;
            int sH = (int)((float)tex.Height / tex.Width * sW);
            if (_countdownStep >= 4)
            {
                float pulse = 1f + MathF.Sin(_countdownTimer * MathF.PI * 5) * 0.04f;
                sW = (int)(sW * pulse); sH = (int)(sH * pulse);
            }
            sb.Draw(tex, new Rectangle(W / 2 - sW / 2, 10, sW, sH), Color.White);
        }

        private void DrawGoText(SpriteBatch sb, int W, int H)
        {
            if (_countdownStep < 4) return;
            DrawText(sb, "GO!", W / 2 - 55, H / 2 - 30, new Color(50, 255, 80), 3.5f * _countdownScale);
        }

        // ── Rival Card ────────────────────────────────────────────────
        private void DrawRivalCard(SpriteBatch sb, int W, int H)
        {
            int pW = 500, pH = 290, px = W / 2 - 250, py = H / 2 - 145;
            sb.Draw(_pixel, new Rectangle(px + 6, py + 6, pW, pH), Color.Black * 0.7f);
            sb.Draw(_pixel, new Rectangle(px, py, pW, pH), new Color(12, 12, 20, 235));
            DrawBorder(sb, px, py, pW, pH, new Color(255, 100, 20), 3);
            sb.Draw(_pixel, new Rectangle(px, py, pW, 50), new Color(255, 100, 20));
            DrawText(sb, $"RIVAL  -  {_rivalName.ToUpper()}", px + 18, py + 12, Color.Black, 1.8f);
            sb.Draw(_pixel, new Rectangle(px + pW - 58, py + 9, 34, 34), _rivalColor);
            DrawText(sb, $"VEL MAX : {_rivalMaxSpeed:F0} km/h", px + 28, py + 68, new Color(210, 210, 210), 1f);
            DrawText(sb, $"ACEL    : {_rivalAccel:F0}", px + 28, py + 96, new Color(210, 210, 210), 1f);
            float diff = MathHelper.Clamp(_rivalMaxSpeed / 480f, 0f, 1f);
            DrawText(sb, "DIFICULDADE", px + 28, py + 128, new Color(150, 150, 150), 1f);
            sb.Draw(_pixel, new Rectangle(px + 28, py + 148, 300, 14), new Color(35, 35, 45));
            sb.Draw(_pixel, new Rectangle(px + 28, py + 148, (int)(300 * diff), 14), Color.Lerp(Color.LimeGreen, Color.Red, diff));
            DrawBorder(sb, px + 28, py + 148, 300, 14, new Color(60, 60, 80), 1);
            DrawText(sb, "SPACE / ENTER para comecar", px + 75, py + 226, new Color(255, 185, 50), 1f);
            sb.Draw(_pixel, new Rectangle(px, py + pH - 5, (int)(pW * (_previewTimer / 2.5f)), 5), new Color(255, 100, 20, 160));
        }

        // ── Barra de mudanças ─────────────────────────────────────────
        private void DrawGearBar(SpriteBatch sb, int W, int H)
        {
            int bW = 500, bH = 28, bx = W / 2 - 250, by = H - 108;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(12, 12, 18));
            DrawBorder(sb, bx, by, bW, bH, new Color(50, 50, 70), 2);
            FillZone(sb, bx, by, bW, bH, 0f, 0.40f, new Color(180, 30, 30, 150));
            FillZone(sb, bx, by, bW, bH, 0.40f, 0.55f, new Color(200, 180, 30, 160));
            FillZone(sb, bx, by, bW, bH, 0.55f, 0.80f, new Color(40, 200, 60, 200));
            FillZone(sb, bx, by, bW, bH, 0.80f, 0.92f, new Color(200, 180, 30, 160));
            FillZone(sb, bx, by, bW, bH, 0.92f, 1.00f, new Color(180, 30, 30, 150));
            int cur = bx + (int)(bW * (_revTimer / GearRevCycle));
            sb.Draw(_pixel, new Rectangle(cur - 2, by - 5, 4, bH + 10), Color.White);
            DrawText(sb, "SHIFT", bx - 58, by + 6, new Color(170, 170, 170), 1f);
        }

        private void FillZone(SpriteBatch sb, int bx, int by, int bW, int bH, float f, float t, Color c)
            => sb.Draw(_pixel, new Rectangle(bx + (int)(bW * f) + 1, by + 1, (int)(bW * (t - f)) - 1, bH - 2), c);

        // ── Velocímetro ───────────────────────────────────────────────
        private void DrawSpeedometer(SpriteBatch sb, int W, int H)
        {
            int bx = 28, by = H - 116;
            sb.Draw(_pixel, new Rectangle(bx, by, 130, 62), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, 130, 62, new Color(255, 100, 20), 2);
            DrawText(sb, $"{(int)_playerSpeed}", bx + 10, by + 6, Color.White, 2.0f);
            DrawText(sb, "km/h", bx + 10, by + 40, new Color(170, 170, 170), 1.0f);
        }

        // ── Barra de nitro ────────────────────────────────────────────
        private void DrawNitroBar(SpriteBatch sb, int W, int H)
        {
            int bx = W - 178, by = H - 116, bW = 140, bH = 22;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, bW, bH, new Color(0, 100, 220), 2);
            float r = _nitroCharge / NitroMaxCharge;
            Color c = _nitroActive ? new Color(120, 200, 255) : Color.Lerp(new Color(0, 50, 130), new Color(0, 140, 255), r);
            sb.Draw(_pixel, new Rectangle(bx + 1, by + 1, (int)((bW - 2) * r), bH - 2), c);
            DrawText(sb, "NITRO  [N]", bx, by + 26, new Color(80, 150, 210), 1f);
        }

        // ── Barra de progresso ────────────────────────────────────────
        private void DrawProgressBar(SpriteBatch sb, int W, int H)
        {
            int bW = 500, bH = 16, bx = W / 2 - 250, by = 18;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, bW, bH, new Color(50, 50, 70), 2);
            sb.Draw(_pixel, new Rectangle(bx + (int)(_rivalProgress * (bW - 12)), by, 12, bH), _rivalColor);
            sb.Draw(_pixel, new Rectangle(bx + (int)(_playerProgress * (bW - 12)), by, 12, bH), Color.OrangeRed);
            sb.Draw(_pixel, new Rectangle(bx + bW - 3, by - 5, 3, bH + 10), Color.White);
            DrawText(sb, _rivalName, bx - 5, by + 18, _rivalColor, 1f);
            DrawText(sb, "TU", bx - 5, by + 34, new Color(255, 150, 50), 1f);
        }

        // ── Indicador de mudança ──────────────────────────────────────
        private void DrawGearIndicator(SpriteBatch sb, int W, int H)
        {
            int bx = W / 2 - 32, by = H - 162;
            sb.Draw(_pixel, new Rectangle(bx, by, 64, 46), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, 64, 46, new Color(255, 100, 20), 2);
            DrawText(sb, (_currentGear <= MaxGears ? _currentGear.ToString() : "6"), bx + 10, by + 5, Color.OrangeRed, 2.5f);
            DrawText(sb, "[W] ACELERA   [SPACE] MUDA", bx - 90, by + 52, new Color(130, 130, 130), 0.8f);
        }

        // ── Shift feedback ────────────────────────────────────────────
        private void DrawShiftFeedback(SpriteBatch sb, int W, int H)
        {
            if (_shiftFeedbackTimer <= 0f) return;
            float a = MathHelper.Clamp(_shiftFeedbackTimer / 1.2f, 0f, 1f);
            int fy = H / 2 - 90 - (int)((1.2f - _shiftFeedbackTimer) * 28);
            DrawText(sb, _shiftFeedbackText, W / 2 - _shiftFeedbackText.Length * 13, fy, _shiftFeedbackColor * a, 2.2f);
        }

        // ── Ecrã de resultado ─────────────────────────────────────────
        private void DrawResultScreen(SpriteBatch sb, int W, int H)
        {
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.62f);
            int pW = 460, pH = 310, px = W / 2 - 230, py = H / 2 - 155;
            Color ac = _playerWon ? new Color(50, 255, 80) : new Color(255, 55, 55);
            sb.Draw(_pixel, new Rectangle(px + 5, py + 5, pW, pH), Color.Black * 0.6f);
            sb.Draw(_pixel, new Rectangle(px, py, pW, pH), new Color(10, 10, 18));
            DrawBorder(sb, px, py, pW, pH, ac, 4);
            sb.Draw(_pixel, new Rectangle(px, py, pW, 54), ac * 0.88f);
            string title = _playerWon ? "VITORIA!" : "DERROTA";
            DrawText(sb, title, px + pW / 2 - title.Length * 13, py + 10, Color.Black, 2.5f);
            DrawText(sb, $"+ {(_playerWon ? PlayerData.XP_WIN : PlayerData.XP_LOSS)} XP", px + 38, py + 76, Color.White, 1.5f);
            DrawText(sb, $"+ {(_playerWon ? PlayerData.MONEY_WIN : PlayerData.MONEY_LOSS)} EUR", px + 38, py + 108, new Color(255, 205, 50), 1.5f);
            if (_levelUpInfo.LeveledUp)
            {
                sb.Draw(_pixel, new Rectangle(px + 18, py + 148, pW - 36, 42), new Color(35, 55, 18));
                DrawBorder(sb, px + 18, py + 148, pW - 36, 42, new Color(90, 240, 70), 2);
                DrawText(sb, $"LEVEL UP!  {_levelUpInfo.PreviousLevel} -> {_levelUpInfo.NewLevel}", px + 28, py + 158, new Color(90, 240, 70), 1.4f);
            }
            if (_playerData != null)
                DrawText(sb, $"Saldo: {_playerData.Money}  Nivel: {_playerData.Level}", px + 28, py + 218, new Color(150, 150, 150), 1f);
            DrawText(sb, "SPACE / ENTER para continuar", px + pW / 2 - 138, py + 264, new Color(170, 170, 170), 1f);
            sb.Draw(_pixel, new Rectangle(px, py + pH - 4, (int)(pW * (_resultTimer / ResultDisplayTime)), 4), ac * 0.65f);
        }

        // ── Helpers ───────────────────────────────────────────────────
        private void DrawText(SpriteBatch sb, string text, int x, int y, Color color, float scale = 1f)
        {
            if (_font != null)
            { sb.DrawString(_font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); return; }
            int cW = (int)(8 * scale), cH = (int)(12 * scale), sp = cW + 2;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') continue;
                int w = (text[i] == 'I' || text[i] == '1' || text[i] == '!') ? cW / 2 : cW;
                sb.Draw(_pixel, new Rectangle(x + i * sp, y, w, cH), color * 0.92f);
            }
        }

        private void DrawBorder(SpriteBatch sb, int x, int y, int w, int h, Color c, int t)
        {
            sb.Draw(_pixel, new Rectangle(x, y, w, t), c);
            sb.Draw(_pixel, new Rectangle(x, y + h - t, w, t), c);
            sb.Draw(_pixel, new Rectangle(x, y, t, h), c);
            sb.Draw(_pixel, new Rectangle(x + w - t, y, t, h), c);
        }
    }
}