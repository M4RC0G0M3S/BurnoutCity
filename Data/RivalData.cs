using Microsoft.Xna.Framework;
using BurnoutCity.Entities;
using System.Collections.Generic;

namespace BurnoutCity.Data
{
    // Lista estática e ordenada de todos os rivais do jogo.
    // A progressão é sequencial: o jogador enfrenta o primeiro rival não derrotado.
    // O campeão final (The King) é o último da lista.
    public static class RivalData
    {
        public static List<Rival> All { get; } = new()
        {

            new Rival {
                Id           = "rival_01",
                Name         = "Rusty",
                CarName      = "Civic Enferrujado",
                CarColor     = new Color(160, 80, 50),
                SpriteName = "rival_rusty",
                MaxSpeed     = 500f,
                Acceleration = 500f,
                MinLevel     = 1,
                BonusReward  = 500,
                PreRaceQuote = "Vai buscar as rodas de treino, novato."
            },

            new Rival {
                Id           = "rival_02",
                Name         = "Spike",
                CarName      = "Golf Turbinado",
                CarColor     = new Color(160, 80, 50),
                SpriteName = "rival_spike",
                MaxSpeed     = 700f,
                Acceleration = 700f,
                MinLevel     = 1,
                BonusReward  = 1000,
                PreRaceQuote = "Já vi caracóis mais rápidos que tu."
            },

            new Rival {
                Id           = "rival_03",
                Name         = "Nova",
                CarName      = "Supra Clone",
                CarColor     = new Color(180, 40, 180),
                SpriteName = "rival_nova",
                MaxSpeed     = 900f,
                Acceleration = 900f,
                MinLevel     = 1,
                BonusReward  = 2000,
                PreRaceQuote = "Vou-te deixar ver as minhas luzes traseiras."
            },

            new Rival {
                Id           = "rival_04",
                Name         = "Phantom",
                CarName      = "R34 das Sombras",
                CarColor     = new Color(30, 30, 30),
                SpriteName = "rival_phantom",
                MaxSpeed     = 1100f,
                Acceleration = 1100f,
                MinLevel     = 1,
                BonusReward  = 3500,
                PreRaceQuote = "Nem vais ver quando te ultrapassar."
            },

            new Rival {
                Id           = "rival_05",
                Name         = "Nitro King",
                CarName      = "EVO Azul-Fogo",
                CarColor     = new Color(0, 180, 255),
                SpriteName = "rival_nitroking",
                MaxSpeed     = 1500f,
                Acceleration = 1500f,
                MinLevel     = 1,
                BonusReward  = 5000,
                PreRaceQuote = "O meu nitro sozinho é mais rápido que o teu carro."
            },

            new Rival {
                Id           = "rival_06",
                Name         = "Blaze",
                CarName      = "NSX Fantasma",
                CarColor     = new Color(220, 100, 20),
                SpriteName = "rival_blaze",
                MaxSpeed     = 1800f,
                Acceleration = 1800f,
                MinLevel     = 1,
                BonusReward  = 7500,
                PreRaceQuote = "Diz-me quando queres desistir. Poupa-nos tempo."
            },


            new Rival {
                Id           = "rival_champion",
                Name         = "The King",
                CarName      = "Darkstar GT",
                CarColor     = new Color(255, 140, 0),
                SpriteName = "rival_king",
                MaxSpeed     = 2000f,
                Acceleration = 2000f,
                MinLevel     = 1,
                BonusReward  = 15000,
                PreRaceQuote = "Toda a gente tenta. Ninguém passa."
            },
        };
    }
}