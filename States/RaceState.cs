using System;
using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.States
{
    // ══════════════════════════════════════════════════════════════════
    //  RaceState — Corrida drag CSR completa com sprites reais
    //
    //  Assets necessários em Content/Sprites/Race/ :
    //    Semaforo_Vermelho   ← ChatGPT_Image_9_03_2026__16_25_40
    //    Semaforo_Amarelo1   ← amarelo_1   (1 luz acesa)
    //    Semaforo_Amarelo2   ← amarelo_2   (2 luzes acesas)
    //    Semaforo_Amarelo3   ← amarelo_3   (3 luzes acesas / todas)
    //    Semaforo_Verde      ← verde
    //    Bancada_Cima        ← Bancada_Cima
    //    Bancada_Baixo       ← Bancada_Baixo
    //    Pista               ← pista_completa
    //
    //  Controlos: W = acelerar | Space = mudar velocidade | N = nitro
    // ══════════════════════════════════════════════════════════════════
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

        // ── Dimensões da pista (tamanho real, nao escala ao ecra) ────────
        // A pista_completa.png tem 2000x222 px — usamos estas dimensoes reais
        private const int TrackRealW = 2000;  // largura total da pista
        private const int TrackRealH = 222;   // altura da pista (1 imagem, 2 pistas)
        private const int TrackY = 249;   // Y no mundo onde a pista começa (centrada em 720/2)

        // Pistas individuais (Y no mundo, centro de cada pista)
        private const int RivalLaneY = TrackY + 55;    // pista de cima
        private const int PlayerLaneY = TrackY + 167;   // pista de baixo

        // ── Tamanho dos carros (horizontal: mais largo que alto) ───────
        private const int CarW = 54;   // comprimento do carro (eixo X, direção de movimento)
        private const int CarH = 28;   // largura do carro (eixo Y)

        // ── Câmara ─────────────────────────────────────────────────────
        private Vector2 _cameraPos = Vector2.Zero;

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

            Console.WriteLine($"[RaceState] RootDir = '{ContentManager.RootDirectory}'");

            // Tenta nome com underscore primeiro, depois com espaco (qualquer que esteja no .mgcb)
            _texPista = LoadTexTry("Sprites/World/Track/pista_completa", "Sprites/World/Track/pista completa");
            _texBancCima = LoadTexTry("Sprites/World/Track/Bancada_Cima", "Sprites/World/Track/Bancada Cima");
            _texBancBaixo = LoadTexTry("Sprites/World/Track/Bancada_Baixo", "Sprites/World/Track/Bancada Baixo");
            _texSemVerm = LoadTexTry("Sprites/World/Track/vermelha", "Sprites/World/Track/vermelha");
            _texSemAm1 = LoadTexTry("Sprites/World/Track/amarelo_1", "Sprites/World/Track/amarelo 1");
            _texSemAm2 = LoadTexTry("Sprites/World/Track/amarelo_2", "Sprites/World/Track/amarelo 2");
            _texSemAm3 = LoadTexTry("Sprites/World/Track/amarelo_3", "Sprites/World/Track/amarelo 3");
            _texSemVerde = LoadTexTry("Sprites/World/Track/verde", "Sprites/World/Track/verde");

            Console.WriteLine($"[RaceState] pista={(_texPista.Width > 1 ? "OK" : "FAIL")} bancCima={(_texBancCima.Width > 1 ? "OK" : "FAIL")} semVerm={(_texSemVerm.Width > 1 ? "OK" : "FAIL")}");

            try { _font = ContentManager.Load<SpriteFont>("Fonts/RaceFont"); }
            catch { _font = null; }
        }

        // Tenta path1; se falhar tenta path2; se falhar devolve pixel magenta
        private Texture2D LoadTexTry(string path1, string path2)
        {
            try
            {
                var t = ContentManager.Load<Texture2D>(path1);
                Console.WriteLine($"[RaceState] OK: {path1}");
                return t;
            }
            catch { }
            try
            {
                var t = ContentManager.Load<Texture2D>(path2);
                Console.WriteLine($"[RaceState] OK (fallback): {path2}");
                return t;
            }
            catch { }
            Console.WriteLine($"[RaceState] FALHOU: '{path1}'");
            var err = new Texture2D(GraphicsDevice, 1, 1);
            err.SetData(new[] { Color.Magenta });
            return err;
        }

        private Texture2D LoadTex(string path)
        {
            try { return ContentManager.Load<Texture2D>(path); }
            catch
            {
                Console.WriteLine($"[RaceState] Textura nao encontrada: {path}");
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
            UpdateCamera();

            if (_playerProgress >= FinishProgress && !_finished)
            { _finished = true; _playerWon = true; FinishRace(); }
            else if (_rivalProgress >= FinishProgress && !_finished)
            { _finished = true; _playerWon = false; FinishRace(); }
        }

        // ── Câmara segue o carro do jogador horizontalmente ────────────
        private void UpdateCamera()
        {
            // Posição X do jogador no mundo
            float playerWorldX = 60f + _playerProgress * (TrackRealW - 120f);
            // Câmara centra no jogador, limitada aos bounds da pista
            float targetX = playerWorldX;
            _cameraPos.X = MathHelper.Lerp(_cameraPos.X, targetX, 0.15f);
            // Y fixo: centra a pista verticalmente no ecrã (720/2 - TrackRealH/2)
            _cameraPos.Y = TrackY + TrackRealH / 2f;
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
            int W = GraphicsDevice.Viewport.Width;  // 1280
            int H = GraphicsDevice.Viewport.Height; // 720

            // ── Matriz da câmara: centra no carro do jogador ───────────
            // A câmara segue X do jogador; Y fica fixo centrado na pista
            Matrix camTransform = Matrix.CreateTranslation(
                -_cameraPos.X + W / 2f,
                -_cameraPos.Y + H / 2f,
                0f
            );

            // ── Fundo (sem câmara — fixo no ecrã) ─────────────────────
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(8, 8, 14));

            // ── Mundo (com câmara) ─────────────────────────────────────
            sb.End();
            sb.Begin(transformMatrix: camTransform);

            DrawBancadaCima(sb, W, H);
            DrawTrack(sb, W, H);
            DrawBancadaBaixo(sb, W, H);
            DrawCars(sb, W, H);

            sb.End();
            sb.Begin(); // volta ao espaço de ecrã para HUD

            // ── HUD (sem câmara — fixo no ecrã) ───────────────────────
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

        // ── Fundo escuro ───────────────────────────────────────────────
        private void DrawBackground(SpriteBatch sb, int W, int H)
        {
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(8, 8, 14));
        }

        // ── Bancada de cima (acima da pista no mundo) ──────────────────
        private void DrawBancadaCima(SpriteBatch sb, int W, int H)
        {
            if (_texBancCima == null) return;
            // Altura proporcional, largura = tamanho real da pista
            float scaleX = (float)TrackRealW / _texBancCima.Width;
            int bh = (int)(_texBancCima.Height * scaleX);
            sb.Draw(_texBancCima, new Rectangle(0, TrackY - bh, TrackRealW, bh), Color.White);
        }

        // ── Pista no tamanho real ──────────────────────────────────────
        private void DrawTrack(SpriteBatch sb, int W, int H)
        {
            if (_texPista != null)
            {
                sb.Draw(_texPista, new Rectangle(0, TrackY, TrackRealW, TrackRealH), Color.White);
            }
            else
            {
                // Fallback: duas pistas laranja neon
                sb.Draw(_pixel, new Rectangle(0, TrackY, TrackRealW, TrackRealH), new Color(28, 28, 36));
                sb.Draw(_pixel, new Rectangle(0, TrackY, TrackRealW, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackY + TrackRealH - 4, TrackRealW, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackY + TrackRealH / 2 - 2, TrackRealW, 4), new Color(200, 200, 200, 80));
                // Linha de meta
                for (int row = 0; row < TrackRealH / 20; row++)
                    for (int col = 0; col < 2; col++)
                    {
                        Color c = (row + col) % 2 == 0 ? Color.White : Color.Black;
                        sb.Draw(_pixel, new Rectangle(TrackRealW - 40 + col * 20, TrackY + row * 20, 20, 20), c);
                    }
            }
        }

        // ── Bancada de baixo (abaixo da pista no mundo) ────────────────
        private void DrawBancadaBaixo(SpriteBatch sb, int W, int H)
        {
            if (_texBancBaixo == null) return;
            float scaleX = (float)TrackRealW / _texBancBaixo.Width;
            int bh = (int)(_texBancBaixo.Height * scaleX);
            sb.Draw(_texBancBaixo, new Rectangle(0, TrackY + TrackRealH, TrackRealW, bh), Color.White);
        }

        // ── Carros no mundo ────────────────────────────────────────────
        private void DrawCars(SpriteBatch sb, int W, int H)
        {
            // Converte progresso 0..1 → posição X no mundo
            float usable = TrackRealW - 120f;
            int playerX = 60 + (int)(_playerProgress * usable);
            int rivalX = 60 + (int)(_rivalProgress * usable);

            // Carro rival — pista de cima
            DrawCar(sb, rivalX, RivalLaneY, CarW, CarH, _rivalColor, true);
            // Carro jogador — pista de baixo
            DrawCar(sb, playerX, PlayerLaneY, CarW, CarH, Color.OrangeRed, false);

            if (_nitroActive)
                DrawNitroFlame(sb, playerX - CarW / 2 - 2, PlayerLaneY);
        }

        // Carro horizontal (CarW = comprimento, CarH = largura)
        private void DrawCar(SpriteBatch sb, int cx, int cy, int w, int h, Color color, bool isRival)
        {
            // Sombra
            sb.Draw(_pixel, new Rectangle(cx - w / 2 + 3, cy - h / 2 + 3, w, h), Color.Black * 0.45f);
            // Carroçaria
            sb.Draw(_pixel, new Rectangle(cx - w / 2, cy - h / 2, w, h), color);
            // Para-brisas
            sb.Draw(_pixel, new Rectangle(cx - w / 2 + 5, cy - h / 2 + 3, 14, h - 6), Color.White * 0.22f);
            // Frente (lado direito — direção de movimento)
            Color front = isRival ? Color.Yellow : Color.Orange;
            sb.Draw(_pixel, new Rectangle(cx + w / 2, cy - 4, 6, 8), front);
        }

        private void DrawNitroFlame(SpriteBatch sb, int x, int cy)
        {
            float t = (float)(DateTime.Now.Millisecond % 180) / 180f;
            int flameL = 30 + (int)(t * 16);
            // Chama atrás do carro (lado esquerdo)
            sb.Draw(_pixel, new Rectangle(x - flameL, cy - 6, flameL, 12), new Color(255, 70, 0, 200));
            sb.Draw(_pixel, new Rectangle(x - flameL + 8, cy - 4, flameL - 8, 8), new Color(255, 170, 0, 180));
            sb.Draw(_pixel, new Rectangle(x - flameL + 16, cy - 2, flameL - 16, 4), new Color(255, 255, 160, 150));
        }

        // ── Semaforo com sprites reais ────────────────────────────────
        //  Desenhado no espaço de ecrã (HUD), centrado no topo da janela
        //  step 0 = vermelho | 1 = am1 | 2 = am2 | 3 = am3 | 4+ = verde
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

            // Largura fixa 400px, altura proporcional (semáforo é horizontal 1536x1024 → ratio ~0.67)
            int semW = 400;
            int semH = (int)((float)tex.Height / tex.Width * semW);
            int semX = W / 2 - semW / 2;
            int semY = 10; // topo do ecrã

            if (_countdownStep >= 4)
            {
                // Pulse no verde
                float pulse = 1f + MathF.Sin(_countdownTimer * MathF.PI * 5) * 0.04f;
                int pw = (int)(semW * pulse);
                int ph = (int)(semH * pulse);
                sb.Draw(tex, new Rectangle(W / 2 - pw / 2, semY, pw, ph), Color.White);
            }
            else
            {
                sb.Draw(tex, new Rectangle(semX, semY, semW, semH), Color.White);
            }
        }

        private void DrawCountdownText(SpriteBatch sb, int W, int H)
        {
            if (_countdownStep < 4) return;
            DrawText(sb, "GO!", W / 2 - 55, H / 2 - 30, new Color(50, 255, 80), 3.5f * _countdownScale);
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