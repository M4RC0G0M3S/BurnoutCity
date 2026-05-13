using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BurnoutCity.Map
{
    // Define uma sequência de waypoints que os carros de tráfego percorrem.
    // Caminhos com IsLoop=false fazem os carros voltarem ao início quando chegam ao fim;
    // IsLoop=true liga o último ponto ao primeiro (percurso contínuo em circuito).
    // Cada caminho define também quantos carros (MaxCars) podem circular simultaneamente.
    public class TrafficPath
    {
        public string Id            { get; private set; }   // identificador (ex: "h1_LR" = rua H1 da esquerda para a direita)
        public List<Vector2> Points { get; private set; }   // waypoints em coordenadas do mundo
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