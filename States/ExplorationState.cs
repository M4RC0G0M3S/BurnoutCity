using BurnoutCity.Core;
using BurnoutCity.Entities;
using BurnoutCity.Map;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BurnoutCity.States
{
    public class ExplorationState : BaseState
    {
        private Car                _playerCar      = null!;
        private Camera             _camera         = null!;
        private Texture2D          _pixelTexture   = null!;
        private Rectangle          _worldBounds;
        private MapManager         _mapManager     = null!;
        private BuildingManager    _buildingManager = null!;
        private TriggerZoneManager _triggerZones   = null!;
        private TrafficManager     _trafficManager = null!;
        private HUD                _hud            = null!;

        // Detecção de mudança de velocidade (para som de mudança)
        private float _lastSpeedBucket = 0f;

        private KeyboardState _prevKeyboard;

        public override void LoadContent()
        {
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            int viewportWidth  = GraphicsDevice.Viewport.Width;
            int viewportHeight = GraphicsDevice.Viewport.Height;
            _worldBounds = new Rectangle(0, 0, viewportWidth * 3, viewportHeight * 3);

            Vector2 spawnpoint = new Vector2(1792f, 900f);
            PlayerData pd = GameStateManager.Instance.PlayerData;
            CarStats stats = BuildCarStatsFromPlayerData(pd);
            _playerCar = new Car(spawnpoint, stats);    
            // Carrega e atribui o sprite sheet do carro jogador
            
            Texture2D carSprite = ContentManager.Load<Texture2D>("Sprites/CarSprites/car");
            _playerCar.SetSpriteSheet(carSprite);

           _camera    = new Camera(viewportWidth, viewportHeight, _worldBounds);
            PlayerData pd    = GameStateManager.Instance.PlayerData;
            CarStats   stats = BuildCarStatsFromPlayerData(pd);
            _playerCar = new Car(spawnpoint, stats);
            _camera    = new Camera(viewportWidth, viewportHeight, _worldBounds);
            _camera.Update(_playerCar.Position);

            _mapManager = new MapManager(_worldBounds.Width, _worldBounds.Height);
            _mapManager.LoadContent(ContentManager);
            _buildingManager = new BuildingManager(_mapManager);

            _triggerZones = new TriggerZoneManager();
            _triggerZones.OnZoneEntered += HandleZoneEntered;

            CreateStreetLayout();
            _trafficManager = new TrafficManager(_worldBounds);

            // HUD
            _hud = new HUD(null!, GraphicsDevice);
            _hud.LoadContent(
                fontBig:    ContentManager.Load<SpriteFont>("Fonts/FontBig"),
                fontMedium: ContentManager.Load<SpriteFont>("Fonts/FontMedium"),
                fontSmall:  ContentManager.Load<SpriteFont>("Fonts/FontSmall")
            );
            _hud.MaxSpeed = _playerCar.Stats.MaxSpeed;
            _hud.MaxNitro = 100f;

            // Efeitos visuais do carro
            var smokeSheet = ContentManager.Load<Texture2D>("Sprites/Effects/smoke_sheet");
            var nitroSheet = ContentManager.Load<Texture2D>("Sprites/Effects/nitro_sheet");
            var wheelSheet = ContentManager.Load<Texture2D>("Sprites/Effects/wheel_sheet");
            _playerCar.LoadEffects(smokeSheet, nitroSheet, wheelSheet);

            // Áudio
            AudioManager.Instance.LoadContent(ContentManager);
            AudioManager.Instance.StartEngine();
            AudioManager.Instance.PlayExplorationMusic();
        }

        private void HandleZoneEntered(TriggerZoneType zoneType)
        {
            GameStateManager.Instance.PlayerData.SavePosition(
                _playerCar.Position.X, _playerCar.Position.Y);

            AudioManager.Instance.StopEngine();

            switch (zoneType)
            {
                case TriggerZoneType.Garage:
                    GameStateManager.Instance.ChangeState(new GarageState());
                    break;
                case TriggerZoneType.PartsShop:
                    GameStateManager.Instance.ChangeState(new ShopState());
                    break;
                case TriggerZoneType.CustomShop:
                    GameStateManager.Instance.ChangeState(new CustomizationState());
                    break;
                case TriggerZoneType.RacePoint:
                    AudioManager.Instance.PlayRaceMusic();
                    GameStateManager.Instance.ChangeState(new RaceState());
                    break;
                case TriggerZoneType.TestTrack:
                    AudioManager.Instance.PlayRaceMusic();
                    GameStateManager.Instance.ChangeState(new TestTrackState());
                    break;
            }
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();

            if (kb.IsKeyDown(Keys.F1) && _prevKeyboard.IsKeyUp(Keys.F1))
                _triggerZones.DebugVisible = !_triggerZones.DebugVisible;
            if (kb.IsKeyDown(Keys.F2) && _prevKeyboard.IsKeyUp(Keys.F2))
                System.IO.File.WriteAllText("pos.txt",
                    $"X:{(int)_playerCar.Position.X} Y:{(int)_playerCar.Position.Y}");

            _prevKeyboard = kb;

            _playerCar.Update(gameTime);
            HandleBuildingCollisions();
            HandleWorldBoundaryCollision();

            _triggerZones.Update(_playerCar.Position);
            _camera.Update(_playerCar.Position);
            _trafficManager.Update(gameTime, _playerCar.Bounds, _playerCar);

            // ── Áudio do motor ────────────────────────────────────────────────
            float speed = _playerCar.CurrentSpeed;
            AudioManager.Instance.UpdateEngine(speed, _playerCar.Stats.MaxSpeed);
            AudioManager.Instance.Update(gameTime);

            // Som de mudança de velocidade (a cada ~60 km/h)
            float bucket = MathF.Floor(Math.Abs(speed) / 60f);
            if (bucket != _lastSpeedBucket && Math.Abs(speed) > 20f)
            {
                AudioManager.Instance.PlayGearChange();
                _lastSpeedBucket = bucket;
            }

            // ── HUD ───────────────────────────────────────────────────────────
            PlayerData pd      = GameStateManager.Instance.PlayerData;
            _hud.Speed         = speed;
            _hud.Nitro         = 100f;
            _hud.NitroActive   = false;
            _hud.Level         = pd.Level;
            _hud.CurrentXP     = pd.XPInCurrentLevel();
            _hud.XPToNextLevel = pd.XPForNextLevel();
            _hud.Money         = pd.Money;
            _hud.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(transformMatrix: _camera.GetTransform());

            spriteBatch.Draw(_pixelTexture,
                new Rectangle(0, 0, _worldBounds.Width, _worldBounds.Height),
                new Color(40, 40, 45));

            DrawDebugGrid(spriteBatch);
            DrawWorldBounds(spriteBatch);
            _mapManager.Draw(spriteBatch);
            _triggerZones.DrawDebug(spriteBatch, _pixelTexture);
            _buildingManager.Draw(spriteBatch, _pixelTexture, _playerCar.Position);
            _trafficManager.Draw(spriteBatch, _pixelTexture);
            _playerCar.Draw(spriteBatch, _pixelTexture);

            spriteBatch.End();
            spriteBatch.Begin();

            _hud.Draw(spriteBatch);
        }

        private void HandleWorldBoundaryCollision()
        {
            Rectangle car  = _playerCar.Bounds;
            Vector2   pos  = _playerCar.Position;
            bool      pushed = false;

            if (car.Left < _worldBounds.Left)
            { pos.X = _worldBounds.Left + car.Width / 2f; pushed = true; }
            else if (car.Right > _worldBounds.Right)
            { pos.X = _worldBounds.Right - car.Width / 2f; pushed = true; }

            if (car.Top < _worldBounds.Top)
            { pos.Y = _worldBounds.Top + car.Height / 2f; pushed = true; }
            else if (car.Bottom > _worldBounds.Bottom)
            { pos.Y = _worldBounds.Bottom - car.Height / 2f; pushed = true; }

            if (pushed)
            {
                _playerCar.SetPosition(pos);
                AudioManager.Instance.PlayCollision(0.5f);
            }
        }

        private void HandleBuildingCollisions()
        {
            Rectangle carBounds = _playerCar.Bounds;
            foreach (var building in _buildingManager.Buildings)
            {
                Rectangle bb = building.Bounds;
                if (!carBounds.Intersects(bb)) continue;

                float overlapLeft   = carBounds.Right  - bb.Left;
                float overlapRight  = bb.Right  - carBounds.Left;
                float overlapTop    = carBounds.Bottom - bb.Top;
                float overlapBottom = bb.Bottom - carBounds.Top;

                bool  fromLeft = overlapLeft  < overlapRight;
                bool  fromTop  = overlapTop   < overlapBottom;
                float minX = fromLeft ? overlapLeft  : overlapRight;
                float minY = fromTop  ? overlapTop   : overlapBottom;

                Vector2 corrected = _playerCar.Position;
                if (minX < minY)
                    corrected.X += fromLeft ? -overlapLeft : overlapRight;
                else
                    corrected.Y += fromTop  ? -overlapTop  : overlapBottom;

                _playerCar.SetPosition(corrected);
                _playerCar.ApplyCollisionDamage(5f);

                // Som de colisão com intensidade baseada na velocidade
                float intensity = MathHelper.Clamp(
                    Math.Abs(_playerCar.CurrentSpeed) / _playerCar.Stats.MaxSpeed, 0.2f, 1f);
                AudioManager.Instance.PlayCollision(intensity);

                Console.WriteLine(
                    $"[ExplorationState] Colisão com {building.Type}. " +
                    $"Dano: {_playerCar.Stats.CurrentDamage}");
            }
        }

        private CarStats BuildCarStatsFromPlayerData(PlayerData pd)
        {
            CarStats stats = new CarStats();
            stats.MaxSpeed      = 300f + pd.EngineLevel * 40f;
            stats.Acceleration  = 200f + pd.TurboLevel  * 30f;
            stats.Handling      = 1.0f + pd.TiresLevel  * 0.15f;
            stats.NitroBoost    = 150f + pd.NitroLevel  * 25f;
            stats.CurrentDamage = pd.CarDamage;
            return stats;
        }

        public void CreateStreetLayout()
        {
            const float scaleRua        = 0.5f;
            const float scaleCruzamento = 0.485f;
            const int   seg             = 256;

            int worldW   = _worldBounds.Width;
            int worldH   = _worldBounds.Height;
            int avH      = 768;
            int avV      = 1792;
            int streetH1 = 256;
            int streetH2 = 1536;
            int streetV1 = 1024;
            int streetV2 = 2560;

            for (int x = 0; x < worldW; x += seg)
            {
                Vector2 ajuste = new Vector2(0, -9);
                Vector2 posAv  = new Vector2(x, avH);

                if (x == avV || x == streetV1 || x == streetV2)
                    _mapManager.AddSprite("Road_Cruzamento", posAv + ajuste,
                        layer: 2, scale: scaleCruzamento * 1.04f);
                else
                    _mapManager.AddSprite("Road_Horizontal", posAv,
                        layer: 0, scale: scaleRua);

                Vector2 posH1 = new Vector2(x, streetH1);
                Vector2 posH2 = new Vector2(x, streetH2);

                if (x == avV || x == streetV1 || x == streetV2)
                {
                    _mapManager.AddSprite("Road_Cruzamento", posH1 + ajuste,
                        layer: 1, scale: scaleCruzamento * 1.04f);
                    _mapManager.AddSprite("Road_Cruzamento", posH2 + ajuste,
                        layer: 1, scale: scaleCruzamento * 1.04f);
                }
                else
                {
                    _mapManager.AddSprite("Road_Horizontal", posH1,
                        layer: 0, scale: scaleRua);
                    _mapManager.AddSprite("Road_Horizontal", posH2,
                        layer: 0, scale: scaleRua);
                }
            }

            for (int y = 0; y < worldH; y += seg)
            {
                if (y == avH || y == streetH1 || y == streetH2) continue;
                float rot = MathHelper.PiOver2;
                _mapManager.AddSprite("Road_Horizontal", new Vector2(avV,      y),
                    layer: 0, scale: scaleRua, rotation: rot);
                _mapManager.AddSprite("Road_Horizontal", new Vector2(streetV1, y),
                    layer: 0, scale: scaleRua, rotation: rot);
                _mapManager.AddSprite("Road_Horizontal", new Vector2(streetV2, y),
                    layer: 0, scale: scaleRua, rotation: rot);
            }

            Console.WriteLine(
                $"[ExplorationState] {_mapManager._sprites.Count} sprites criados.");
        }

        private void DrawDebugGrid(SpriteBatch spriteBatch)
        {
            int   gridSize  = 100;
            Color gridColor = new Color(30, 30, 40);
            for (int x = 0; x < 5000; x += gridSize)
                spriteBatch.Draw(_pixelTexture, new Rectangle(x, 0, 2, 5000), gridColor);
            for (int y = 0; y < 5000; y += gridSize)
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, y, 5000, 2), gridColor);
        }

        private void DrawWorldBounds(SpriteBatch spriteBatch)
        {
            int   t = 8;
            Color c = Color.Red;
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(_worldBounds.Left,      _worldBounds.Top,        _worldBounds.Width, t), c);
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(_worldBounds.Left,      _worldBounds.Bottom - t, _worldBounds.Width, t), c);
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(_worldBounds.Left,      _worldBounds.Top,        t, _worldBounds.Height), c);
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(_worldBounds.Right - t, _worldBounds.Top,        t, _worldBounds.Height), c);
        }
    }
}