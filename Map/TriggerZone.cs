using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Map
{
    // Enum ANTES da classe para estar disponível no mesmo ficheiro
    public enum TriggerZoneType
    {
        Garage,
        PartsShop,
        CustomShop,
        RacePoint,
        TestTrack
    }

    public class TriggerZone
    {
        public Rectangle Bounds     { get; private set; }
        public TriggerZoneType Type { get; private set; }
        public bool IsActive        { get; private set; } = true;

        private static readonly Dictionary<TriggerZoneType, Color> _debugColors = new()
        {
            { TriggerZoneType.Garage,     new Color(0,   200, 255) },
            { TriggerZoneType.PartsShop,  new Color(255, 140, 0)   },
            { TriggerZoneType.CustomShop, new Color(180, 0,   255) },
            { TriggerZoneType.RacePoint,  new Color(255, 30,  30)  },
            { TriggerZoneType.TestTrack,  new Color(30,  255, 80)  },
        };

        public TriggerZone(Rectangle bounds, TriggerZoneType type)
        {
            Bounds = bounds;
            Type   = type;
        }

        public bool IsTriggeredBy(Vector2 playerCenter)
        {
            if (!IsActive) return false;
            return Bounds.Contains((int)playerCenter.X, (int)playerCenter.Y);
        }

        public void SetActive(bool active) => IsActive = active;

        public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!IsActive) return;

            Color c      = _debugColors.TryGetValue(Type, out var col) ? col : Color.White;
            Color fill   = c * 0.15f;
            Color border = c * 0.90f;
            int   t      = 3;

            spriteBatch.Draw(pixel, Bounds, fill);
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Left,      Bounds.Top,        Bounds.Width,  t),            border);
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Left,      Bounds.Bottom - t, Bounds.Width,  t),            border);
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Left,      Bounds.Top,        t, Bounds.Height),            border);
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Right - t, Bounds.Top,        t, Bounds.Height),            border);
        }
    }
}