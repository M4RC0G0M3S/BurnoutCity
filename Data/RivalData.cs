using Microsoft.Xna.Framework;
using BurnoutCity.Entities;
using System.Collections.Generic;

namespace BurnoutCity.Data
{
    public static class RivalData
    {
        public static List<Rival> All { get; } = new()
        {

            new Rival {
                Id           = "rival_01",
                Name         = "Rusty",
                CarName      = "Civic Enferrujado",
                CarColor     = new Color(160, 80, 50),
                MaxSpeed     = 340f,
                Acceleration = 200f,
                MinLevel     = 1,
                BonusReward  = 500,
                PreRaceQuote = "Vai buscar as rodas de treino, novato."
            },

            new Rival {
                Id           = "rival_02",
                Name         = "Spike",
                CarName      = "Golf Turbinado",
                CarColor     = new Color(60, 130, 200),
                MaxSpeed     = 400f,
                Acceleration = 260f,
                MinLevel     = 4,
                BonusReward  = 1000,
                PreRaceQuote = "Já vi caracóis mais rápidos que tu."
            },

            new Rival {
                Id           = "rival_03",
                Name         = "Nova",
                CarName      = "Supra Clone",
                CarColor     = new Color(180, 40, 180),
                MaxSpeed     = 500f,
                Acceleration = 300f,
                MinLevel     = 7,
                BonusReward  = 2000,
                PreRaceQuote = "Vou-te deixar ver as minhas luzes traseiras."
            },

            new Rival {
                Id           = "rival_04",
                Name         = "Phantom",
                CarName      = "R34 das Sombras",
                CarColor     = new Color(30, 30, 30),
                MaxSpeed     = 580f,
                Acceleration = 350f,
                MinLevel     = 11,
                BonusReward  = 3500,
                PreRaceQuote = "Nem vais ver quando te ultrapassar."
            },

            new Rival {
                Id           = "rival_05",
                Name         = "Nitro King",
                CarName      = "EVO Azul-Fogo",
                CarColor     = new Color(0, 180, 255),
                MaxSpeed     = 600f,
                Acceleration = 400f,
                MinLevel     = 15,
                BonusReward  = 5000,
                PreRaceQuote = "O meu nitro sozinho é mais rápido que o teu carro."
            },

            new Rival {
                Id           = "rival_06",
                Name         = "Blaze",
                CarName      = "NSX Fantasma",
                CarColor     = new Color(220, 100, 20),
                MaxSpeed     = 765f,
                Acceleration = 450f,
                MinLevel     = 18,
                BonusReward  = 7500,
                PreRaceQuote = "Diz-me quando queres desistir. Poupa-nos tempo."
            },


            new Rival {
                Id           = "rival_champion",
                Name         = "The King",
                CarName      = "Darkstar GT",
                CarColor     = new Color(255, 140, 0),
                MaxSpeed     = 800f,
                Acceleration = 530f,
                MinLevel     = 20,
                BonusReward  = 15000,
                PreRaceQuote = "Toda a gente tenta. Ninguém passa."
            },
        };
    }
}