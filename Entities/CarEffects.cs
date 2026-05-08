using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace BurnoutCity.Entities
{
    public class CarEffects
    {
        private SpriteAnimation _smokeAnim;
        private SpriteAnimation _nitroAnim;
        private SpriteAnimation _wheelAnim;

        private class SmokeParticle
        {
            public Vector2 Position;
            public float   Alpha;
            public float   Scale;
            public float   Life;
            public float   MaxLife;
            public SpriteAnimation Anim = null!;
        }

        private List<SmokeParticle> _particles = new();
        private float _smokeTimer = 0f;

        private const int CarWidth  = 24;
        private const int CarHeight = 44;

        private const int SmokeFrameW = 32;
        private const int SmokeFrameH = 32;
        private const int NitroFrameW = 48;
        private const int NitroFrameH = 24;
        private const int WheelSize   = 16;

        public CarEffects(Texture2D smokeSheet, Texture2D nitroSheet, Texture2D wheelSheet)
        {
            _smokeAnim      = new SpriteAnimation(smokeSheet, SmokeFrameW, SmokeFrameH, fps: 18f);
            _smokeAnim.Loop = true;

            _nitroAnim      = new SpriteAnimation(nitroSheet, NitroFrameW, NitroFrameH, fps: 20f);
            _nitroAnim.Loop = true;
            _nitroAnim.Play();

            _wheelAnim      = new SpriteAnimation(wheelSheet, WheelSize, WheelSize, fps: 12f);
            _wheelAnim.Loop = true;
            _wheelAnim.Play();
        }

        public void Update(
            GameTime gameTime,
            Vector2  carPosition,
            float    carRotation,
            float    speed,
            bool     isBraking,
            bool     isSkidding,
            bool     nitroActive)
        {
            float dt       = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float absSpeed = Math.Abs(speed);

            _wheelAnim.Alpha = absSpeed > 5f ? 1f : 0f;
            _wheelAnim.Update(gameTime);
            _nitroAnim.Update(gameTime);

            bool shouldSmoke = (isBraking && absSpeed > 30f) || (isSkidding && absSpeed > 60f);
            if (shouldSmoke)
            {
                _smokeTimer -= dt;
                if (_smokeTimer <= 0f)
                {
                    _smokeTimer = isBraking ? 0.04f : 0.07f;

                    Vector2 back  = GetBackOffset(carRotation);
                    Vector2 right = GetRightOffset(carRotation);

                    SpawnSmoke(carPosition + back * 18f + right * 8f);
                    SpawnSmoke(carPosition + back * 18f - right * 8f);
                }
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life  -= dt;
                p.Alpha  = MathHelper.Clamp(p.Life / p.MaxLife, 0f, 1f) * 0.85f;
                p.Scale += dt * 1.2f;
                p.Anim.Update(gameTime);

                if (p.Life <= 0f)
                    _particles.RemoveAt(i);
            }
        }

        private void SpawnSmoke(Vector2 position)
        {
            var anim   = new SpriteAnimation(_smokeAnim.GetTexture(), SmokeFrameW, SmokeFrameH, fps: 16f);
            anim.Loop  = true;
            anim.Scale = 0.6f;
            anim.Play();

            _particles.Add(new SmokeParticle
            {
                Position = position,
                Alpha    = 0.8f,
                Scale    = 0.6f,
                Life     = 0.5f,
                MaxLife  = 0.5f,
                Anim     = anim
            });
        }

        public void DrawBehindCar(SpriteBatch spriteBatch, Vector2 carPosition, float carRotation)
        {
            DrawWheels(spriteBatch, carPosition, carRotation);

            foreach (var p in _particles)
            {
                p.Anim.Alpha = p.Alpha;
                p.Anim.Scale = p.Scale;
                p.Anim.Draw(spriteBatch, p.Position);
            }
        }

        public void DrawInFrontOfCar(SpriteBatch spriteBatch, Vector2 carPosition, float carRotation, bool nitroActive)
        {
            if (!nitroActive) return;

            Vector2 exhaustPos = carPosition + GetBackOffset(carRotation) * 24f;
            _nitroAnim.Draw(spriteBatch, exhaustPos, carRotation + MathHelper.Pi);
        }

        private void DrawWheels(SpriteBatch spriteBatch, Vector2 carPosition, float carRotation)
        {
            Vector2 front = GetFrontOffset(carRotation);
            Vector2 back  = GetBackOffset(carRotation);
            Vector2 right = GetRightOffset(carRotation);

            // Rodas dentro do carro nos 4 cantos
            float wSide  = CarWidth  / 2f - 3f;  // distância lateral
            float wFront = CarHeight / 2f - 8f;  // distância frente/trás

            Vector2[] positions = {
                carPosition + front * wFront + right * wSide,  // frente direita
                carPosition + front * wFront - right * wSide,  // frente esquerda
                carPosition + back  * wFront + right * wSide,  // trás direita
                carPosition + back  * wFront - right * wSide,  // trás esquerda
            };

            foreach (var pos in positions)
                _wheelAnim.Draw(spriteBatch, pos, carRotation);
        }

        private Vector2 GetFrontOffset(float r) => new Vector2( MathF.Sin(r), -MathF.Cos(r));
        private Vector2 GetBackOffset(float r)  => new Vector2(-MathF.Sin(r),  MathF.Cos(r));
        private Vector2 GetRightOffset(float r) => new Vector2( MathF.Cos(r),  MathF.Sin(r));
    }
}