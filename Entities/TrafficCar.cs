using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Entities
{
    // As 3 variantes de carro de tráfego que vão ter sprite
    public enum TrafficVariant
    {
        Sedan,   // traffic_car_a.png — carro compacto normal
        SUV,     // traffic_car_b.png — SUV/crossover
        Hatch    // traffic_car_c.png — hatchback pequeno
    }

    public class TrafficCar
    {
        // ── Posição e estado ──────────────────────────────────────────────────
        public Vector2   Position { get; private set; }
        public float     Rotation { get; private set; }
        public bool      IsActive { get; private set; } = true;
        public TrafficVariant Variant { get; private set; }
        public Rectangle Bounds   => GetBounds();

        // ── Dimensões do carro no mundo (em píxeis) ───────────────────────────
        private readonly int _carWidth;
        private readonly int _carHeight;

        // ── Variante e cor de fallback ─────────────────────────────────────────
        private readonly TrafficVariant _variant;
        private readonly Color          _bodyColor; // usado só se não houver sprite

        // ── Sprite atribuído pelo TrafficManager ───────────────────────────────
        // Pode ser null enquanto os ficheiros PNG ainda não existirem —
        // nesse caso o Draw faz fallback para o retângulo colorido original.
        private Texture2D? _sprite;

        // ── Waypoints / navegação ─────────────────────────────────────────────
        private readonly List<Vector2> _waypoints;
        private readonly bool          _isLoop;
        private int         _targetIndex  = 1;
        private const float ArrivalThreshold = 18f;

        // ── Velocidade ────────────────────────────────────────────────────────
        private readonly float _baseSpeed;
        private float          _speed;
        private const float    MaxSpeed  = 95f;
        private const float    TurnSpeed = 3.5f;

        private float          _trafficSlowTimer = 0f;
        private readonly Random _rng;

        // ── Colisão com o jogador ─────────────────────────────────────────────
        public const float  CollisionDamage       = 8f;
        private float       _collisionCooldown    = 0f;
        private const float CollisionCooldownTime = 1.2f;

        // ─────────────────────────────────────────────────────────────────────
        //  CONSTRUTOR
        // ─────────────────────────────────────────────────────────────────────
        public TrafficCar(List<Vector2> waypoints, bool isLoop, TrafficVariant variant, Random rng)
        {
            _waypoints = waypoints;
            _isLoop    = isLoop;
            _variant   = variant;
            Variant = variant;
            _rng       = rng;

            Position   = waypoints.Count > 0 ? waypoints[0] : Vector2.Zero;
            Rotation   = 0f;

            // Cada variante tem dimensões ligeiramente diferentes
            switch (variant)
            {
                case TrafficVariant.SUV:
                    _carWidth  = 44;  // mais largo
                    _carHeight = 80;  // mais comprido
                    break;
                case TrafficVariant.Hatch:
                    _carWidth  = 34;  // mais estreito
                    _carHeight = 65;  // mais curto
                    break;
                default: // Sedan
                    _carWidth  = 38;
                    _carHeight = 75;
                    break;
            }

            _bodyColor = PickBodyColor(variant, rng);
            _baseSpeed = MaxSpeed * (0.55f + (float)rng.NextDouble() * 0.35f);
            _speed     = _baseSpeed;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ATRIBUIR SPRITE
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Chamado pelo TrafficManager depois de carregar as texturas.
        /// Passa a textura certa para esta variante de carro.
        /// </summary>
        public void SetSprite(Texture2D? texture)
        {
            _sprite = texture;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MÉTODOS PÚBLICOS DE CONTROLO
        // ─────────────────────────────────────────────────────────────────────
        public void SlowForTraffic(float duration = 0.5f) =>
            _trafficSlowTimer = MathF.Max(_trafficSlowTimer, duration);

        public void SetPosition(Vector2 pos) => Position = pos;

        public void Deactivate() => IsActive = false;

        // ─────────────────────────────────────────────────────────────────────
        //  UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public void Update(GameTime gameTime)
        {
            if (!IsActive || _waypoints.Count < 2) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Cooldown de colisão
            if (_collisionCooldown > 0f) _collisionCooldown -= dt;

            // Gerir velocidade: abrandar por tráfego ou recuperar gradualmente
            if (_trafficSlowTimer > 0f)
            {
                _trafficSlowTimer -= dt;
                _speed = _baseSpeed * 0.08f;
            }
            else
            {
                _speed = MathF.Min(_speed + _baseSpeed * 2f * dt, _baseSpeed);
            }

            Vector2 target   = _waypoints[_targetIndex];
            Vector2 toTarget = target - Position;
            float   dist     = toTarget.Length();

            // Rodar suavemente em direção ao próximo waypoint
            if (dist > 1f)
            {
                float targetAngle = MathF.Atan2(toTarget.X, -toTarget.Y);
                float angleDiff   = NormalizeAngle(targetAngle - Rotation);
                float maxTurn     = TurnSpeed * dt;
                Rotation += MathHelper.Clamp(angleDiff, -maxTurn, maxTurn);
                Rotation  = NormalizeAngle(Rotation);
            }

            // Mover para a frente
            Vector2 dir = new Vector2(MathF.Sin(Rotation), -MathF.Cos(Rotation));
            Position += dir * _speed * dt;

            // Verificar chegada ao waypoint
            if (dist < ArrivalThreshold)
                AdvanceWaypoint();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  COLISÃO COM JOGADOR
        // ─────────────────────────────────────────────────────────────────────
        public float CheckPlayerCollision(Rectangle playerBounds)
        {
            if (!IsActive)                      return 0f;
            if (!Bounds.Intersects(playerBounds)) return 0f;
            if (_collisionCooldown > 0f)         return 0f;

            _collisionCooldown = CollisionCooldownTime;
            SlowForTraffic(1.2f);
            return CollisionDamage;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DRAW
        // ─────────────────────────────────────────────────────────────────────
        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!IsActive) return;

            if (_sprite != null)
            {
                // ── Desenha o sprite PNG real ─────────────────────────────────
                // Calcula escala para que o sprite ocupe as dimensões do carro
               // Escala baseada numa altura alvo de 48px no mundo independentemente do tamanho do PNG
                float uniformScale = 75f / _sprite.Height;
                float scaleX = uniformScale;
                float scaleY = uniformScale;

                spriteBatch.Draw(
                    texture:         _sprite,
                    position:        Position,
                    sourceRectangle: null,                                        // textura inteira
                    color:           Color.White,                                 // sem tint
                    rotation: Rotation + MathHelper.PiOver2, // PNGs estão orientados para cima, mas queremos que apontem para a direção de movimento
                    origin:          new Vector2(_sprite.Width / 2f, _sprite.Height / 2f),
                    scale:           new Vector2(scaleX, scaleY),
                    effects:         SpriteEffects.None,
                    layerDepth:      0f
                );
            }
            else
            {
                // ── Fallback: retângulo colorido (usado enquanto não há sprites) ──
                spriteBatch.Draw(
                    texture:         pixel,
                    position:        Position,
                    sourceRectangle: new Rectangle(0, 0, 1, 1),
                    color:           _bodyColor,
                    rotation:        Rotation,
                    origin:          new Vector2(0.5f, 0.5f),
                    scale:           new Vector2(_carWidth, _carHeight),
                    effects:         SpriteEffects.None,
                    layerDepth:      0f
                );

                // Indicador laranja na frente (debug visual)
                Vector2 frontOffset = new Vector2(MathF.Sin(Rotation), -MathF.Cos(Rotation))
                                      * (_carHeight / 2f + 2f);
                spriteBatch.Draw(
                    texture:         pixel,
                    position:        Position + frontOffset,
                    sourceRectangle: new Rectangle(0, 0, 1, 1),
                    color:           Color.Orange * 0.8f,
                    rotation:        0f,
                    origin:          new Vector2(0.5f, 0.5f),
                    scale:           new Vector2(5, 5),
                    effects:         SpriteEffects.None,
                    layerDepth:      0f
                );
            }

    
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PRIVADOS
        // ─────────────────────────────────────────────────────────────────────
        private void AdvanceWaypoint()
        {
            if (_isLoop)
            {
                _targetIndex = (_targetIndex + 1) % _waypoints.Count;
            }
            else
            {
                if (_targetIndex < _waypoints.Count - 1)
                {
                    _targetIndex++;
                }
                else
                {
                    // Chegou ao fim do path: volta ao início com pequeno delay aleatório
                    Position          = _waypoints[0];
                    _targetIndex      = 1;
                    _trafficSlowTimer = MathF.Max(_trafficSlowTimer, (float)_rng.NextDouble() * 1.5f + 0.3f);
                }
            }
        }

        private Rectangle GetBounds()
        {
            // Calcula o AABB que envolve o sprite rodado.
            // Os 4 cantos do carro (sem rotação, centrados na origem):
            float hw = _carWidth  / 2f;
            float hh = _carHeight / 2f;

            // Projeção do retângulo rodado: quando um rectângulo roda, o seu AABB
            // tem metade-largura = |hw*cos| + |hh*sin|  e metade-altura = |hw*sin| + |hh*cos|
            float cos = MathF.Abs(MathF.Cos(Rotation));
            float sin = MathF.Abs(MathF.Sin(Rotation));

            int aabbW = (int)(hw * cos + hh * sin) * 2;
            int aabbH = (int)(hw * sin + hh * cos) * 2;

            return new Rectangle(
                (int)(Position.X - aabbW / 2f),
                (int)(Position.Y - aabbH / 2f),
                aabbW,
                aabbH
            );
        }

        private float NormalizeAngle(float angle)
        {
            while (angle >  MathHelper.Pi)  angle -= MathHelper.TwoPi;
            while (angle < -MathHelper.Pi)  angle += MathHelper.TwoPi;
            return angle;
        }

        // Paleta de cores usada no fallback (sem sprites)
        private static Color PickBodyColor(TrafficVariant variant, Random rng)
        {
            Color[][] palettes = new Color[][]
            {
                // Sedan — tons cinzentos/neutros
                new Color[] {
                    new Color(80,  80,  90),
                    new Color(50,  50,  65),
                    new Color(90,  60,  60),
                    new Color(70,  85,  70),
                    new Color(110, 100, 80),
                },
                // SUV — tons azulados/castanhos
                new Color[] {
                    new Color(60,  100, 140),
                    new Color(130, 80,  40),
                    new Color(50,  120, 60),
                    new Color(100, 95,  85),
                    new Color(60,  55,  70),
                },
                // Hatch — tons terrosos/escuros
                new Color[] {
                    new Color(75,  65,  50),
                    new Color(110, 85,  55),
                    new Color(55,  70,  80),
                    new Color(90,  45,  30),
                    new Color(65,  65,  65),
                },
            };

            int idx = Math.Min((int)variant, palettes.Length - 1);
            return palettes[idx][rng.Next(palettes[idx].Length)];
        }
    }
}