using System.Collections.Generic;

namespace BurnoutCity.Data
{
    public enum UpgradeType { Engine, Tires, Turbo, Nitro }

    // Metadados de um upgrade comprável na loja de peças.
    // Os efeitos reais de cada upgrade estão em BuildCarStatsFromPlayerData (ExplorationState)
    // e no construtor de RaceState — aqui apenas se guardam custo e requisitos.
    public class UpgradeData
    {
        public string Name { get; set; } = "";
        public UpgradeType Type { get; set; }
        public int Tier { get; set; }              // 1–4; tier mais alto = melhor desempenho
        public int Cost { get; set; }              // preço em EUR
        public int RequiredLevel { get; set; }     // nível mínimo do jogador para desbloquear
        public string IconName { get; set; } = "";
        public string Description { get; set; } = "";

        // Verifica se o jogador tem nível suficiente para ver/comprar este upgrade.
        public bool IsUnlocked(PlayerData player)
        {
            return player.Level >= RequiredLevel;
        }

        // Devolve a lista completa de upgrades disponíveis na loja (dois tiers por categoria).
        public static List<UpgradeData> GetDefaultUpgrades()
        {
            return new List<UpgradeData>
            {
                new UpgradeData
                {
                    Name = "Motor T1",
                    Type = UpgradeType.Engine,
                    Tier = 1,
                    Cost = 500,
                    RequiredLevel = 1,
                    IconName = "Icon_Engine",
                    Description = "Aumenta a aceleração."
                },

                new UpgradeData
                {
                    Name = "Motor T2",
                    Type = UpgradeType.Engine,
                    Tier = 2,
                    Cost = 1500,
                    RequiredLevel = 5,
                    IconName = "Icon_Engine",
                    Description = "Melhora ainda mais a aceleração."
                },

                new UpgradeData
                {
                    Name = "Pneus T1",
                    Type = UpgradeType.Tires,
                    Tier = 1,
                    Cost = 400,
                    RequiredLevel = 1,
                    IconName = "Icon_Tires",
                    Description = "Melhora a aderência."
                },

                new UpgradeData
                {
                    Name = "Pneus T2",
                    Type = UpgradeType.Tires,
                    Tier = 2,
                    Cost = 1200,
                    RequiredLevel = 4,
                    IconName = "Icon_Tires",
                    Description = "Aderência melhorada."
                },

                new UpgradeData
                {
                    Name = "Turbo T1",
                    Type = UpgradeType.Turbo,
                    Tier = 1,
                    Cost = 800,
                    RequiredLevel = 3,
                    IconName = "Icon_Turbo",
                    Description = "Aumenta a velocidade."
                },

                new UpgradeData
                {
                    Name = "Turbo T2",
                    Type = UpgradeType.Turbo,
                    Tier = 2,
                    Cost = 2200,
                    RequiredLevel = 8,
                    IconName = "Icon_Turbo",
                    Description = "Velocidade melhorada."
                },

                new UpgradeData
                {
                    Name = "Nitro T1",
                    Type = UpgradeType.Nitro,
                    Tier = 1,
                    Cost = 1000,
                    RequiredLevel = 6,
                    IconName = "Icon_Nitro",
                    Description = "Desbloqueia nitro."
                },

                new UpgradeData
                {
                    Name = "Nitro T2",
                    Type = UpgradeType.Nitro,
                    Tier = 2,
                    Cost = 3000,
                    RequiredLevel = 12,
                    IconName = "Icon_Nitro",
                    Description = "Nitro melhorado."
                }
            };
        }
    }
}