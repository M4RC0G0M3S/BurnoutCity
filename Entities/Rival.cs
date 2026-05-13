using System;
using BurnoutCity.Core;
using BurnoutCity.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.Entities
{
    // Dados de um rival: identidade, sprite, parâmetros de velocidade e recompensa.
    // As instâncias concretas estão definidas em RivalData.All (static list).
    // MaxSpeed e Acceleration são usados diretamente no RaceState para calcular
    // a velocidade da IA do rival, escalada em relação ao top speed do jogador.
    public class Rival
    {
        public string Id { get; set; } = "";              // chave única usada em PlayerData.DefeatedRivals
        public string Name { get; set; } = "Rival";
        public string CarName { get; set; } = "Unknown";
        public Color CarColor { get; set; } = Color.DeepSkyBlue;
        public float MaxSpeed { get; set; } = 350f;       // px/s de referência para a IA
        public float Acceleration { get; set; } = 140f;   // taxa de aceleração da IA
        public int MinLevel { get; set; } = 1;            // nível mínimo para desafiar este rival
        public int BonusReward { get; set; } = 0;         // dinheiro extra ao derrotá-lo (além do MONEY_WIN padrão)
        public string PreRaceQuote { get; set; } = "..."; // frase exibida no card antes da corrida
        public string SpriteName { get; set; } = "rival_default"; // nome do asset em Sprites/CarSprites/
        public bool IsDefeated { get; set; } = false;
    }
}