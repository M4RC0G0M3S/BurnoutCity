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
        // ── Constantes da pista ────────────────────────────────────────
        private const float FinishProgress = 1f;      // progresso normalizado 0..1

        // ── Constantes do carro ────────────────────────────────────────
        private const int MaxGears = 6;
        private const float GearRevCycle = 1.8f;   // segundos para a barra encher
        private const float NitroMaxCharge = 100f;
        private const float NitroDrainRate = 40f;    // unidades/s a drenar
        private const float NitroRechargeRate = 12f;    // unidades/s a recarregar
        private const float NitroBoostMult = 1.55f;  // multiplicador de velocidade

        // Velocidades máximas por mudança (px/s visuais)
        private static readonly float[] GearTopSpeeds = { 0f, 120f, 210f, 290f, 360f, 420f, 480f };
        private static readonly float[] GearAccel = { 0f, 280f, 220f, 170f, 130f, 95f, 70f };
        // Converte px/s → progresso normalizado/s (pista visual = 1280px)
        private const float PixelsToProgress = 1f / 1280f;

        // ── Layout da pista no ecrã ────────────────────────────────────
        private const int TrackTopY = 150;
        private const int TrackBottomY = 530;
        private const int TrackH = TrackBottomY - TrackTopY;
        private const int RivalLaneY = TrackTopY + 90;   // centro da pista de cima
        private const int PlayerLaneY = TrackBottomY - 90; // centro da pista de baixo

        // ── Tamanho dos carros ─────────────────────────────────────────
        private const int CarW = 30;
        private const int CarH = 54;

        // ── Fases da corrida ───────────────────────────────────────────
        private enum RacePhase { Preview, Countdown, Racing, Finished }
        private RacePhase _phase = RacePhase.Preview;

        // ── Jogador ────────────────────────────────────────────────────
        private float _playerProgress = 0f;
        private float _playerSpeed = 0f;
        private int _currentGear = 1;
        private float _revTimer = 0f;
        private float _nitroCharge = NitroMaxCharge;
        private bool _nitroActive = false;
        private bool _finished = false;
        private bool _playerWon = false;

        // ── Shift feedback ─────────────────────────────────────────────
        private float _shiftFeedbackTimer = 0f;
        private string _shiftFeedbackText = "";
        private Color _shiftFeedbackColor = Color.White;

        // ── Rival ──────────────────────────────────────────────────────
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

        // ── Countdown ──────────────────────────────────────────────────
        // step 0 = vermelho (3s) | 1 = am1 (1s) | 2 = am2 (1s) | 3 = am3 (1s) | 4 = verde GO!
        private float _countdownTimer = 3.0f;
        private int _countdownStep = 0;
        private float _countdownScale = 1f;

        // ── Preview ────────────────────────────────────────────────────
        private float _previewTimer = 2.5f;

        // ── Resultado ──────────────────────────────────────────────────
        private float _resultTimer = 0f;
        private const float ResultDisplayTime = 4f;
        private LevelUpInfo _levelUpInfo;

        // ── Input ──────────────────────────────────────────────────────
        private KeyboardState _prevKb;

        // ── Texturas ───────────────────────────────────────────────────
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

        // ── PlayerData (injectado) ─────────────────────────────────────
        private PlayerData? _playerData;

        // ══════════════════════════════════════════════════════════════════
        //  Construtor
        // ══════════════════════════════════════════════════════════════════
        public RaceState(PlayerData? playerData = null,
                         string rivalName = "Rival",
                         Color? rivalColor = null,
                         float rivalMaxSpeed = 350f,
                         float rivalAccel = 140f)
        {
            _playerData = playerData;
            _rivalName = rivalName;
            _rivalColor = rivalColor ?? Color.DeepSkyBlue;
            _rivalMaxSpeed = rivalMaxSpeed;
            _rivalAccel = rivalAccel;
        }

        // ══════════════════════════════════════════════════════════════════
        //  LOAD CONTENT
        // ══════════════════════════════════════════════════════════════════
        public override void LoadContent()
        {
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _texPista = LoadTex("Sprites/Race/Pista");
            _texBancCima = LoadTex("Sprites/Race/Bancada_Cima");
            _texBancBaixo = LoadTex("Sprites/Race/Bancada_Baixo");
            _texSemVerm = LoadTex("Sprites/Race/Semaforo_Vermelho");
            _texSemAm1 = LoadTex("Sprites/Race/Semaforo_Amarelo1");
            _texSemAm2 = LoadTex("Sprites/Race/Semaforo_Amarelo2");
            _texSemAm3 = LoadTex("Sprites/Race/Semaforo_Amarelo3");
            _texSemVerde = LoadTex("Sprites/Race/Semaforo_Verde");

            try { _font = ContentManager.Load<SpriteFont>("Fonts/RaceFont"); }
            catch { _font = null; }
        }

        private Texture2D LoadTex(string path)
        {
            try { return ContentManager.Load<Texture2D>(path); }
            catch
            {
                Console.WriteLine($"[RaceState] Textura não encontrada: {path}");
                var t = new Texture2D(GraphicsDevice, 1, 1);
                t.SetData(new[] { Color.Magenta });
                return t;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════
        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            switch (_phase)
            {
                case RacePhase.Preview: UpdatePreview(dt, kb); break;
                case RacePhase.Countdown: UpdateCountdown(dt, kb); break;
                case RacePhase.Racing: UpdateRacing(dt, kb); break;
                case RacePhase.Finished: UpdateFinished(dt, kb); break;
            }

            _prevKb = kb;
        }

        // ── Preview ────────────────────────────────────────────────────
        private void UpdatePreview(float dt, KeyboardState kb)
        {
            _previewTimer -= dt;
            bool pressed = (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space)) ||
                           (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter));
            if (_previewTimer <= 0f || pressed)
            {
                _phase = RacePhase.Countdown;
                _countdownStep = 0;
                _countdownTimer = 3.0f;
            }
        }

        // ── Countdown ──────────────────────────────────────────────────
        private void UpdateCountdown(float dt, KeyboardState kb)
        {
            _countdownTimer -= dt;
            _countdownScale = 1f + (1f - (_countdownTimer % 1f)) * 0.5f;

            if (_countdownTimer <= 0f)
            {
                _countdownStep++;
                _countdownTimer = _countdownStep switch
                {
                    1 or 2 or 3 => 1.0f,   // cada amarelo dura 1s
                    4 => 0.8f,   // verde fica 0.8s
                    _ => 0f      // arranca
                };
                if (_countdownStep >= 5)
                    _phase = RacePhase.Racing;
            }
        }

        // ── Racing ─────────────────────────────────────────────────────
        private void UpdateRacing(float dt, KeyboardState kb)
        {
            if (_finished) return;
            UpdatePlayerPhysics(dt, kb);
            UpdateRivalAI(dt);
            UpdateShiftFeedback(dt);

            if (_playerProgress >= FinishProgress && !_finished)
            { _finished = true; _playerWon = true; FinishRace(); }
            else if (_rivalProgress >= FinishProgress && !_finished)
            { _finished = true; _playerWon = false; FinishRace(); }
        }

        // ── Física do jogador ───────────────────────────────────────────
        private void UpdatePlayerPhysics(float dt, KeyboardState kb)
        {
            bool wDown = kb.IsKeyDown(Keys.W);
            bool spaceUp = kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space);
            bool nDown = kb.IsKeyDown(Keys.N);

            // Aceleração
            if (wDown && _currentGear > 0)
            {
                float top = GearTopSpeeds[_currentGear] * (_nitroActive ? NitroBoostMult : 1f);
                _playerSpeed = _playerSpeed < top
                    ? (float)Math.Min(_playerSpeed + GearAccel[_currentGear] * dt, top)
                    : MathHelper.Lerp(_playerSpeed, top, 0.1f);
            }
            else
            {
                _playerSpeed = (float)Math.Max(0f, _playerSpeed - 60f * dt);
            }

            // Barra de rotações enche automaticamente
            _revTimer = (float)Math.Min(_revTimer + dt, GearRevCycle - 0.01f);

            // Mudança de velocidade
            if (spaceUp && _currentGear < MaxGears)
                ProcessGearShift(_revTimer / GearRevCycle);

            // Nitro
            _nitroActive = nDown && _nitroCharge > 0f;
            _nitroCharge = _nitroActive
                ? (float)Math.Max(0f, _nitroCharge - NitroDrainRate * dt)
                : (float)Math.Min(NitroMaxCharge, _nitroCharge + NitroRechargeRate * dt);

            // Progresso
            float spd = _playerSpeed * (_nitroActive ? NitroBoostMult : 1f);
            _playerProgress = (float)Math.Min(FinishProgress, _playerProgress + spd * PixelsToProgress * dt);
        }

        private void ProcessGearShift(float ratio)
        {
            if (ratio >= 0.55f && ratio <= 0.80f)
            {
                _playerSpeed *= 1.08f;
                _shiftFeedbackText = "PERFECT!";
                _shiftFeedbackColor = new Color(50, 255, 80);
            }
            else if ((ratio >= 0.40f && ratio < 0.55f) || (ratio > 0.80f && ratio <= 0.92f))
            {
                _shiftFeedbackText = "GOOD";
                _shiftFeedbackColor = new Color(255, 200, 50);
            }
            else
            {
                _playerSpeed *= 0.82f;
                _shiftFeedbackText = "MISS!";
                _shiftFeedbackColor = new Color(255, 60, 60);
            }

            _currentGear++;
            _revTimer = 0f;
            _shiftFeedbackTimer = 1.2f;
            Console.WriteLine($"[RaceState] Gear {_currentGear} | ratio:{ratio:F2} | {_shiftFeedbackText}");
        }

        private void UpdateShiftFeedback(float dt)
        {
            if (_shiftFeedbackTimer > 0f) _shiftFeedbackTimer -= dt;
        }

        // ── Rival IA ───────────────────────────────────────────────────
        private void UpdateRivalAI(float dt)
        {
            _rivalNoise = MathHelper.Lerp(_rivalNoise, (float)(_rng.NextDouble() * 20 - 10), 0.05f);
            _rivalGearTimer += dt;
            if (_rivalGearTimer >= 1.5f && _rivalGear < MaxGears)
            { _rivalGear++; _rivalGearTimer = 0f; }

            float top = GearTopSpeeds[Math.Min(_rivalGear, MaxGears)]
                      * (_rivalMaxSpeed / GearTopSpeeds[MaxGears]) + _rivalNoise;
            _rivalSpeed = _rivalSpeed < top
                ? (float)Math.Min(_rivalSpeed + _rivalAccel * dt, top)
                : MathHelper.Lerp(_rivalSpeed, top, 0.1f);
            _rivalSpeed = (float)Math.Max(0f, _rivalSpeed);

            _rivalProgress = (float)Math.Min(FinishProgress,
                _rivalProgress + _rivalSpeed * PixelsToProgress * dt);
        }

        // ── Fim da corrida ─────────────────────────────────────────────
        private void FinishRace()
        {
            _phase = RacePhase.Finished;
            _resultTimer = ResultDisplayTime;
            if (_playerData != null)
                _levelUpInfo = _playerData.RegisterRaceResult(_playerWon);
            Console.WriteLine($"[RaceState] {(_playerWon ? "VITÓRIA" : "DERROTA")}");
        }

        private void UpdateFinished(float dt, KeyboardState kb)
        {
            _resultTimer -= dt;
            bool anyKey = (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)) ||
                          (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space));
            if (_resultTimer <= 0f || anyKey)
                GameStateManager.Instance.ChangeState(new ExplorationState());
        }

        // ══════════════════════════════════════════════════════════════════
        //  DRAW
        // ══════════════════════════════════════════════════════════════════
        public override void Draw(SpriteBatch sb)
        {
            int W = GraphicsDevice.Viewport.Width;
            int H = GraphicsDevice.Viewport.Height;

            DrawBackground(sb, W, H);
            DrawBancadaCima(sb, W, H);
            DrawTrack(sb, W, H);
            DrawBancadaBaixo(sb, W, H);
            DrawCars(sb, W, H);

            if (_phase == RacePhase.Preview)
            {
                DrawRivalCard(sb, W, H);
                return;
            }

            DrawTrafficLight(sb, W, H);

            if (_phase == RacePhase.Countdown)
                DrawCountdownText(sb, W, H);

            if (_phase == RacePhase.Racing || _phase == RacePhase.Finished)
            {
                DrawGearBar(sb, W, H);
                DrawSpeedometer(sb, W, H);
                DrawNitroBar(sb, W, H);
                DrawProgressBar(sb, W, H);
                DrawGearIndicator(sb, W, H);
                DrawShiftFeedback(sb, W, H);
            }

            if (_phase == RacePhase.Finished)
                DrawResultScreen(sb, W, H);
        }

        // ── Fundo ──────────────────────────────────────────────────────
        private void DrawBackground(SpriteBatch sb, int W, int H)
        {
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(8, 8, 14));
        }

        // ── Bancada de cima ────────────────────────────────────────────
        private void DrawBancadaCima(SpriteBatch sb, int W, int H)
        {
            if (_texBancCima == null) return;
            // Ocupa o espaço acima da pista
            float scale = (float)W / _texBancCima.Width;
            int bh = (int)(_texBancCima.Height * scale);
            int by = TrackTopY - bh;
            // Se a bancada for maior que o espaço, clipa por baixo
            if (by < 0) by = 0;
            sb.Draw(_texBancCima, new Rectangle(0, by, W, Math.Min(bh, TrackTopY)), Color.White);
        }

        // ── Pista (pista_completa.png) ─────────────────────────────────
        private void DrawTrack(SpriteBatch sb, int W, int H)
        {
            if (_texPista != null)
            {
                sb.Draw(_texPista, new Rectangle(0, TrackTopY, W, TrackH), Color.White);
            }
            else
            {
                // Fallback
                sb.Draw(_pixel, new Rectangle(0, TrackTopY, W, TrackH), new Color(28, 28, 36));
                sb.Draw(_pixel, new Rectangle(0, TrackTopY, W, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackBottomY - 4, W, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackTopY + TrackH / 2 - 2, W, 4),
                        new Color(200, 200, 200, 80));
            }
        }

        // ── Bancada de baixo ───────────────────────────────────────────
        private void DrawBancadaBaixo(SpriteBatch sb, int W, int H)
        {
            if (_texBancBaixo == null) return;
            float scale = (float)W / _texBancBaixo.Width;
            int bh = (int)(_texBancBaixo.Height * scale);
            int by = TrackBottomY;
            sb.Draw(_texBancBaixo, new Rectangle(0, by, W, Math.Min(bh, H - TrackBottomY)), Color.White);
        }

        // ── Carros ─────────────────────────────────────────────────────
        private void DrawCars(SpriteBatch sb, int W, int H)
        {
            int trackStart = 60;
            int trackUsable = W - 80 - trackStart;

            int playerX = trackStart + (int)(_playerProgress * trackUsable);
            int rivalX = trackStart + (int)(_rivalProgress * trackUsable);

            DrawCar(sb, rivalX, RivalLaneY, CarW, CarH, _rivalColor, true);
            DrawCar(sb, playerX, PlayerLaneY, CarW, CarH, Color.OrangeRed, false);

            if (_nitroActive)
                DrawNitroFlame(sb, playerX - CarW / 2 - 2, PlayerLaneY);
        }

        private void DrawCar(SpriteBatch sb, int cx, int cy, int w, int h, Color color, bool isRival)
        {
            sb.Draw(_pixel, new Rectangle(cx - w / 2 + 4, cy - h / 2 + 4, w, h), Color.Black * 0.45f);
            sb.Draw(_pixel, new Rectangle(cx - w / 2, cy - h / 2, w, h), color);
            sb.Draw(_pixel, new Rectangle(cx - w / 2 + 4, cy - h / 2 + 6, w - 8, 14), Color.White * 0.22f);
            Color front = isRival ? Color.Yellow : Color.Orange;
            sb.Draw(_pixel, new Rectangle(cx - 5, cy - h / 2 - 6, 10, 6), front);
        }

        private void DrawNitroFlame(SpriteBatch sb, int x, int cy)
        {
            float t = (float)(DateTime.Now.Millisecond % 180) / 180f;
            int flameL = 30 + (int)(t * 16);
            sb.Draw(_pixel, new Rectangle(x - flameL, cy - 7, flameL, 14), new Color(255, 70, 0, 200));
            sb.Draw(_pixel, new Rectangle(x - flameL + 8, cy - 5, flameL - 8, 10), new Color(255, 170, 0, 180));
            sb.Draw(_pixel, new Rectangle(x - flameL + 16, cy - 3, flameL - 16, 6), new Color(255, 255, 160, 150));
        }

        // ── Semáforo com sprites reais ─────────────────────────────────
        //
        //  step 0 → vermelho  (tudo vermelho)
        //  step 1 → amarelo1  (1 luz acesa da esquerda)
        //  step 2 → amarelo2  (2 luzes acesas)
        //  step 3 → amarelo3  (3 luzes acesas / todas amarelas)
        //  step 4+ → verde    (GO!)
        // ──────────────────────────────────────────────────────────────
        private void DrawTrafficLight(SpriteBatch sb, int W, int H)
        {
            Texture2D tex = _countdownStep switch
            {
                0 => _texSemVerm,
                1 => _texSemAm1,
                2 => _texSemAm2,
                3 => _texSemAm3,
                _ => _texSemVerde
            };

            if (tex == null) return;

            // Largura fixa 340px, altura proporcional à textura original
            int semW = 340;
            int semH = (int)((float)tex.Height / tex.Width * semW);
            int semX = W / 2 - semW / 2;
            int semY = TrackTopY - semH + 10; // cola ao topo da pista

            // Pulse no verde
            if (_countdownStep >= 4)
            {
                float pulse = 1f + MathF.Sin(_countdownTimer * MathF.PI * 5) * 0.04f;
                int pw = (int)(semW * pulse);
                int ph = (int)(semH * pulse);
                sb.Draw(tex, new Rectangle(W / 2 - pw / 2, semY - (ph - semH) / 2, pw, ph), Color.White);
            }
            else
            {
                sb.Draw(tex, new Rectangle(semX, semY, semW, semH), Color.White);
            }
        }

        private void DrawCountdownText(SpriteBatch sb, int W, int H)
        {
            if (_countdownStep < 4) return;
            // "GO!" grande
            DrawText(sb, "GO!", W / 2 - 55, TrackTopY + TrackH / 2 - 30,
                     new Color(50, 255, 80), 3.5f * _countdownScale);
        }

        // ── Rival Card (preview) ───────────────────────────────────────
        private void DrawRivalCard(SpriteBatch sb, int W, int H)
        {
            int pW = 500, pH = 290;
            int px = W / 2 - pW / 2;
            int py = H / 2 - pH / 2;

            sb.Draw(_pixel, new Rectangle(px + 6, py + 6, pW, pH), Color.Black * 0.7f);
            sb.Draw(_pixel, new Rectangle(px, py, pW, pH), new Color(12, 12, 20, 235));
            DrawBorder(sb, px, py, pW, pH, new Color(255, 100, 20), 3);

            sb.Draw(_pixel, new Rectangle(px, py, pW, 50), new Color(255, 100, 20));
            DrawText(sb, $"RIVAL  —  {_rivalName.ToUpper()}", px + 18, py + 12, Color.Black, 1.8f);

            // Cor do rival
            sb.Draw(_pixel, new Rectangle(px + pW - 58, py + 9, 34, 34), _rivalColor);
            DrawBorder(sb, px + pW - 58, py + 9, 34, 34, Color.Black * 0.5f, 2);

            DrawText(sb, $"VELOCIDADE MAX : {_rivalMaxSpeed:F0} km/h", px + 28, py + 68, new Color(210, 210, 210), 1f);
            DrawText(sb, $"ACELERAÇÃO     : {_rivalAccel:F0}", px + 28, py + 96, new Color(210, 210, 210), 1f);

            float diff = MathHelper.Clamp(_rivalMaxSpeed / 480f, 0f, 1f);
            DrawText(sb, "DIFICULDADE", px + 28, py + 130, new Color(150, 150, 150), 1f);
            sb.Draw(_pixel, new Rectangle(px + 28, py + 150, 300, 14), new Color(35, 35, 45));
            sb.Draw(_pixel, new Rectangle(px + 28, py + 150, (int)(300 * diff), 14),
                    Color.Lerp(Color.LimeGreen, Color.Red, diff));
            DrawBorder(sb, px + 28, py + 150, 300, 14, new Color(60, 60, 80), 1);

            DrawText(sb, "SPACE / ENTER para começar", px + 80, py + 230, new Color(255, 185, 50), 1f);

            float ratio = _previewTimer / 2.5f;
            sb.Draw(_pixel, new Rectangle(px, py + pH - 5, (int)(pW * ratio), 5), new Color(255, 100, 20, 160));
        }

        // ── Barra de mudanças ──────────────────────────────────────────
        private void DrawGearBar(SpriteBatch sb, int W, int H)
        {
            int bW = 500, bH = 28;
            int bx = W / 2 - bW / 2, by = H - 108;

            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(12, 12, 18));
            DrawBorder(sb, bx, by, bW, bH, new Color(50, 50, 70), 2);

            FillBar(sb, bx, by, bW, bH, 0f, 0.40f, new Color(180, 30, 30, 150));
            FillBar(sb, bx, by, bW, bH, 0.40f, 0.55f, new Color(200, 180, 30, 160));
            FillBar(sb, bx, by, bW, bH, 0.55f, 0.80f, new Color(40, 200, 60, 200));
            FillBar(sb, bx, by, bW, bH, 0.80f, 0.92f, new Color(200, 180, 30, 160));
            FillBar(sb, bx, by, bW, bH, 0.92f, 1.00f, new Color(180, 30, 30, 150));

            int cursor = bx + (int)(bW * (_revTimer / GearRevCycle));
            sb.Draw(_pixel, new Rectangle(cursor - 2, by - 5, 4, bH + 10), Color.White);

            DrawText(sb, "SHIFT", bx - 58, by + 6, new Color(170, 170, 170), 1f);
        }

        private void FillBar(SpriteBatch sb, int bx, int by, int bW, int bH,
                              float from, float to, Color color)
        {
            sb.Draw(_pixel, new Rectangle(bx + (int)(bW * from) + 1, by + 1,
                                          (int)(bW * (to - from)) - 1, bH - 2), color);
        }

        // ── Velocímetro ────────────────────────────────────────────────
        private void DrawSpeedometer(SpriteBatch sb, int W, int H)
        {
            int bx = 28, by = H - 116;
            sb.Draw(_pixel, new Rectangle(bx, by, 130, 62), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, 130, 62, new Color(255, 100, 20), 2);
            DrawText(sb, $"{(int)_playerSpeed}", bx + 10, by + 6, Color.White, 2.0f);
            DrawText(sb, "km/h", bx + 10, by + 40, new Color(170, 170, 170), 1f);
        }

        // ── Barra de nitro ─────────────────────────────────────────────
        private void DrawNitroBar(SpriteBatch sb, int W, int H)
        {
            int bx = W - 178, by = H - 116;
            int bW = 140, bH = 22;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, bW, bH, new Color(0, 100, 220), 2);
            float ratio = _nitroCharge / NitroMaxCharge;
            Color col = _nitroActive
                ? new Color(120, 200, 255)
                : Color.Lerp(new Color(0, 50, 130), new Color(0, 140, 255), ratio);
            sb.Draw(_pixel, new Rectangle(bx + 1, by + 1, (int)((bW - 2) * ratio), bH - 2), col);
            DrawText(sb, "NITRO  [N]", bx, by + 26, new Color(80, 150, 210), 1f);
        }

        // ── Barra de progresso ─────────────────────────────────────────
        private void DrawProgressBar(SpriteBatch sb, int W, int H)
        {
            int bW = 500, bH = 16;
            int bx = W / 2 - bW / 2, by = 18;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, bW, bH, new Color(50, 50, 70), 2);

            int rX = bx + (int)(_rivalProgress * (bW - 12));
            int pX = bx + (int)(_playerProgress * (bW - 12));
            sb.Draw(_pixel, new Rectangle(rX, by, 12, bH), _rivalColor);
            sb.Draw(_pixel, new Rectangle(pX, by, 12, bH), Color.OrangeRed);
            sb.Draw(_pixel, new Rectangle(bx + bW - 3, by - 5, 3, bH + 10), Color.White);

            DrawText(sb, _rivalName, bx - 5, by + 18, _rivalColor, 1f);
            DrawText(sb, "TU", bx - 5, by + 34, new Color(255, 150, 50), 1f);
        }

        // ── Indicador de mudança atual ─────────────────────────────────
        private void DrawGearIndicator(SpriteBatch sb, int W, int H)
        {
            int bx = W / 2 - 32, by = H - 162;
            sb.Draw(_pixel, new Rectangle(bx, by, 64, 46), new Color(8, 8, 14));
            DrawBorder(sb, bx, by, 64, 46, new Color(255, 100, 20), 2);
            string g = _currentGear <= MaxGears ? _currentGear.ToString() : "6";
            DrawText(sb, g, bx + 10, by + 5, Color.OrangeRed, 2.5f);
            DrawText(sb, "[W] ACELERA    [SPACE] MUDA", bx - 90, by + 52, new Color(130, 130, 130), 0.8f);
        }

        // ── Shift feedback ─────────────────────────────────────────────
        private void DrawShiftFeedback(SpriteBatch sb, int W, int H)
        {
            if (_shiftFeedbackTimer <= 0f) return;
            float alpha = MathHelper.Clamp(_shiftFeedbackTimer / 1.2f, 0f, 1f);
            int fy = H / 2 - 90 - (int)((1.2f - _shiftFeedbackTimer) * 28);
            DrawText(sb, _shiftFeedbackText,
                     W / 2 - _shiftFeedbackText.Length * 13, fy,
                     _shiftFeedbackColor * alpha, 2.2f);
        }

        // ── Ecrã de resultado ──────────────────────────────────────────
        private void DrawResultScreen(SpriteBatch sb, int W, int H)
        {
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.62f);

            int pW = 460, pH = 310;
            int px = W / 2 - pW / 2, py = H / 2 - pH / 2;
            Color accent = _playerWon ? new Color(50, 255, 80) : new Color(255, 55, 55);

            sb.Draw(_pixel, new Rectangle(px + 5, py + 5, pW, pH), Color.Black * 0.6f);
            sb.Draw(_pixel, new Rectangle(px, py, pW, pH), new Color(10, 10, 18));
            DrawBorder(sb, px, py, pW, pH, accent, 4);

            sb.Draw(_pixel, new Rectangle(px, py, pW, 54), accent * 0.88f);
            string title = _playerWon ? "VITÓRIA!" : "DERROTA";
            DrawText(sb, title, px + pW / 2 - title.Length * 13, py + 10, Color.Black, 2.5f);

            int xpG = _playerWon ? PlayerData.XP_WIN : PlayerData.XP_LOSS;
            int moneyG = _playerWon ? PlayerData.MONEY_WIN : PlayerData.MONEY_LOSS;
            DrawText(sb, $"+ {xpG} XP", px + 38, py + 76, Color.White, 1.5f);
            DrawText(sb, $"+ {moneyG} €", px + 38, py + 108, new Color(255, 205, 50), 1.5f);

            if (_levelUpInfo.LeveledUp)
            {
                sb.Draw(_pixel, new Rectangle(px + 18, py + 148, pW - 36, 42), new Color(35, 55, 18));
                DrawBorder(sb, px + 18, py + 148, pW - 36, 42, new Color(90, 240, 70), 2);
                string lv = $"LEVEL UP!  {_levelUpInfo.PreviousLevel} → {_levelUpInfo.NewLevel}";
                DrawText(sb, lv, px + 28, py + 158, new Color(90, 240, 70), 1.4f);
            }

            if (_playerData != null)
                DrawText(sb, $"Saldo: {_playerData.Money} €   Nível: {_playerData.Level}",
                         px + 28, py + 218, new Color(150, 150, 150), 1f);

            DrawText(sb, "SPACE / ENTER para continuar",
                     px + pW / 2 - 138, py + 264, new Color(170, 170, 170), 1f);

            float ratio = _resultTimer / ResultDisplayTime;
            sb.Draw(_pixel, new Rectangle(px, py + pH - 4, (int)(pW * ratio), 4), accent * 0.65f);
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════
        private void DrawText(SpriteBatch sb, string text, int x, int y, Color color, float scale = 1f)
        {
            if (_font != null)
            {
                sb.DrawString(_font, text, new Vector2(x, y), color, 0f,
                              Vector2.Zero, scale, SpriteEffects.None, 0f);
                return;
            }
            int cW = (int)(8 * scale), cH = (int)(12 * scale), sp = cW + 2;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') continue;
                int w = (text[i] == 'I' || text[i] == '1' || text[i] == '!') ? (int)(cW * 0.5f) : cW;
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