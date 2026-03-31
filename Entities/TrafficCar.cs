using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Entities
{
    public enum TrafficVariant
    {
        Sedan,
        Van,
        Pickup
    }

    public class TrafficCar
    {
        public Vector2   Position { get; private set; }
        public float     Rotation { get; private set; }
        public bool      IsActive { get; private set; } = true;
        public Rectangle Bounds   => GetBounds();

        private readonly int            _carWidth;
        private readonly int            _carHeight;
        private readonly TrafficVariant _variant;
        private readonly Color          _bodyColor;

        private readonly List<Vector2> _waypoints;
        private readonly bool          _isLoop;
        private int   _targetIndex = 1;
        private const float ArrivalThreshold = 18f;

        private readonly float _baseSpeed;
        private float _speed;
        private const float MaxSpeed  = 95f;
        private const float TurnSpeed = 3.5f;

        private float _trafficSlowTimer = 0f;
        private readonly Random _rng;

        public const float CollisionDamage       = 8f;
        private float      _collisionCooldown    = 0f;
        private const float CollisionCooldownTime = 1.2f;

        public TrafficCar(List<Vector2> waypoints, bool isLoop, TrafficVariant variant, Random rng)
        {
            _waypoints = waypoints;
            _isLoop    = isLoop;
            _variant   = variant;
            _rng       = rng;

            Position   = waypoints.Count > 0 ? waypoints[0] : Vector2.Zero;
            Rotation   = 0f;
            _baseSpeed = MaxSpeed * (0.55f + (float)rng.NextDouble() * 0.35f);
            _speed     = _baseSpeed;

            switch (variant)
            {
                case TrafficVariant.Sedan:  _carWidth = 24; _carHeight = 44; break;
                case TrafficVariant.Van:    _carWidth = 30; _carHeight = 52; break;
                case TrafficVariant.Pickup: _carWidth = 22; _carHeight = 56; break;
                default:                   _carWidth = 24; _carHeight = 44; break;
            }

            _bodyColor = PickBodyColor(variant, rng);
        }

        public void SlowForTraffic(float duration = 0.5f) =>
            _trafficSlowTimer = MathF.Max(_trafficSlowTimer, duration);

        public void SetPosition(Vector2 pos) => Position = pos;

        public void Update(GameTime gameTime)
        {
            if (!IsActive || _waypoints.Count < 2) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            if (dist > 1f)
            {
                float targetAngle = MathF.Atan2(toTarget.X, -toTarget.Y);
                float angleDiff   = NormalizeAngle(targetAngle - Rotation);
                float maxTurn     = TurnSpeed * dt;
                Rotation += MathHelper.Clamp(angleDiff, -maxTurn, maxTurn);
                Rotation  = NormalizeAngle(Rotation);
            }

            Vector2 dir = new Vector2(MathF.Sin(Rotation), -MathF.Cos(Rotation));
            Position += dir * _speed * dt;

            if (dist < ArrivalThreshold)
                AdvanceWaypoint();
        }

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
                    // Chegou ao fim: volta ao início com delay aleatório para evitar sincronização
                    Position          = _waypoints[0];
                    _targetIndex      = 1;
                    _trafficSlowTimer = MathF.Max(_trafficSlowTimer, (float)_rng.NextDouble() * 1.5f + 0.3f);
                }
            }
        }

        public float CheckPlayerCollision(Rectangle playerBounds)
        {
            if (!IsActive)                        return 0f;
            if (!Bounds.Intersects(playerBounds)) return 0f;
            if (_collisionCooldown > 0f)          return 0f;

            _collisionCooldown = CollisionCooldownTime;
            SlowForTraffic(1.2f);
            return CollisionDamage;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!IsActive) return;

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

            Vector2 frontOffset = new Vector2(MathF.Sin(Rotation), -MathF.Cos(Rotation)) * (_carHeight / 2f + 2f);
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

        private Rectangle GetBounds()
        {
            return new Rectangle(
                (int)(Position.X - _carWidth  / 2f),
                (int)(Position.Y - _carHeight / 2f),
                _carWidth,
                _carHeight
            );
        }

        private float NormalizeAngle(float angle)
        {
            while (angle >  MathHelper.Pi)  angle -= MathHelper.TwoPi;
            while (angle < -MathHelper.Pi)  angle += MathHelper.TwoPi;
            return angle;
        }

        private static Color PickBodyColor(TrafficVariant variant, Random rng)
        {
            Color[][] palettes = new Color[][]
            {
                new Color[] {
                    new Color(80,  80,  90),
                    new Color(50,  50,  65),
                    new Color(90,  60,  60),
                    new Color(70,  85,  70),
                    new Color(110, 100, 80),
                },
                new Color[] {
                    new Color(60,  100, 140),
                    new Color(130, 80,  40),
                    new Color(50,  120, 60),
                    new Color(100, 95,  85),
                    new Color(60,  55,  70),
                },
                new Color[] {
                    new Color(75,  65,  50),
                    new Color(110, 85,  55),
                    new Color(55,  70,  80),
                    new Color(90,  45,  30),
                    new Color(65,  65,  65),
                },
            };

            return palettes[(int)variant][rng.Next(palettes[(int)variant].Length)];
        }

        public void Deactivate() => IsActive = false;
    }
}