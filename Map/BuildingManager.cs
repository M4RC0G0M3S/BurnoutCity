using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Map
{
    public class BuildingManager
    {
        private readonly List<Building> _buildings = new();
        private const float InteractionRadius = 200f;
        public IReadOnlyList<Building> Buildings => _buildings;

        public BuildingManager(MapManager mapManager)
        {
            PlaceAllBuildings(mapManager);
        }

        private void PlaceAllBuildings(MapManager mapManager)
        {

            Add(new Vector2(704,  320), BuildingType.Hotel,             mapManager);
            Add(new Vector2(1088, 320), BuildingType.TestTrack,         mapManager);
            Add(new Vector2(1472, 320), BuildingType.Predio,            mapManager);
            Add(new Vector2(1856, 320), BuildingType.Residencias,       mapManager);
            Add(new Vector2(2240, 320), BuildingType.Restaurante,       mapManager);
            Add(new Vector2(2624, 320), BuildingType.PredioResidencias, mapManager);

   
            Add(new Vector2(704,  832), BuildingType.PartsShop,         mapManager);
            Add(new Vector2(1088, 832), BuildingType.PredioIndustrias,  mapManager);
            Add(new Vector2(1472, 832), BuildingType.Hotel,             mapManager);
            Add(new Vector2(1856, 832), BuildingType.Predio,            mapManager);
            Add(new Vector2(2240, 832), BuildingType.CustomShop,        mapManager);
            Add(new Vector2(2624, 832), BuildingType.Restaurante,       mapManager);


      
            Add(new Vector2(704,  1216), BuildingType.PredioResidencias, mapManager);
            Add(new Vector2(1088, 1216), BuildingType.Hotel,             mapManager);
            Add(new Vector2(1472, 1216), BuildingType.Restaurante,       mapManager);
            Add(new Vector2(1856, 1216), BuildingType.Predio,            mapManager);
            Add(new Vector2(2240, 1216), BuildingType.PredioIndustrias,  mapManager);
            Add(new Vector2(2624, 1216), BuildingType.Residencias,       mapManager);

     
            Add(new Vector2(704,  1600), BuildingType.Garage,            mapManager);
            Add(new Vector2(1088, 1600), BuildingType.Predio,            mapManager);
            Add(new Vector2(1472, 1600), BuildingType.Hotel,             mapManager);
            Add(new Vector2(1856, 1600), BuildingType.PredioResidencias, mapManager);
            Add(new Vector2(2240, 1600), BuildingType.Restaurante,       mapManager);
            Add(new Vector2(2624, 1600), BuildingType.RacePoint,         mapManager);

      
            Add(new Vector2(0,    320),  BuildingType.Predio,            mapManager);
            Add(new Vector2(0,    832),  BuildingType.Residencias,       mapManager);
            Add(new Vector2(0,    1216), BuildingType.PredioIndustrias,  mapManager);
            Add(new Vector2(0,    1600), BuildingType.Restaurante,       mapManager);
            Add(new Vector2(3584, 320),  BuildingType.Restaurante,       mapManager);
            Add(new Vector2(3584, 832),  BuildingType.Hotel,             mapManager);
            Add(new Vector2(3584, 1216), BuildingType.Predio,            mapManager);
            Add(new Vector2(3584, 1600), BuildingType.PredioResidencias, mapManager);
        }

        private void Add(Vector2 pos, BuildingType type, MapManager mapManager)
        {
            _buildings.Add(new Building(pos, type, mapManager));
        }

        public Building? GetNearbyInteractive(Vector2 playerCenter)
        {
            Building? closest = null;
            float closestDist = InteractionRadius;
            foreach (var b in _buildings)
            {
                if (!b.IsInteractive) continue;
                Vector2 center = new Vector2(b.Bounds.X + b.Bounds.Width / 2f, b.Bounds.Y + b.Bounds.Height / 2f);
                float dist = Vector2.Distance(playerCenter, center);
                if (dist < closestDist) { closestDist = dist; closest = b; }
            }
            return closest;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Vector2 playerPosition)
        {
            foreach (var b in _buildings)
            {
                b.Draw(spriteBatch);
                if (b.IsInteractive)
                {
                    Vector2 center = new Vector2(b.Bounds.X + b.Bounds.Width / 2f, b.Bounds.Y + b.Bounds.Height / 2f);
                    if (Vector2.Distance(playerPosition, center) < InteractionRadius)
                        b.DrawInteractionHint(spriteBatch, pixel);
                }
            }
        }
    }
}
