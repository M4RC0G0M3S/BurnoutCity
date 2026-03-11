using System;
using System.Collections.Generic;
using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.States
{
    // ══════════════════════════════════════════════════════════════════
    //  TestTrackState — Pista de treino drag (sem rival, sem recompensa)
    //
    //  Controlos: W = acelerar | Space = mudar | N = nitro | ESC = sair
    //  Fluxo:
    //    1. Carro parado na linha de partida
    //    2. W arranca — cronómetro inicia automaticamente
    //    3. Chega ao fim — tempo registado no top 5
    //    4. Carro reseta para o início — nova tentativa
    // ══════════════════════════════════════════════════════════════════
    public class TestTrackState : BaseState
    {
        // ── Física (igual ao RaceState) ────────────────────────────────
        private const int MaxGears = 6;
        private const float GearRevCycle = 1.8f;
        private const float NitroMaxCharge = 100f;
        private const float NitroDrainRate = 40f;
        private const float NitroRechargeRate = 12f;
        private const float NitroBoostMult = 1.55f;
        private const float PixelsToProgress = 1f / 1280f;

        private static readonly float[] GearTopSpeeds = { 0f, 120f, 210f, 290f, 360f, 420f, 480f };
        private static readonly float[] GearAccel = { 0f, 280f, 220f, 170f, 130f, 95f, 70f };

        // ── Pista ──────────────────────────────────────────────────────
        private const int TrackRealW = 2000;
        private const int TrackRealH = 222;
        private const int TrackY = 249;
        private const int PlayerLaneY = TrackY + 167;  // pista de baixo (igual RaceState)

        // ── Carro ──────────────────────────────────────────────────────
        private const int CarW = 54;
        private const int CarH = 28;

        // ── Estado do carro ────────────────────────────────────────────
        private float _progress = 0f;
        private float _speed = 0f;
        private int _gear = 1;
        private float _revTimer = 0f;
        private float _nitroCharge = NitroMaxCharge;
        private bool _nitroActive = false;

        // ── Shift feedback ─────────────────────────────────────────────
        private float _shiftTimer = 0f;
        private string _shiftText = "";
        private Color _shiftColor = Color.White;

        // ── Cronómetro ─────────────────────────────────────────────────
        private enum RunState { Waiting, Running, Finished }
        private RunState _runState = RunState.Waiting;
        private float _lapTime = 0f;
        private float _lastTime = 0f;
        private bool _isRecord = false;
        private float _flashTimer = 0f;

        // ── Top 5 ──────────────────────────────────────────────────────
        private List<float> _bestTimes = new();

        // ── Câmara ─────────────────────────────────────────────────────
        private Vector2 _camPos = Vector2.Zero;

        // ── Input ──────────────────────────────────────────────────────
        private KeyboardState _prevKb;

        // ── Texturas ──────────────────────────────────────────────────
        private Texture2D _pixel = null!;
        private Texture2D _tPista = null!;
        private Texture2D _tBancC = null!;
        private Texture2D _tBancB = null!;
        private SpriteFont? _font = null;

        // ── PlayerData (opcional) ──────────────────────────────────────
        private PlayerData? _playerData;

        // ══════════════════════════════════════════════════════════════
        public TestTrackState(PlayerData? playerData = null)
        {
            _playerData = playerData;
        }

        // ══════════════════════════════════════════════════════════════
        //  LOAD CONTENT
        // ══════════════════════════════════════════════════════════════
        public override void LoadContent()
        {
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _tPista = Tex("Sprites/World/Track/pista_completa");
            _tBancC = Tex("Sprites/World/Track/Bancada_Cima");
            _tBancB = Tex("Sprites/World/Track/Bancada_Baixo");

            try { _font = ContentManager.Load<SpriteFont>("Fonts/RaceFont"); }
            catch { _font = null; }

            if (_playerData != null)
                _bestTimes = new List<float>(_playerData.BestLapTimes);

            ResetCar();

            Console.WriteLine("[TestTrack] LoadContent OK");
        }

        private Texture2D Tex(string path)
        {
            try { return ContentManager.Load<Texture2D>(path); }
            catch
            {
                Console.WriteLine($"[TestTrack] FAIL: {path}");
                var t = new Texture2D(GraphicsDevice, 1, 1);
                t.SetData(new[] { Color.Magenta });
                return t;
            }
        }

        // ── Reset para o início da pista ───────────────────────────────
        private void ResetCar()
        {
            _progress = 0f;
            _speed = 0f;
            _gear = 1;
            _revTimer = 0f;
            _nitroCharge = NitroMaxCharge;
            _nitroActive = false;
            _runState = RunState.Waiting;
            _lapTime = 0f;
            _shiftTimer = 0f;

            // Câmara: início da pista (clamped)
            int vW = GraphicsDevice.Viewport.Width;
            _camPos.X = vW / 2f;
            _camPos.Y = TrackY + TrackRealH / 2f;
        }

        // ══════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════
        public override void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            var kb = Keyboard.GetState();

            if (kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape))
                GameStateManager.Instance.ChangeState(new ExplorationState());

            switch (_runState)
            {
                case RunState.Waiting: UpdateWaiting(dt, kb); break;
                case RunState.Running: UpdateRunning(dt, kb); break;
                case RunState.Finished: UpdateFinished(dt, kb); break;
            }

            UpdateCam();
            if (_shiftTimer > 0f) _shiftTimer -= dt;
            if (_flashTimer > 0f) _flashTimer -= dt;

            _prevKb = kb;
        }

        // ── Waiting: espera que o jogador carregue W para arrancar ─────
        private void UpdateWaiting(float dt, KeyboardState kb)
        {
            if (kb.IsKeyDown(Keys.W) && _prevKb.IsKeyUp(Keys.W))
            {
                _runState = RunState.Running;
                _lapTime = 0f;
            }
        }

        // ── Running: física completa + cronómetro ──────────────────────
        private void UpdateRunning(float dt, KeyboardState kb)
        {
            _lapTime += dt;

            bool w = kb.IsKeyDown(Keys.W);
            bool sp = kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space);
            bool n = kb.IsKeyDown(Keys.N);

            // Aceleração (só para a frente, sem marcha-atrás)
            if (w)
            {
                float top = GearTopSpeeds[_gear] * (_nitroActive ? NitroBoostMult : 1f);
                _speed = _speed < top
                    ? (float)Math.Min(_speed + GearAccel[_gear] * dt, top)
                    : MathHelper.Lerp(_speed, top, 0.1f);
            }
            else
            {
                // Largar o acelerador: desacelera (sem poder andar para trás)
                _speed = (float)Math.Max(0f, _speed - 60f * dt);
            }

            // Barra de rotações
            _revTimer = (float)Math.Min(_revTimer + dt, GearRevCycle - 0.01f);

            // Mudança
            if (sp && _gear < MaxGears) Shift(_revTimer / GearRevCycle);

            // Nitro
            _nitroActive = n && _nitroCharge > 0f;
            _nitroCharge = _nitroActive
                ? (float)Math.Max(0f, _nitroCharge - NitroDrainRate * dt)
                : (float)Math.Min(NitroMaxCharge, _nitroCharge + NitroRechargeRate * dt);

            // Progresso
            float spd = _speed * (_nitroActive ? NitroBoostMult : 1f);
            _progress = (float)Math.Min(1f, _progress + spd * PixelsToProgress * dt);

            // Chegou ao fim?
            if (_progress >= 1f) FinishRun();
        }

        // ── Finished: R = nova tentativa | SPACE/ENTER = voltar ao lobby ─
        private void UpdateFinished(float dt, KeyboardState kb)
        {
            if (kb.IsKeyDown(Keys.R) && _prevKb.IsKeyUp(Keys.R))
                ResetCar();

            if ((kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space)) ||
                (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)))
                GameStateManager.Instance.ChangeState(new ExplorationState());
        }

        // ── Shift ─────────────────────────────────────────────────────
        private void Shift(float ratio)
        {
            if (ratio >= 0.55f && ratio <= 0.80f)
            { _speed *= 1.08f; _shiftText = "PERFECT!"; _shiftColor = new Color(50, 255, 80); }
            else if ((ratio >= 0.40f && ratio < 0.55f) || (ratio > 0.80f && ratio <= 0.92f))
            { _shiftText = "GOOD"; _shiftColor = new Color(255, 200, 50); }
            else
            { _speed *= 0.82f; _shiftText = "MISS!"; _shiftColor = new Color(255, 60, 60); }

            _gear++;
            _revTimer = 0f;
            _shiftTimer = 1.2f;
            Console.WriteLine($"[TestTrack] Gear {_gear} ratio:{ratio:F2} {_shiftText}");
        }

        // ── Fim da corrida ─────────────────────────────────────────────
        private void FinishRun()
        {
            _runState = RunState.Finished;
            _lastTime = _lapTime;

            if (_playerData != null)
            {
                _isRecord = _playerData.RegisterLapTime(_lapTime);
                _bestTimes = new List<float>(_playerData.BestLapTimes);
            }
            else
            {
                _bestTimes.Add(_lapTime);
                _bestTimes.Sort();
                if (_bestTimes.Count > 5) _bestTimes.RemoveRange(5, _bestTimes.Count - 5);
                _isRecord = _bestTimes[0] == _lapTime;
            }

            _flashTimer = _isRecord ? 3f : 0f;
            Console.WriteLine($"[TestTrack] Tempo: {Fmt(_lastTime)} Record:{_isRecord}");
        }

        // ── Câmara (igual ao RaceState) ────────────────────────────────
        private void UpdateCam()
        {
            int vW = GraphicsDevice.Viewport.Width;
            float halfVW = vW / 2f;
            float carX = 60f + _progress * (TrackRealW - 120f);
            float targetX = MathHelper.Clamp(carX, halfVW, TrackRealW - halfVW);
            _camPos.X = MathHelper.Lerp(_camPos.X, targetX, 0.15f);
            _camPos.Y = TrackY + TrackRealH / 2f;
        }

        // ══════════════════════════════════════════════════════════════
        //  DRAW
        // ══════════════════════════════════════════════════════════════
        public override void Draw(SpriteBatch sb)
        {
            int W = GraphicsDevice.Viewport.Width;
            int H = GraphicsDevice.Viewport.Height;

            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(8, 8, 14));

            // ── Mundo com câmara ────────────────────────────────────────
            Matrix cam = Matrix.CreateTranslation(-_camPos.X + W / 2f, -_camPos.Y + H / 2f, 0f);
            sb.End();
            sb.Begin(transformMatrix: cam);

            DrawWorldBancadaCima(sb);
            DrawWorldTrack(sb);
            DrawWorldBancadaBaixo(sb);
            DrawWorldCar(sb);

            sb.End();
            sb.Begin();

            // ── HUD ────────────────────────────────────────────────────
            DrawGearBar(sb, W, H);
            DrawNitroBar(sb, W, H);
            DrawGearIndicator(sb, W, H);
            DrawShiftFeedback(sb, W, H);
            DrawChrono(sb, W, H);
            DrawBestTimes(sb, W, H);
            DrawSpeedometer(sb, W, H);

            if (_runState == RunState.Finished) DrawResultScreen(sb, W, H);
            if (_flashTimer > 0f) DrawRecordFlash(sb, W, H);

            DrawHints(sb, W, H);
        }

        // ── Mundo ─────────────────────────────────────────────────────
        private void DrawWorldBancadaCima(SpriteBatch sb)
        {
            if (_tBancC.Width <= 1) return;
            float sx = (float)TrackRealW / _tBancC.Width;
            int bh = (int)(_tBancC.Height * sx);
            sb.Draw(_tBancC, new Rectangle(0, TrackY - bh, TrackRealW, bh), Color.White);
        }

        private void DrawWorldTrack(SpriteBatch sb)
        {
            if (_tPista.Width > 1)
                sb.Draw(_tPista, new Rectangle(0, TrackY, TrackRealW, TrackRealH), Color.White);
            else
            {
                sb.Draw(_pixel, new Rectangle(0, TrackY, TrackRealW, TrackRealH), new Color(28, 28, 36));
                sb.Draw(_pixel, new Rectangle(0, TrackY, TrackRealW, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackY + TrackRealH - 4, TrackRealW, 4), new Color(255, 100, 20));
                sb.Draw(_pixel, new Rectangle(0, TrackY + TrackRealH / 2 - 2, TrackRealW, 4), new Color(200, 200, 200, 60));
            }

            // Linha de partida (início)
            for (int i = 0; i < TrackRealH / 20; i++)
            {
                Color c = i % 2 == 0 ? Color.White : Color.Black;
                sb.Draw(_pixel, new Rectangle(55, TrackY + i * 20, 8, 20), c);
            }

            // Linha de chegada (fim)
            for (int i = 0; i < TrackRealH / 20; i++)
            {
                Color c = i % 2 == 0 ? Color.White : Color.Black;
                sb.Draw(_pixel, new Rectangle(TrackRealW - 63, TrackY + i * 20, 8, 20), c);
            }
        }

        private void DrawWorldBancadaBaixo(SpriteBatch sb)
        {
            if (_tBancB.Width <= 1) return;
            float sx = (float)TrackRealW / _tBancB.Width;
            int bh = (int)(_tBancB.Height * sx);
            sb.Draw(_tBancB, new Rectangle(0, TrackY + TrackRealH, TrackRealW, bh), Color.White);
        }

        private void DrawWorldCar(SpriteBatch sb)
        {
            float usable = TrackRealW - 120f;
            int cx = 60 + (int)(_progress * usable);
            int cy = PlayerLaneY;

            // Sombra
            sb.Draw(_pixel, new Rectangle(cx - CarW / 2 + 3, cy - CarH / 2 + 3, CarW, CarH), Color.Black * 0.45f);
            // Corpo
            sb.Draw(_pixel, new Rectangle(cx - CarW / 2, cy - CarH / 2, CarW, CarH), Color.OrangeRed);
            // Capot (frente mais clara)
            sb.Draw(_pixel, new Rectangle(cx + CarW / 2 - 16, cy - CarH / 2, 16, CarH), new Color(255, 120, 50));
            // Para-brisas
            sb.Draw(_pixel, new Rectangle(cx + CarW / 2 - 28, cy - CarH / 2 + 3, 10, CarH - 6), Color.Black * 0.45f);
            // Faróis
            sb.Draw(_pixel, new Rectangle(cx + CarW / 2, cy - CarH / 2, 4, 5), Color.Orange);
            sb.Draw(_pixel, new Rectangle(cx + CarW / 2, cy + CarH / 2 - 5, 4, 5), Color.Orange);
            // Farolins
            sb.Draw(_pixel, new Rectangle(cx - CarW / 2 - 3, cy - CarH / 2, 3, 5), new Color(200, 20, 20));
            sb.Draw(_pixel, new Rectangle(cx - CarW / 2 - 3, cy + CarH / 2 - 5, 3, 5), new Color(200, 20, 20));
            // Rodas
            Color wh = new Color(30, 30, 30);
            sb.Draw(_pixel, new Rectangle(cx - CarW / 2 + 4, cy - CarH / 2 - 3, 10, 4), wh);
            sb.Draw(_pixel, new Rectangle(cx - CarW / 2 + 4, cy + CarH / 2 - 1, 10, 4), wh);
            sb.Draw(_pixel, new Rectangle(cx + CarW / 2 - 14, cy - CarH / 2 - 3, 10, 4), wh);
            sb.Draw(_pixel, new Rectangle(cx + CarW / 2 - 14, cy + CarH / 2 - 1, 10, 4), wh);

            // Nitro
            if (_nitroActive)
            {
                float ft = (float)(DateTime.Now.Millisecond % 180) / 180f;
                int fl = 28 + (int)(ft * 16);
                int fx = cx - CarW / 2 - 2;
                sb.Draw(_pixel, new Rectangle(fx - fl, cy - 5, fl, 10), new Color(255, 70, 0, 200));
                sb.Draw(_pixel, new Rectangle(fx - fl + 7, cy - 3, fl - 7, 6), new Color(255, 170, 0, 180));
                sb.Draw(_pixel, new Rectangle(fx - fl + 14, cy - 2, fl - 14, 4), new Color(255, 255, 160, 150));
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  HUD
        // ══════════════════════════════════════════════════════════════

        // ── Barra de mudanças ─────────────────────────────────────────
        private void DrawGearBar(SpriteBatch sb, int W, int H)
        {
            if (_runState != RunState.Running) return;
            int bW = 500, bH = 28, bx = W / 2 - 250, by = H - 108;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(12, 12, 18));
            Border(sb, bx, by, bW, bH, new Color(50, 50, 70), 2);
            Zone(sb, bx, by, bW, bH, 0f, 0.40f, new Color(180, 30, 30, 150));
            Zone(sb, bx, by, bW, bH, 0.40f, 0.55f, new Color(200, 180, 30, 160));
            Zone(sb, bx, by, bW, bH, 0.55f, 0.80f, new Color(40, 200, 60, 200));
            Zone(sb, bx, by, bW, bH, 0.80f, 0.92f, new Color(200, 180, 30, 160));
            Zone(sb, bx, by, bW, bH, 0.92f, 1.00f, new Color(180, 30, 30, 150));
            int cur = bx + (int)(bW * (_revTimer / GearRevCycle));
            sb.Draw(_pixel, new Rectangle(cur - 2, by - 5, 4, bH + 10), Color.White);
            T(sb, "SHIFT", bx - 58, by + 6, new Color(170, 170, 170), 1f);
        }

        // ── Velocímetro ───────────────────────────────────────────────
        private void DrawSpeedometer(SpriteBatch sb, int W, int H)
        {
            int bx = 28, by = H - 116;
            sb.Draw(_pixel, new Rectangle(bx, by, 130, 62), new Color(8, 8, 14));
            Border(sb, bx, by, 130, 62, new Color(255, 100, 20), 2);
            T(sb, $"{(int)_speed}", bx + 10, by + 6, Color.White, 2f);
            T(sb, "km/h", bx + 10, by + 40, new Color(170, 170, 170), 1f);
        }

        // ── Nitro ─────────────────────────────────────────────────────
        private void DrawNitroBar(SpriteBatch sb, int W, int H)
        {
            if (_runState != RunState.Running) return;
            int bx = W - 178, by = H - 116, bW = 140, bH = 22;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(8, 8, 14));
            Border(sb, bx, by, bW, bH, new Color(0, 100, 220), 2);
            float r = _nitroCharge / NitroMaxCharge;
            Color c = _nitroActive ? new Color(120, 200, 255) : Color.Lerp(new Color(0, 50, 130), new Color(0, 140, 255), r);
            sb.Draw(_pixel, new Rectangle(bx + 1, by + 1, (int)((bW - 2) * r), bH - 2), c);
            T(sb, "NITRO [N]", bx, by + 26, new Color(80, 150, 210), 1f);
        }

        // ── Indicador de mudança ──────────────────────────────────────
        private void DrawGearIndicator(SpriteBatch sb, int W, int H)
        {
            if (_runState != RunState.Running) return;
            int bx = W / 2 - 32, by = H - 162;
            sb.Draw(_pixel, new Rectangle(bx, by, 64, 46), new Color(8, 8, 14));
            Border(sb, bx, by, 64, 46, new Color(255, 100, 20), 2);
            T(sb, (_gear <= MaxGears ? _gear.ToString() : "6"), bx + 10, by + 5, Color.OrangeRed, 2.5f);
        }

        // ── Shift feedback ────────────────────────────────────────────
        private void DrawShiftFeedback(SpriteBatch sb, int W, int H)
        {
            if (_shiftTimer <= 0f) return;
            float a = MathHelper.Clamp(_shiftTimer / 1.2f, 0f, 1f);
            int fy = H / 2 - 90 - (int)((1.2f - _shiftTimer) * 28);
            T(sb, _shiftText, W / 2 - _shiftText.Length * 13, fy, _shiftColor * a, 2.2f);
        }

        // ── Cronómetro ────────────────────────────────────────────────
        private void DrawChrono(SpriteBatch sb, int W, int H)
        {
            int bW = 280, bH = 80, bx = W / 2 - bW / 2, by = 20;
            sb.Draw(_pixel, new Rectangle(bx + 4, by + 4, bW, bH), Color.Black * 0.6f);
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(8, 8, 18, 230));
            Border(sb, bx, by, bW, bH, new Color(255, 100, 20), 3);

            Color timeCol = _runState == RunState.Waiting ? new Color(100, 100, 100) : Color.White;
            T(sb, Fmt(_lapTime), bx + 20, by + 10, timeCol, 2.5f);

            string label = _runState switch
            {
                RunState.Waiting => "PRIMA W PARA ARRANCAR",
                RunState.Running => "VOLTA ATUAL",
                RunState.Finished => "TEMPO FINAL",
                _ => ""
            };
            T(sb, label, bx + 20, by + 56, new Color(150, 150, 150), 0.9f);
        }

        // ── Top 5 ─────────────────────────────────────────────────────
        private void DrawBestTimes(SpriteBatch sb, int W, int H)
        {
            int bW = 210, bx = 20, by = 20;
            int bH = 32 + Math.Max(1, _bestTimes.Count) * 22 + 8;
            sb.Draw(_pixel, new Rectangle(bx, by, bW, bH), new Color(8, 8, 18, 210));
            Border(sb, bx, by, bW, bH, new Color(80, 80, 120), 2);
            T(sb, "TOP 5 VOLTAS", bx + 10, by + 8, new Color(255, 100, 20), 1f);

            if (_bestTimes.Count == 0)
            { T(sb, "SEM TEMPOS", bx + 10, by + 32, new Color(80, 80, 80), 1f); return; }

            for (int i = 0; i < _bestTimes.Count; i++)
            {
                Color c = i == 0 ? new Color(50, 255, 80) : new Color(180, 180, 180);
                T(sb, $"{i + 1}. {Fmt(_bestTimes[i])}", bx + 10, by + 32 + i * 22, c, 1f);
            }
        }

        // ── Ecrã de resultado ─────────────────────────────────────────
        private void DrawResultScreen(SpriteBatch sb, int W, int H)
        {
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.55f);
            int pW = 440, pH = 260, px = W / 2 - 220, py = H / 2 - 130;
            Color ac = _isRecord ? new Color(50, 255, 80) : new Color(255, 100, 20);
            sb.Draw(_pixel, new Rectangle(px + 5, py + 5, pW, pH), Color.Black * 0.6f);
            sb.Draw(_pixel, new Rectangle(px, py, pW, pH), new Color(10, 10, 18));
            Border(sb, px, py, pW, pH, ac, 4);
            sb.Draw(_pixel, new Rectangle(px, py, pW, 52), ac * 0.85f);

            string title = _isRecord ? "NOVO RECORD!" : "TEMPO FINAL";
            T(sb, title, px + pW / 2 - title.Length * 13, py + 10, Color.Black, 2.5f);
            T(sb, Fmt(_lastTime), px + pW / 2 - 90, py + 80, Color.White, 3f);

            if (_bestTimes.Count >= 2)
                T(sb, $"Recorde: {Fmt(_bestTimes[0])}", px + 30, py + 148, new Color(150, 150, 150), 1f);

            T(sb, "R = TENTAR DE NOVO", px + 60, py + 196, new Color(170, 170, 170), 1f);
            T(sb, "SPACE/ENTER = VOLTAR AO LOBBY", px + 60, py + 218, new Color(170, 170, 170), 1f);
        }

        // ── "NOVO RECORD!" flash ──────────────────────────────────────
        private void DrawRecordFlash(SpriteBatch sb, int W, int H)
        {
            float a = MathHelper.Clamp(_flashTimer / 3f, 0f, 1f);
            float pulse = 1f + MathF.Sin(_flashTimer * MathF.PI * 4) * 0.06f;
            T(sb, "NOVO RECORD!", W / 2 - 130, H / 2 - 80, new Color(50, 255, 80) * a, 2.5f * pulse);
        }

        // ── Hints ─────────────────────────────────────────────────────
        private void DrawHints(SpriteBatch sb, int W, int H)
        {
            if (_runState == RunState.Running)
                T(sb, "[W] ACELERA  [SPACE] MUDA  [N] NITRO  [ESC] SAIR", 20, H - 30, new Color(70, 70, 70), 0.85f);
            else if (_runState == RunState.Waiting)
                T(sb, "[ESC] SAIR", 20, H - 30, new Color(70, 70, 70), 0.85f);
        }

        // ══════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════
        private static string Fmt(float s)
        {
            int m = (int)(s / 60);
            int ss = (int)(s % 60);
            int ms = (int)((s - MathF.Floor(s)) * 1000);
            return $"{m:D2}:{ss:D2}.{ms:D3}";
        }

        private void T(SpriteBatch sb, string text, int x, int y, Color color, float scale = 1f)
        {
            if (_font != null) { sb.DrawString(_font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); return; }
            int cW = (int)(8 * scale), cH = (int)(12 * scale), sp = cW + 2;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') continue;
                int w = (text[i] == 'I' || text[i] == '1' || text[i] == '!' || text[i] == '.' || text[i] == ':' || text[i] == ',') ? cW / 2 : cW;
                sb.Draw(_pixel, new Rectangle(x + i * sp, y, w, cH), color * 0.92f);
            }
        }

        private void Border(SpriteBatch sb, int x, int y, int w, int h, Color c, int t)
        {
            sb.Draw(_pixel, new Rectangle(x, y, w, t), c);
            sb.Draw(_pixel, new Rectangle(x, y + h - t, w, t), c);
            sb.Draw(_pixel, new Rectangle(x, y, t, h), c);
            sb.Draw(_pixel, new Rectangle(x + w - t, y, t, h), c);
        }

        private void Zone(SpriteBatch sb, int bx, int by, int bW, int bH, float f, float t, Color c)
            => sb.Draw(_pixel, new Rectangle(bx + (int)(bW * f) + 1, by + 1, (int)(bW * (t - f)) - 1, bH - 2), c);
    }
}