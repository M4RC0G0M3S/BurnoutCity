using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BurnoutCity.Entities;
using BurnoutCity.Map;

namespace BurnoutCity.Core
{
    public class TrafficManager
    {
        private readonly List<TrafficCar>  _cars  = new();
        private readonly List<TrafficPath> _paths = new();
        private readonly Random            _rng   = new();
        private readonly Rectangle         _worldBounds;

        public bool IsActive      { get; set; } = true;
        public int  ActiveCarCount => _cars.Count(c => c.IsActive);

        public TrafficManager(Rectangle worldBounds)
        {
            _worldBounds = worldBounds;
            BuildPaths();
            SpawnInitialCars();
            Console.WriteLine($"[TrafficManager] {_cars.Count} carros spawned em {_paths.Count} paths.");
        }

        private void BuildPaths()
        {
            // Separação entre faixas:
            //   Horizontal: ±40px do centro (80px total) — evita sobreposição de bounds (carHeight até 56px)
            //   Vertical:   ±32px do centro (64px total) — evita sobreposição de bounds (carWidth até 30px)

            // ── Rua horizontal superior (streetH1 = 256) ──────────────────────
            _paths.Add(new TrafficPath("h1_LR", new List<Vector2> {
                new(100,  216), new(512,  216), new(1024, 216),
                new(1536, 216), new(1819, 216), new(2060, 216),
                new(2533, 216), new(3200, 216), new(3700, 216),
            }, isLoop: false, maxCars: 2));

            _paths.Add(new TrafficPath("h1_RL", new List<Vector2> {
                new(3700, 296), new(3200, 296), new(2533, 296),
                new(2060, 296), new(1819, 296), new(1536, 296),
                new(1024, 296), new(512,  296), new(100,  296),
            }, isLoop: false, maxCars: 2));

            // ── Avenida horizontal (avH = 768) ────────────────────────────────
            _paths.Add(new TrafficPath("avH_LR", new List<Vector2> {
                new(100,  728), new(512,  728), new(1024, 728),
                new(1536, 728), new(1819, 728), new(2060, 728),
                new(2533, 728), new(3200, 728), new(3700, 728),
            }, isLoop: false, maxCars: 4));

            _paths.Add(new TrafficPath("avH_RL", new List<Vector2> {
                new(3700, 808), new(3200, 808), new(2533, 808),
                new(2060, 808), new(1819, 808), new(1536, 808),
                new(1024, 808), new(512,  808), new(100,  808),
            }, isLoop: false, maxCars: 4));

            // ── Rua horizontal inferior (streetH2 = 1536) ────────────────────
            _paths.Add(new TrafficPath("h2_LR", new List<Vector2> {
                new(100,  1496), new(512,  1496), new(1024, 1496),
                new(1536, 1496), new(1819, 1496), new(2060, 1496),
                new(2533, 1496), new(3200, 1496), new(3700, 1496),
            }, isLoop: false, maxCars: 2));

            _paths.Add(new TrafficPath("h2_RL", new List<Vector2> {
                new(3700, 1576), new(3200, 1576), new(2533, 1576),
                new(2060, 1576), new(1819, 1576), new(1536, 1576),
                new(1024, 1576), new(512,  1576), new(100,  1576),
            }, isLoop: false, maxCars: 2));

            // ── Rua vertical esquerda (streetV1 = 1024) ───────────────────────
            _paths.Add(new TrafficPath("v1_TB", new List<Vector2> {
                new(992, 100),  new(992, 256),  new(992, 512),
                new(992, 768),  new(992, 1024), new(992, 1536),
                new(992, 1900),
            }, isLoop: false, maxCars: 2));

            _paths.Add(new TrafficPath("v1_BT", new List<Vector2> {
                new(1056, 1900), new(1056, 1536), new(1056, 1024),
                new(1056, 768),  new(1056, 512),  new(1056, 256),
                new(1056, 100),
            }, isLoop: false, maxCars: 2));

            // ── Avenida vertical (avV = 1792) ─────────────────────────────────
            _paths.Add(new TrafficPath("avV_TB", new List<Vector2> {
                new(1760, 100),  new(1760, 256),  new(1760, 512),
                new(1760, 768),  new(1760, 1024), new(1760, 1536),
                new(1760, 1900),
            }, isLoop: false, maxCars: 3));

            _paths.Add(new TrafficPath("avV_BT", new List<Vector2> {
                new(1824, 1900), new(1824, 1536), new(1824, 1024),
                new(1824, 768),  new(1824, 512),  new(1824, 256),
                new(1824, 100),
            }, isLoop: false, maxCars: 3));

            // ── Rua vertical direita (streetV2 = 2560) ───────────────────────
            _paths.Add(new TrafficPath("v2_TB", new List<Vector2> {
                new(2528, 100),  new(2528, 256),  new(2528, 512),
                new(2528, 768),  new(2528, 1024), new(2528, 1536),
                new(2528, 1900),
            }, isLoop: false, maxCars: 2));

            _paths.Add(new TrafficPath("v2_BT", new List<Vector2> {
                new(2592, 1900), new(2592, 1536), new(2592, 1024),
                new(2592, 768),  new(2592, 512),  new(2592, 256),
                new(2592, 100),
            }, isLoop: false, maxCars: 2));
        }

