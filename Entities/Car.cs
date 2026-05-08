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

        // ── Estatísticas ─────────────────────────────────────────────────────────
        public CarStats Stats { get; private set; }

        // ── Visual: dimensões de colisão (mantidas independentes do sprite) ──────
        public Color CarColor { get; set; } = Color.OrangeRed; // Cor de fallback quando não há sprite
        private const int CarWidth  = 60; // Largura do Sprite em pixels
        private const int CarHeight = 60    ; // Altura do Sprite colisão em pixels
        private const int CollisionWidth  = 25; // Hitbox 
        private const int CollisionHeight = 38; // Hitbox 

        // ── Sprite Sheet ─────────────────────────────────────────────────────────
        // O ficheiro car.png contém 4 variantes do carro lado a lado
        // Cada frame mede 64x64 pixels (total ~256x64)
        private Texture2D _spriteSheet;          // Textura carregada externamente via SetSpriteSheet()
        private const int FrameWidth  = 64;      // Largura de um frame no sprite sheet
        private const int FrameHeight = 64;      // Altura de um frame no sprite sheet
        private int _spriteVariant = 0;          // Índice da variante a usar (0 = primeira, à esquerda)

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
        /// Deve ser chamado no LoadContent/Initialize do estado que cria este Car.
        /// Exemplo: _playerCar.SetSpriteSheet(Content.Load<Texture2D>("car"));
        /// </summary>
        public void SetSpriteSheet(Texture2D spriteSheet)
        {
            _spriteSheet = spriteSheet;
        }

        /// <summary>
        /// Define qual das 4 variantes do sprite sheet será usada (0 a 3).
        /// Útil para trocar o aspeto do carro com upgrades ou customização.
        /// </summary>
        public void SetSpriteVariant(int variant)
        {
            _spriteVariant = Math.Clamp(variant, 0, 3); // Garante que está dentro dos limites (0-3)
        }

        // ── Update principal ─────────────────────────────────────────────────────
        public void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboard = Keyboard.GetState();

            HandleInput(keyboard, delta);
            ApplyFriction();
            ApplyMovement(delta);
        }

        // ── Leitura de input e aceleração/travagem ───────────────────────────────
        private void HandleInput(KeyboardState keyboard, float delta)
        {
            bool accelerating  = keyboard.IsKeyDown(Keys.W);
            bool braking       = keyboard.IsKeyDown(Keys.S);
            bool turningLeft   = keyboard.IsKeyDown(Keys.A);
            bool turningRight  = keyboard.IsKeyDown(Keys.D);

            if (accelerating)
            {
                // Velocidade máxima reduzida se o carro estiver danificado
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
                // Quanto mais rápido, menor o fator de rotação (comportamento realista)
                float speedRatio       = Math.Abs(_speed) / Stats.MaxSpeed;
                float currentTurnSpeed = TurnSpeed * Stats.Handling * speedRatio * delta;

                // Inverte a direção de rotação quando em marcha atrás
                float turnDirection = _speed < 0 ? -1f : 1f;

                if (turningLeft)
                    Rotation -= currentTurnSpeed * turnDirection;

                if (turningRight)
                    Rotation += currentTurnSpeed * turnDirection;

                Rotation = NormalizeAngle(Rotation); // Mantém o ângulo entre -PI e +PI
            }

            // Guarda o estado de aceleração para o sistema de atrito
            _isAccelerating = accelerating || braking;
        }

        // ── Atrito: desacelera o carro quando não há input ───────────────────────
        private void ApplyFriction()
        {
            if (!_isAccelerating)
            {
                _speed *= Friction; // Multiplica por fator < 1 a cada frame → desaceleração gradual

                // Threshold para parar completamente e evitar deslizamento infinito
                if (Math.Abs(_speed) < 0.5f)
                    _speed = 0f;
            }
        }

        // ── Movimento: aplica velocidade à posição ───────────────────────────────
        private void ApplyMovement(float delta)
        {
            // Não processa movimento a velocidades desprezíveis
            if (MathF.Abs(_speed) < 0.1f)
            {
                _speed = 0f;
                return;
            }

            // Calcula direção com base na rotação atual do carro
            _velocity = new Vector2(
                 MathF.Sin(Rotation) * _speed,  // Componente X (horizontal)
                -MathF.Cos(Rotation) * _speed   // Componente Y (vertical, invertido porque Y cresce para baixo)
            );

            Position += _velocity * delta; // Atualiza posição com base na velocidade e tempo decorrido
        }

        // ── Dano e Reparação ─────────────────────────────────────────────────────

        /// <summary>
        /// Aplica dano ao carro (por colisão com trânsito, etc).
        /// Máximo de 100% de dano.
        /// </summary>
        public void ApplyCollisionDamage(float amount)
        {
            Stats.CurrentDamage = MathHelper.Min(Stats.CurrentDamage + amount, 100f);
        }

        /// <summary>
        /// Repara completamente o carro (chamado na Garagem após pagamento).
        /// </summary>
        public void Repair()
        {
            Stats.CurrentDamage = 0f;
        }

        /// <summary>
        /// Reposiciona o carro (usado pelo CollisionManager após colisão com paredes).
        /// Aplica recuo na velocidade para simular impacto.
        /// </summary>
        public void SetPosition(Vector2 newPosition)
        {
            Position = newPosition;
            _speed  *= -0.4f; // Recuo de 40% da velocidade para simular impacto
        }

        // ── Multiplicador de velocidade por dano ─────────────────────────────────
        // Quanto mais danificado, mais lento fica o carro
        private float getDamageSpeedMultiplier()
        {
            if (Stats.CurrentDamage >= 75f) return 0.4f; // Dano crítico: 40% da velocidade
            if (Stats.CurrentDamage >= 50f) return 0.7f; // Dano moderado: 70% da velocidade
            return 1f;                                    // Sem dano: velocidade total
        }

        // ── Normalização de ângulo ────────────────────────────────────────────────
        // Mantém a rotação no intervalo [-PI, +PI] para cálculos corretos
        private float NormalizeAngle(float angle)
        {
            while (angle >  MathHelper.Pi) angle -= MathHelper.TwoPi;
            while (angle < -MathHelper.Pi) angle += MathHelper.TwoPi;
            return angle;
        }

        // ── Retângulo de colisão AABB ─────────────────────────────────────────────
        // Baseado na posição central do carro
        private Rectangle GetBounds()
        {
            return new Rectangle(
                (int)(Position.X - CarWidth  / 2f), // X = centro - metade da largura
                (int)(Position.Y - CarHeight / 2f), // Y = centro - metade da altura
                CollisionWidth,
                CollisionHeight
            );
        }

        // ── Propriedades de leitura ───────────────────────────────────────────────
        public float CurrentSpeed => _speed; // Velocidade atual (útil para HUD e lógica de raça)

        // ── Draw ──────────────────────────────────────────────────────────────────
        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (_spriteSheet != null)
            {
                // ── Modo Sprite Sheet ──────────────────────────────────────────
                // Recorta o frame correto do sprite sheet com base na variante escolhida
                // Cada variante ocupa um espaço de FrameWidth x FrameHeight pixels
                Rectangle sourceRect = new Rectangle(
                    _spriteVariant * FrameWidth, // Offset X: variante 0 = pixel 0, variante 1 = pixel 64, etc.
                    0,                            // Offset Y: só existe uma linha de frames
                    FrameWidth,
                    FrameHeight
                );

                // Origem no centro do frame para que a rotação seja em torno do centro do carro
                Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

                // Escala o sprite para corresponder às dimensões de colisão (CarWidth x CarHeight)
                // Assim o visual está sempre alinhado com a hitbox
                float scaleX = CarWidth  / (float)FrameWidth;
                float scaleY = CarHeight / (float)FrameHeight;

                spriteBatch.Draw(
                    texture:         _spriteSheet,
                    position:        Position,              // Centro do carro no mundo
                    sourceRectangle: sourceRect,            // Frame a usar do sprite sheet
                    color:           Color.White,           // White = sem tint, cores originais do sprite
                    rotation:        Rotation,              // Rotação atual do carro (radianos)
                    origin:          origin,                // Centro do frame como ponto de rotação
                    scale:           new Vector2(scaleX, scaleY), // Ajusta ao tamanho de colisão
                    effects:         SpriteEffects.None,
                    layerDepth:      0f
                );
            }
            else
            {
                // ── Fallback: retângulo colorido ───────────────────────────────
                // Usado quando o sprite sheet ainda não foi carregado
                // Mantido para não crashar durante desenvolvimento
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
        }
    }
}