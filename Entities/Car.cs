using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BurnoutCity.Entities
{
    public class Car
    {
        // ── Posição e Rotação ────────────────────────────────────────────────────
        public Vector2 Position { get; private set; }
        public float Rotation { get; private set; }

        // Retângulo de colisão AABB baseado na posição atual
        public Rectangle Bounds => GetBounds();

        // ── Física ───────────────────────────────────────────────────────────────
        private Vector2 _velocity;
        private float _speed;

        private const float Friction       = 0.88f; // Atrito que desacelera o carro quando não está a acelerar
        private const float TurnSpeed      = 2.8f;  // Velocidade de rotação do carro (radianos/segundo escalonados)
        private const float MinSpeedToTurn = 10f;   // Velocidade mínima para permitir rotação (evita girar no lugar)

        private bool _isAccelerating; // Flag que indica se o carro está a receber input de aceleração
        private bool _isBraking;      // Flag que indica se o carro está a travar

        // ── Estatísticas ─────────────────────────────────────────────────────────
        public CarStats Stats { get; private set; }

        // ── Visual: dimensões de colisão (mantidas independentes do sprite) ──────
        public Color CarColor { get; set; } = Color.OrangeRed; // Cor de fallback quando não há sprite
        private const int CarWidth        = 60; // Largura do Sprite em pixels
        private const int CarHeight       = 70; // Altura do Sprite em pixels
        private const int CollisionWidth  = 38; // Hitbox largura
        private const int CollisionHeight = 38; // Hitbox altura

        // ── Sprite Sheet ─────────────────────────────────────────────────────────
        // O ficheiro car.png contém 4 variantes do carro lado a lado
        // Cada frame mede 64x64 pixels (total ~256x64)
        private Texture2D _spriteSheet;     // Textura carregada externamente via SetSpriteSheet()
        private const int FrameWidth  = 64; // Largura de um frame no sprite sheet
        private const int FrameHeight = 64; // Altura de um frame no sprite sheet
        private int _spriteVariant = 0;     // Índice da variante a usar (0 = primeira, à esquerda)

        // ── Efeitos visuais (fumo, nitro, rodas) ─────────────────────────────────
        private CarEffects? _effects;

        // ── Nitro ────────────────────────────────────────────────────────────────
        public bool IsNitroActive { get; private set; }
        private float _nitroAmount         = 100f;
        private const float NitroDepletionRate = 40f; // unidades por segundo enquanto ativo
        private const float NitroRechargeRate  = 12f; // unidades por segundo ao recarregar
        private const float NitroSpeedBoost    = 80f; // boost de velocidade máxima com nitro

        // ── Construtor ───────────────────────────────────────────────────────────
        public Car(Vector2 spawnpoint, CarStats? stats = null)
        {
            Position  = spawnpoint;
            Stats     = stats ?? new CarStats();
            Rotation  = 0f;
            _velocity = Vector2.Zero;
            _speed    = 0f;
        }

        // ── Sprite Sheet: método público para atribuir a textura ─────────────────
        /// <summary>
        /// Atribui o sprite sheet do carro carregado pelo Content Pipeline.
        /// Deve ser chamado no LoadContent do estado que cria este Car.
        /// </summary>
        public void SetSpriteSheet(Texture2D spriteSheet)
        {
            _spriteSheet = spriteSheet;
        }

        /// <summary>
        /// Define qual das 4 variantes do sprite sheet será usada (0 a 3).
        /// </summary>
        public void SetSpriteVariant(int variant)
        {
            _spriteVariant = Math.Clamp(variant, 0, 3);
        }

        // ── Efeitos visuais ───────────────────────────────────────────────────────
        /// <summary>
        /// Carrega os efeitos visuais do carro (fumo, nitro, rodas).
        /// Chamado pela ExplorationState após criar o carro.
        /// </summary>
        public void LoadEffects(Texture2D smokeSheet, Texture2D nitroSheet, Texture2D wheelSheet)
        {
            _effects = new CarEffects(smokeSheet, nitroSheet, wheelSheet);
        }

        // ── Update principal ─────────────────────────────────────────────────────
        public void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboard = Keyboard.GetState();

            HandleInput(keyboard, delta);
            HandleNitro(keyboard, delta);
            ApplyFriction();
            ApplyMovement(delta);

            // Detectar derrapagem (curva a alta velocidade)
            bool isSkidding = Math.Abs(_speed) > 80f &&
                              (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D));

            _effects?.Update(gameTime, Position, Rotation, _speed,
                isBraking: _isBraking, isSkidding: isSkidding, nitroActive: IsNitroActive);
        }

        // ── Leitura de input e aceleração/travagem ───────────────────────────────
        private void HandleInput(KeyboardState keyboard, float delta)
        {
            bool accelerating = keyboard.IsKeyDown(Keys.W);
            bool braking      = keyboard.IsKeyDown(Keys.S);
            bool turningLeft  = keyboard.IsKeyDown(Keys.A);
            bool turningRight = keyboard.IsKeyDown(Keys.D);

            _isBraking = braking && _speed > 20f; // só considera "a travar" se tiver velocidade

            if (accelerating)
            {
                // Velocidade máxima reduzida se o carro estiver danificado
                float effectiveMaxSpeed = Stats.MaxSpeed * getDamageSpeedMultiplier();
                if (IsNitroActive) effectiveMaxSpeed += NitroSpeedBoost;

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
                    // Travagem à frente: desacelera mais rápido que a aceleração
                    _speed -= Stats.Acceleration * 1.5f * delta;
                    _speed  = MathHelper.Max(_speed, 0f);
                }
                else
                {
                    // Marcha atrás: velocidade máxima reversa é 40% da velocidade normal
                    _speed -= Stats.Acceleration * 0.5f * delta;
                    _speed  = MathHelper.Max(_speed, -Stats.MaxSpeed * 0.4f);
                }
            }

            // Rotação: só funciona acima da velocidade mínima para não girar no lugar
            if (Math.Abs(_speed) > MinSpeedToTurn)
            {
                float speedRatio       = Math.Abs(_speed) / Stats.MaxSpeed;
                float currentTurnSpeed = TurnSpeed * Stats.Handling * speedRatio * delta;
                float turnDirection    = _speed < 0 ? -1f : 1f; // inverte em marcha atrás

                if (turningLeft)  Rotation -= currentTurnSpeed * turnDirection;
                if (turningRight) Rotation += currentTurnSpeed * turnDirection;

                Rotation = NormalizeAngle(Rotation);
            }

            _isAccelerating = accelerating || braking;
        }

        // ── Gestão do nitro ──────────────────────────────────────────────────────
        private void HandleNitro(KeyboardState keyboard, float delta)
        {
            if (keyboard.IsKeyDown(Keys.N) && _nitroAmount > 0f)
            {
                IsNitroActive = true;
                _nitroAmount -= NitroDepletionRate * delta;
                _nitroAmount  = MathHelper.Max(_nitroAmount, 0f);
            }
            else
            {
                IsNitroActive = false;
                _nitroAmount += NitroRechargeRate * delta;
                _nitroAmount  = MathHelper.Min(_nitroAmount, 100f);
            }
        }

        // ── Atrito: desacelera o carro quando não há input ───────────────────────
        private void ApplyFriction()
        {
            if (!_isAccelerating)
            {
                _speed *= Friction;

                if (Math.Abs(_speed) < 0.5f)
                    _speed = 0f;
            }
        }

        // ── Movimento: aplica velocidade à posição ───────────────────────────────
        private void ApplyMovement(float delta)
        {
            if (MathF.Abs(_speed) < 0.1f)
            {
                _speed = 0f;
                return;
            }

            _velocity = new Vector2(
                 MathF.Sin(Rotation) * _speed,
                -MathF.Cos(Rotation) * _speed
            );

            Position += _velocity * delta;
        }

        // ── Dano e Reparação ─────────────────────────────────────────────────────
        public void ApplyCollisionDamage(float amount)
        {
            Stats.CurrentDamage = MathHelper.Min(Stats.CurrentDamage + amount, 100f);
        }

        public void Repair()
        {
            Stats.CurrentDamage = 0f;
        }

        public void SetPosition(Vector2 newPosition)
        {
            Position = newPosition;
            _speed  *= -0.4f;
        }

        // ── Multiplicador de velocidade por dano ─────────────────────────────────
        private float getDamageSpeedMultiplier()
        {
            if (Stats.CurrentDamage >= 75f) return 0.4f;
            if (Stats.CurrentDamage >= 50f) return 0.7f;
            return 1f;
        }

        // ── Normalização de ângulo ────────────────────────────────────────────────
        private float NormalizeAngle(float angle)
        {
            while (angle >  MathHelper.Pi) angle -= MathHelper.TwoPi;
            while (angle < -MathHelper.Pi) angle += MathHelper.TwoPi;
            return angle;
        }

        // ── Retângulo de colisão AABB ─────────────────────────────────────────────
        private Rectangle GetBounds()
        {
            return new Rectangle(
                (int)(Position.X - CollisionWidth  / 2f),
                (int)(Position.Y - CollisionHeight / 2f),
                CollisionWidth,
                CollisionHeight
            );
        }

        // ── Propriedades de leitura ───────────────────────────────────────────────
        public float CurrentSpeed  => _speed;
        public float NitroPercent  => _nitroAmount;

        // ── Draw ──────────────────────────────────────────────────────────────────
        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            // Efeitos por trás do carro (rodas, fumo)
            _effects?.DrawBehindCar(spriteBatch, Position, Rotation);

            if (_spriteSheet != null)
            {
                // Recorta o frame correto do sprite sheet
                Rectangle sourceRect = new Rectangle(
                    _spriteVariant * FrameWidth,
                    0,
                    FrameWidth,
                    FrameHeight
                );

                Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

                float scaleX = CarWidth  / (float)FrameWidth;
                float scaleY = CarHeight / (float)FrameHeight;

                spriteBatch.Draw(
                    texture:         _spriteSheet,
                    position:        Position,
                    sourceRectangle: sourceRect,
                    color:           Color.White,
                    rotation:        Rotation,
                    origin:          origin,
                    scale:           new Vector2(scaleX, scaleY),
                    effects:         SpriteEffects.None,
                    layerDepth:      0f
                );
            }
            else
            {
                // Fallback: retângulo colorido quando não há sprite
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

                // Indicador laranja na frente do carro (debug visual)
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
            }

            // Efeitos por cima do carro (chama do nitro)
            _effects?.DrawInFrontOfCar(spriteBatch, Position, Rotation, IsNitroActive);
        }
    }
}