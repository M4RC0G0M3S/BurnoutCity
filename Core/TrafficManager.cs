using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BurnoutCity.Entities;
using BurnoutCity.Map;

namespace BurnoutCity.Core
{
    public class TrafficManager
    {
        private readonly List<TrafficCar>  _cars  = new();
        public IReadOnlyList<TrafficCar> Cars => _cars;
        private readonly List<TrafficPath> _paths = new();
        private readonly Random            _rng   = new();
        private readonly Rectangle         _worldBounds;

        public bool IsActive      { get; set; } = true;
        public int  ActiveCarCount => _cars.Count(c => c.IsActive);

        // ── Sprites dos 3 tipos de carro de tráfego ───────────────────────────
        // Índice corresponde ao valor do enum TrafficVariant:
        //   0 = Sedan  → traffic_car_a.png
        //   1 = SUV    → traffic_car_b.png
        //   2 = Hatch  → traffic_car_c.png
        private readonly Texture2D?[] _trafficSprites = new Texture2D?[3];

        // ─────────────────────────────────────────────────────────────────────
        //  CONSTRUTOR
        // ─────────────────────────────────────────────────────────────────────
        public TrafficManager(Rectangle worldBounds)
        {
            _worldBounds = worldBounds;
            BuildPaths();
            SpawnInitialCars();
            Console.WriteLine($"[TrafficManager] {_cars.Count} carros spawned em {_paths.Count} paths.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LOAD CONTENT — carregar os sprites dos carros de tráfego
        //  Chama este método no LoadContent da ExplorationState, depois de
        //  criar o TrafficManager:
        //      _trafficManager.LoadContent(ContentManager);
        // ─────────────────────────────────────────────────────────────────────
        public void LoadContent(ContentManager content)
        {
            // Nomes dos ficheiros em Content/Sprites/Traffic/
            string[] spriteNames =
            {
                "Sprites/Traffic/traffic_car_a",  // Sedan
                "Sprites/Traffic/traffic_car_b",  // SUV
                "Sprites/Traffic/traffic_car_c",  // Hatch
            };

            for (int i = 0; i < spriteNames.Length; i++)
            {
                try
                {
                    _trafficSprites[i] = content.Load<Texture2D>(spriteNames[i]);
                    Console.WriteLine($"[TrafficManager] Sprite carregado: {spriteNames[i]}");
                }
                catch
                {
                    // Se o ficheiro não existir ainda, usa o fallback (retângulo colorido)
                    _trafficSprites[i] = null;
                    Console.WriteLine($"[TrafficManager] AVISO: sprite não encontrado: {spriteNames[i]} — fallback ativo");
                }
            }

            // Atribui os sprites a todos os carros já criados no construtor
            foreach (var car in _cars)
                AssignSprite(car);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BUILD PATHS — define todos os caminhos do mapa
        // ─────────────────────────────────────────────────────────────────────
        private void BuildPaths()
        {
            // Separação entre faixas:
            //   Horizontal: ±40px do centro (80px total)
            //   Vertical:   ±32px do centro (64px total)

            // ── Rua horizontal superior (streetH1 = 256) ──────────────────────
            _paths.Add(new TrafficPath("h1_LR", new List<Vector2> {
                new(100,  216), new(512,  216), new(1024, 216),
                new(1536, 216), new(1819, 216), new(2060, 216),
                new(2533, 216), new(3200, 216), new(3700, 216),
            }, isLoop: false, maxCars: 1));

            _paths.Add(new TrafficPath("h1_RL", new List<Vector2> {
                new(3700, 296), new(3200, 296), new(2533, 296),
                new(2060, 296), new(1819, 296), new(1536, 296),
                new(1024, 296), new(512,  296), new(100,  296),
            }, isLoop: false, maxCars: 1));

            // ── Avenida horizontal (avH = 768) ────────────────────────────────
            _paths.Add(new TrafficPath("avH_LR", new List<Vector2> {
                new(100,  728), new(512,  728), new(1024, 728),
                new(1536, 728), new(1819, 728), new(2060, 728),
                new(2533, 728), new(3200, 728), new(3700, 728),
            }, isLoop: false, maxCars: 2));

            _paths.Add(new TrafficPath("avH_RL", new List<Vector2> {
                new(3700, 808), new(3200, 808), new(2533, 808),
                new(2060, 808), new(1819, 808), new(1536, 808),
                new(1024, 808), new(512,  808), new(100,  808),
            }, isLoop: false, maxCars: 2));

            // ── Rua horizontal inferior (streetH2 = 1536) ────────────────────
            _paths.Add(new TrafficPath("h2_LR", new List<Vector2> {
                new(100,  1496), new(512,  1496), new(1024, 1496),
                new(1536, 1496), new(1819, 1496), new(2060, 1496),
                new(2533, 1496), new(3200, 1496), new(3700, 1496),
            }, isLoop: false, maxCars: 1));

            _paths.Add(new TrafficPath("h2_RL", new List<Vector2> {
                new(3700, 1576), new(3200, 1576), new(2533, 1576),
                new(2060, 1576), new(1819, 1576), new(1536, 1576),
                new(1024, 1576), new(512,  1576), new(100,  1576),
            }, isLoop: false, maxCars: 1));

            // ── Rua vertical esquerda (streetV1 = 1024) ───────────────────────
            _paths.Add(new TrafficPath("v1_TB", new List<Vector2> {
                new(992, 100),  new(992, 256),  new(992, 512),
                new(992, 768),  new(992, 1024), new(992, 1536),
                new(992, 1900),
            }, isLoop: false, maxCars: 1));

            _paths.Add(new TrafficPath("v1_BT", new List<Vector2> {
                new(1056, 1900), new(1056, 1536), new(1056, 1024),
                new(1056, 768),  new(1056, 512),  new(1056, 256),
                new(1056, 100),
            }, isLoop: false, maxCars: 1));

            // ── Avenida vertical (avV = 1792) ─────────────────────────────────
            _paths.Add(new TrafficPath("avV_TB", new List<Vector2> {
                new(1760, 100),  new(1760, 256),  new(1760, 512),
                new(1760, 768),  new(1760, 1024), new(1760, 1536),
                new(1760, 1900),
            }, isLoop: false, maxCars: 1));

            _paths.Add(new TrafficPath("avV_BT", new List<Vector2> {
                new(1824, 1900), new(1824, 1536), new(1824, 1024),
                new(1824, 768),  new(1824, 512),  new(1824, 256),
                new(1824, 100),
            }, isLoop: false, maxCars: 1));

            // ── Rua vertical direita (streetV2 = 2560) ───────────────────────
            _paths.Add(new TrafficPath("v2_TB", new List<Vector2> {
                new(2528, 100),  new(2528, 256),  new(2528, 512),
                new(2528, 768),  new(2528, 1024), new(2528, 1536),
                new(2528, 1900),
            }, isLoop: false, maxCars: 1));

            _paths.Add(new TrafficPath("v2_BT", new List<Vector2> {
                new(2592, 1900), new(2592, 1536), new(2592, 1024),
                new(2592, 768),  new(2592, 512),  new(2592, 256),
                new(2592, 100),
            }, isLoop: false, maxCars: 1));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SPAWN
        // ─────────────────────────────────────────────────────────────────────
        private void SpawnInitialCars()
        {
            foreach (var path in _paths)
                for (int i = 0; i < path.MaxCars; i++)
                    SpawnCarOnPath(path, offsetIndex: i);
        }

        private void SpawnCarOnPath(TrafficPath path, int offsetIndex = 0)
        {
            if (path.Points.Count < 2) return;

            // Escolhe aleatoriamente entre as 3 variantes: Sedan=0, SUV=1, Hatch=2
            TrafficVariant variant = (TrafficVariant)_rng.Next(3);

            // Distribuir carros ao longo do path sem dar wrap (preserva sentido)
            int stride   = path.Points.Count / Math.Max(1, path.MaxCars);
            int startIdx = offsetIndex * stride % (path.Points.Count - 1);

            // Sublista do startIdx até ao fim
            var waypoints = path.Points.Skip(startIdx).ToList();

            var newCar = new TrafficCar(waypoints, path.IsLoop, variant, _rng);

            // Atribui o sprite correspondente à variante (se já carregado)
            AssignSprite(newCar);

            _cars.Add(newCar);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ATRIBUIR SPRITE A UM CARRO
        // ─────────────────────────────────────────────────────────────────────

        private void AssignSprite(TrafficCar car)
        {
            int idx = (int)car.Variant;
            car.SetSprite(idx < _trafficSprites.Length ? _trafficSprites[idx] : null);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UPDATE
        // ─────────────────────────────────────────────────────────────────────
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

            
        }


        // ─────────────────────────────────────────────────────────────────────
        //  DRAW
        // ─────────────────────────────────────────────────────────────────────
        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!IsActive) return;

            foreach (var car in _cars)
                car.Draw(spriteBatch, pixel);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PAUSE / RESUME (usado nas corridas)
        // ─────────────────────────────────────────────────────────────────────
        public void Pause()  => IsActive = false;
        public void Resume() => IsActive = true;
    }
}