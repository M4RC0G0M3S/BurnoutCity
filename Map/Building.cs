using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Map
{
    public enum BuildingType
    {
        PartsShop, CustomShop, Garage, RacePoint, TestTrack,
        Hotel, Restaurante, Predio, PredioResidencias, Residencias, PredioIndustrias
    }

    public class Building
    {
        public Vector2 Position { get; private set; }
        public BuildingType Type { get; private set; }
        public bool IsInteractive { get; private set; }
        public Rectangle Bounds { get; private set; }

        private Texture2D _sprite;

        private static readonly Dictionary<BuildingType, string> _textureKeys = new()
        {
            { BuildingType.PartsShop,        "Building_Loja"              },
            { BuildingType.CustomShop,        "Building_CustomShop"        },
            { BuildingType.Garage,            "Building_Garagem"           },
            { BuildingType.RacePoint,         "Building_RaceTrack"         },
            { BuildingType.TestTrack,         "Building_TestTrack"         },
            { BuildingType.Hotel,             "Building_Hotel"             },
            { BuildingType.Restaurante,       "Building_Restaurante"       },
            { BuildingType.Predio,            "Building_Predio"            },
            { BuildingType.PredioResidencias, "Building_PredioResidencias" },
            { BuildingType.Residencias,       "Building_Residencias"       },
            { BuildingType.PredioIndustrias,  "Building_PredioIndustrias"  },
        };

        public Building(Vector2 position, BuildingType type, MapManager mapManager, int size = 256)
        {
            Position = position;
            Type = type;
            IsInteractive = type is BuildingType.PartsShop
                                  or BuildingType.CustomShop
                                  or BuildingType.Garage
                                  or BuildingType.RacePoint
                                  or BuildingType.TestTrack;

            _sprite = mapManager.GetTexture(_textureKeys[type])!;
            Bounds = new Rectangle((int)position.X, (int)position.Y, size, size);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_sprite == null) return;
            spriteBatch.Draw(_sprite, Bounds, Color.White);
        }

        public void DrawInteractionHint(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!IsInteractive) return;
            int t = 4;
            Color c = Color.Orange;
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Left,      Bounds.Top,        Bounds.Width,  t),            c);
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Left,      Bounds.Bottom - t, Bounds.Width,  t),            c);
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Left,      Bounds.Top,        t,             Bounds.Height), c);
            spriteBatch.Draw(pixel, new Rectangle(Bounds.Right - t, Bounds.Top,        t,             Bounds.Height), c);
        }
    }
}
