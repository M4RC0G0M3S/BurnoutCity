using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.Map
{
    // Gere todas as zonas de trigger do mapa e deteta quando o jogador prime E dentro delas.
    // A ação concreta (mudar de estado) é delegada via o evento OnZoneEntered,
    // mantendo este manager desacoplado da lógica de navegação entre estados.
    public class TriggerZoneManager
    {
        private readonly List<TriggerZone> _zones = new();
        private TriggerZone? _currentZone = null;
        private KeyboardState _prevKeyboard;

        public Action<TriggerZoneType>? OnZoneEntered;
        public bool DebugVisible { get; set; } = false;

        public TriggerZoneManager()
        {
            PlaceAllZones();
        }

        // Define as posições fixas de todas as zonas de trigger, alinhadas com os edifícios interativos.
        private void PlaceAllZones()
        {
            AddZone(704,  1600, TriggerZoneType.Garage,     offsetX: 28, offsetY: -80);
            AddZone(704,  832,  TriggerZoneType.PartsShop,  offsetX: 28, offsetY: -80);
            AddZone(2240, 832,  TriggerZoneType.CustomShop, offsetX: 28, offsetY: -80);
            AddZone(2624, 1600, TriggerZoneType.RacePoint,  offsetX: 28, offsetY: -80);
            AddZone(1088, 320,  TriggerZoneType.TestTrack,  offsetX: 28, offsetY: -80);

            Console.WriteLine($"[TriggerZoneManager] {_zones.Count} zonas criadas.");
        }

        // Cria uma TriggerZone com posição calculada a partir do canto do edifício + offset.
        // O offset coloca a zona à entrada do edifício (frente da porta).
        private void AddZone(int buildingX, int buildingY, TriggerZoneType type,
                             int offsetX = 28, int offsetY = -80,
                             int zoneWidth = 200, int zoneHeight = 80)
        {
            int x = buildingX + offsetX;
            int y = buildingY + offsetY;
            _zones.Add(new TriggerZone(new Rectangle(x, y, zoneWidth, zoneHeight), type));
        }

        // Deteta em que zona o jogador está e dispara OnZoneEntered ao pressionar E (edge-triggered).
        public void Update(Vector2 playerCenter)
        {
            KeyboardState kb = Keyboard.GetState();

            // Descobre em que zona o jogador está
            TriggerZone? zone = null;
            foreach (var z in _zones)
            {
                if (z.IsTriggeredBy(playerCenter))
                {
                    zone = z;
                    break;
                }
            }

            _currentZone = zone;

            // Só entra se carregar E enquanto está dentro da zona
            if (_currentZone != null
                && kb.IsKeyDown(Keys.E)
                && _prevKeyboard.IsKeyUp(Keys.E))
            {
                Console.WriteLine($"[TriggerZoneManager] E pressionado na zona: {_currentZone.Type}");
                OnZoneEntered?.Invoke(_currentZone.Type);
            }

            _prevKeyboard = kb;
        }

        public TriggerZone? CurrentZone => _currentZone;

        // Desenha todas as zonas com cores por tipo (ativo apenas se DebugVisible = true).
        public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!DebugVisible) return;
            foreach (var zone in _zones)
                zone.DrawDebug(spriteBatch, pixel);
        }

        public IReadOnlyList<TriggerZone> Zones => _zones;
    }
}