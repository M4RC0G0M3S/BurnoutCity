using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BurnoutCity.Map
{
    public class TrafficPath 
    {
        public string Id            { get; private set; } 
        public List<Vector2> Points { get; private set; }
        public bool IsLoop          { get; private set; }
        public int MaxCars          { get; private set; }

        public TrafficPath(string id, List<Vector2> points, bool isLoop = true, int maxCars = 2)
        {
            Id       = id;
            Points   = points;
            IsLoop   = isLoop;
            MaxCars  = maxCars;
        }

        public Vector2 SpawnPoint => Points.Count > 0 ? Points[0] : Vector2.Zero;
    }
}