        private void SpawnInitialCars()
        {
            foreach (var path in _paths)
                for (int i = 0; i < path.MaxCars; i++)
                    SpawnCarOnPath(path, offsetIndex: i);
        }

        private void SpawnCarOnPath(TrafficPath path, int offsetIndex = 0)
        {
            if (path.Points.Count < 2) return;

            TrafficVariant variant = (TrafficVariant)_rng.Next(3);

            // Distribuir carros ao longo do path SEM dar wrap (preserva sentido)
            int stride   = path.Points.Count / Math.Max(1, path.MaxCars);
            int startIdx = offsetIndex * stride % (path.Points.Count - 1);

            // Sublista do startIdx até ao fim — sem wrap circular
            var waypoints = path.Points.Skip(startIdx).ToList();

            _cars.Add(new TrafficCar(waypoints, path.IsLoop, variant, _rng));
        }

        public void Update(GameTime gameTime, Rectangle playerBounds, Car playerCar)
        {
            if (!IsActive) return;

            foreach (var car in _cars)
            {
                if (!car.IsActive) continue;

                car.Update(gameTime);

                float damage = car.CheckPlayerCollision(playerBounds);
                if (damage > 0f)
                {
                    playerCar.ApplyCollisionDamage(damage);
                    Console.WriteLine($"[TrafficManager] Colisão! Dano: {damage:F1} | Total: {playerCar.Stats.CurrentDamage:F1}%");
                }
            }

            HandleTrafficInteractions();
        }

        private void HandleTrafficInteractions()
        {
            const float LookAheadDist = 130f;

            for (int i = 0; i < _cars.Count; i++)
            {
                if (!_cars[i].IsActive) continue;
                for (int j = i + 1; j < _cars.Count; j++)
                {
                    if (!_cars[j].IsActive) continue;

                    var a = _cars[i];
                    var b = _cars[j];

                    Vector2 aFwd = new Vector2(MathF.Sin(a.Rotation), -MathF.Cos(a.Rotation));
                    Vector2 bFwd = new Vector2(MathF.Sin(b.Rotation), -MathF.Cos(b.Rotation));
                    bool sameDir = Vector2.Dot(aFwd, bFwd) > 0.5f;

                    Rectangle ab = a.Bounds;
                    Rectangle bb = b.Bounds;

                    if (ab.Intersects(bb))
                    {
                        // Separar os carros pelo eixo de menor sobreposição
                        int ox = Math.Min(ab.Right, bb.Right) - Math.Max(ab.Left, bb.Left);
                        int oy = Math.Min(ab.Bottom, bb.Bottom) - Math.Max(ab.Top, bb.Top);
                        if (ox > 0 && oy > 0)
                        {
                            Vector2 diff = b.Position - a.Position;
                            Vector2 push = ox < oy
                                ? new Vector2(ox / 2f * MathF.Sign(diff.X == 0 ? 1 : diff.X), 0)
                                : new Vector2(0, oy / 2f * MathF.Sign(diff.Y == 0 ? 1 : diff.Y));
                            a.SetPosition(a.Position - push);
                            b.SetPosition(b.Position + push);
                        }
                        // Só abrandar se for mesmo sentido — evita pile-up em cruzamentos
                        if (sameDir)
                        {
                            a.SlowForTraffic(0.6f);
                            b.SlowForTraffic(0.6f);
                        }
                    }
                    else if (sameDir)
                    {
                        Vector2 aToB = b.Position - a.Position;
                        float dist = aToB.Length();
                        if (dist < LookAheadDist && dist > 0.1f)
                        {
                            Vector2 dir  = aToB / dist;
                            float   dotA = Vector2.Dot(dir,  aFwd);
                            float   dotB = Vector2.Dot(-dir, bFwd);

                            if (dotA > 0.5f) a.SlowForTraffic(0.4f); // b está à frente de a
                            if (dotB > 0.5f) b.SlowForTraffic(0.4f); // a está à frente de b

                            // Carros lado a lado (perpendiculares ao sentido): forçar separação
                            if (dist < 65f && MathF.Abs(dotA) < 0.4f)
                                b.SlowForTraffic(0.6f);
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!IsActive) return;

            foreach (var car in _cars)
                car.Draw(spriteBatch, pixel);
        }

        public void Pause()  => IsActive = false;
        public void Resume() => IsActive = true;
    }
}