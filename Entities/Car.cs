using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BurnoutCity.Entities
{
    public class Car
    {
        public Vector2  Position { get; private set; }
        public float    Rotation { get; private set; }
        public Rectangle Bounds => GetBounds();

        private Vector2 _velocity;
        private float   _speed;

        public CarStats Stats { get; private set; }

        public Color CarColor { get; set; } = Color.OrangeRed;
        private const int CarWidth  = 24;
        private const int CarHeight = 44;

        private bool _isAccelerating;
        private bool _isBraking;

        // ── Efeitos visuais ───────────────────────────────────────────────────
        private CarEffects? _effects;
        public bool IsNitroActive { get; private set; } = false;

        // Física
        private const float Friction       = 0.88f;
        private const float TurnSpeed      = 2.8f;
        private const float MinSpeedToTurn = 10f;

        public Car(Vector2 spawnpoint, CarStats? stats = null)
        {
            Position  = spawnpoint;
            Stats     = stats ?? new CarStats();
            Rotation  = 0f;
            _velocity = Vector2.Zero;
            _speed    = 0f;
        }

        /// <summary>
        /// Chama depois de carregar as texturas para activar os efeitos visuais.
        /// </summary>
        public void LoadEffects(Texture2D smokeSheet, Texture2D nitroSheet, Texture2D wheelSheet)
        {
            _effects = new CarEffects(smokeSheet, nitroSheet, wheelSheet);
        }

        public void Update(GameTime gameTime)
        {
            float delta    = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboard = Keyboard.GetState();

            HandleInput(keyboard, delta);
            ApplyFriction();
            ApplyMovement(delta);

            // Detectar derrapagem (curva a alta velocidade)
            bool isSkidding = Math.Abs(_speed) > 80f &&
                              (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D));

            _effects?.Update(gameTime, Position, Rotation, _speed,
                isBraking: _isBraking, isSkidding: isSkidding, nitroActive: IsNitroActive);
        }

        private void HandleInput(KeyboardState keyboard, float delta)
        {
            bool accelerating  = keyboard.IsKeyDown(Keys.W);
            bool braking       = keyboard.IsKeyDown(Keys.S);
            bool turningLeft   = keyboard.IsKeyDown(Keys.A);
            bool turningRight  = keyboard.IsKeyDown(Keys.D);

            _isBraking = braking && _speed > 10f;

            if (accelerating)
            {
                float effectiveMaxSpeed = Stats.MaxSpeed * getDamageSpeedMultiplier();
                if (_speed < effectiveMaxSpeed)
                {
                    _speed += Stats.Acceleration * delta;
                    _speed  = MathHelper.Min(_speed, effectiveMaxSpeed);
                }
            }

            if (braking)
            {
                if (_speed > 0f)
                {
                    _speed -= Stats.Acceleration * 1.5f * delta;
                    _speed  = MathHelper.Max(_speed, 0f);
                }
                else
                {
                    _speed -= Stats.Acceleration * 0.5f * delta;
                    _speed  = MathHelper.Max(_speed, -Stats.MaxSpeed * 0.4f);
                }
            }

            if (Math.Abs(_speed) > MinSpeedToTurn)
            {
                float speedRatio       = Math.Abs(_speed) / Stats.MaxSpeed;
                float currentTurnSpeed = TurnSpeed * Stats.Handling * speedRatio * delta;
                float turnDirection    = _speed < 0 ? -1f : 1f;

                if (turningLeft)  Rotation -= currentTurnSpeed * turnDirection;
                if (turningRight) Rotation += currentTurnSpeed * turnDirection;

                Rotation = NormalizeAngle(Rotation);
                _isAccelerating = accelerating || braking;
            }
        }

        private void ApplyFriction()
        {
            if (!_isAccelerating)
            {
                _speed *= Friction;
                if (Math.Abs(_speed) < 0.5f) _speed = 0f;
            }
        }

        private void ApplyMovement(float delta)
        {
            if (MathF.Abs(_speed) < 0.1f) { _speed = 0f; return; }
            if (_speed == 0f) return;

            _velocity = new Vector2(
                MathF.Sin(Rotation) * _speed,
               -MathF.Cos(Rotation) * _speed
            );
            Position += _velocity * delta;
        }

        public void ApplyCollisionDamage(float amount)
        {
            Stats.CurrentDamage = MathHelper.Min(Stats.CurrentDamage + amount, 100f);
        }

        public void Repair() => Stats.CurrentDamage = 0f;

        public void SetPosition(Vector2 newPosition)
        {
            Position = newPosition;
            _speed  *= -0.4f;
        }

        private float getDamageSpeedMultiplier()
        {
            if (Stats.CurrentDamage >= 75f) return 0.4f;
            if (Stats.CurrentDamage >= 50f) return 0.7f;
            return 1f;
        }

        private Rectangle GetBounds() => new Rectangle(
            (int)(Position.X - CarWidth  / 2f),
            (int)(Position.Y - CarHeight / 2f),
            CarWidth, CarHeight
        );

        public float CurrentSpeed => _speed;

        private float NormalizeAngle(float angle)
        {
            while (angle >  MathHelper.Pi) angle -= MathHelper.TwoPi;
            while (angle < -MathHelper.Pi) angle += MathHelper.TwoPi;
            return angle;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            // 1. Efeitos por baixo do carro (rodas + fumo)
            _effects?.DrawBehindCar(spriteBatch, Position, Rotation);

            // 2. Corpo do carro
            spriteBatch.Draw(
                texture:         pixelTexture,
                position:        Position,
                sourceRectangle: new Rectangle(0, 0, 1, 1),
                color:           CarColor,
                rotation:        Rotation,
                origin:          new Vector2(0.5f, 0.5f),
                scale:           new Vector2(CarWidth, CarHeight),
                effects:         SpriteEffects.None,
                layerDepth:      0f
            );

            // Indicador da frente do carro
            Vector2 frontOffset = new Vector2(
                MathF.Sin(Rotation),
               -MathF.Cos(Rotation)
            ) * (CarHeight / 2f + 2f);

            spriteBatch.Draw(
                texture:         pixelTexture,
                position:        Position + frontOffset,
                sourceRectangle: new Rectangle(0, 0, 1, 1),
                color:           Color.Orange,
                rotation:        0f,
                origin:          new Vector2(0.5f, 0.5f),
                scale:           new Vector2(8, 8),
                effects:         SpriteEffects.None,
                layerDepth:      0f
            );

            // 3. Efeito de nitro (por cima)
            _effects?.DrawInFrontOfCar(spriteBatch, Position, Rotation, IsNitroActive);
        }
    }
}