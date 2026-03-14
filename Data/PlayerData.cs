using System;
using System.Collections.Generic;


namespace BurnoutCity.Data
{
    // =========================================================
    //  PlayerData — Toda a informação persistente do jogador
    //  XP, Nível, Dinheiro, Upgrades instalados, etc.
    // =========================================================
    public class PlayerData
    {
        // ── Identificação ──────────────────────────────────────
        public int Level { get;  set; } = 1;
        public int XP { get; set; } = 0;
        public int Money { get; set; } = 1000;   // dinheiro inicial

        // ── Corridas ───────────────────────────────────────────
        public int TotalWins { get; set; } = 0;
        public int TotalLosses { get; set; } = 0;

        // Rivais já derrotados (IDs de RivalData)
        public List<string> DefeatedRivals { get; set; } = new(); 

        // ── Upgrades instalados ────────────────────────────────
        public int EngineLevel { get; set; } = 0;   // 0 = stock, 1-4 = tiers
        public int TiresLevel { get; set; } = 0;
        public int TurboLevel { get; set; } = 0;
        public int NitroLevel { get; set; } = 0;

        // ── Personalização ─────────────────────────────────────
        public int CarColorIndex { get;  set; } = 0;
        public int RimStyleIndex { get; set; } = 0;
        public int BodykitIndex { get; set; } = 0;
        public string ActiveCarId { get; set; } = "default";

        // ── Posição no mundo (persistência) ───────────────────
        public float WorldPositionX { get; set; } = 1792f;
        public float WorldPositionY { get; set; } = 900f;

        // ── Dano ───────────────────────────────────────────────
        public float CarDamage { get; set; } = 0f;

        // ── Test Track ────────────────────────────────────────
        public List<float> BestLapTimes { get; set; } = new(); // top 5, segundos
        private const int MaxBestTimes = 5;

        // ==========================================================
        //  CONSTANTES DE RECOMPENSA (conforme game brief)
        // ==========================================================
        public const int XP_WIN = 200;
        public const int XP_LOSS = 20;
        public const int MONEY_WIN = 2000;
        public const int MONEY_LOSS = 200;

        // ==========================================================
        //  CURVA DE XP  (Level 1→20)
        //  Fórmula: XP_para_proximo = 300 * nivel^1.35  (arredondado a 50)
        //  Resulta numa curva gradual e desafiante mas justa.
        // ==========================================================
        private static readonly int[] _xpTable = BuildXpTable();

        private static int[] BuildXpTable()
        {
            // Índice 0 não é usado; índice i = XP total necessário para ESTAR no nível i+1
            // _xpTable[i] = XP para passar do nível (i+1) para (i+2), i de 0 a 18
            var table = new int[19]; // 19 transições: nível 1→2 até 19→20
            for (int i = 0; i < 19; i++)
            {
                int lvl = i + 1;
                double raw = 300.0 * Math.Pow(lvl, 1.35);
                table[i] = (int)(Math.Round(raw / 50.0) * 50); // arredonda a 50
            }
            return table;
        }

        /// <summary>
        /// XP necessário para subir do nível atual para o seguinte.
        /// Retorna int.MaxValue se já estiver no nível máximo.
        /// </summary>
        public int XPForNextLevel()
        {
            if (Level >= 20) return int.MaxValue;
            return _xpTable[Level - 1]; // índice 0 = transição nível 1→2
        }

        /// <summary>
        /// XP acumulado dentro do nível atual (0 a XPForNextLevel-1).
        /// </summary>
        public int XPInCurrentLevel()
        {
            int total = 0;
            for (int i = 0; i < Level - 1; i++)
                total += _xpTable[i];
            return XP - total;
        }

        /// <summary>Progresso de 0.0 a 1.0 dentro do nível atual.</summary>
        public float LevelProgress()
        {
            if (Level >= 20) return 1f;
            int needed = XPForNextLevel();
            int current = XPInCurrentLevel();
            return needed <= 0 ? 1f : Math.Clamp((float)current / needed, 0f, 1f);
        }

        // ==========================================================
        //  MÉTODOS DE CORRIDA
        // ==========================================================

        /// <summary>
        /// Regista o resultado de uma corrida.
        /// Retorna LevelUpInfo caso tenha havido subida de nível (pode ser múltipla).
        /// </summary>
        public LevelUpInfo RegisterRaceResult(bool won, string? rivalId = null)
        {
            int xpGained = won ? XP_WIN : XP_LOSS;
            int moneyGained = won ? MONEY_WIN : MONEY_LOSS;

            // Dinheiro nunca é removido por perder (conforme brief)
            Money += moneyGained;

            if (won)
            {
                TotalWins++;
                if (rivalId != null && !DefeatedRivals.Contains(rivalId))
                    DefeatedRivals.Add(rivalId);
            }
            else
            {
                TotalLosses++;
            }

            return AddXP(xpGained);
        }

        // ==========================================================
        //  XP & NIVELAMENTO
        // ==========================================================

        /// <summary>Adiciona XP e sobe de nível conforme necessário.</summary>
        public LevelUpInfo AddXP(int amount)
        {
            if (amount <= 0) return new LevelUpInfo(false, Level);
            if (Level >= 20) return new LevelUpInfo(false, Level); // máximo atingido

            int oldLevel = Level;
            XP += amount;

            // Sobe tantos níveis quantos forem necessários
            while (Level < 20)
            {
                int xpNeeded = XPForNextLevel();
                int xpInLvl = XPInCurrentLevel();
                if (xpInLvl >= xpNeeded)
                    Level++;
                else
                    break;
            }

            bool levelled = Level > oldLevel;
            return new LevelUpInfo(levelled, Level, oldLevel);
        }

        // ==========================================================
        //  DINHEIRO
        // ==========================================================

        /// <summary>Gasta dinheiro. Retorna false se saldo insuficiente.</summary>
        public bool SpendMoney(int amount)
        {
            if (amount <= 0) return true;
            if (Money < amount) return false;
            Money -= amount;
            return true;
        }

        public void AddMoney(int amount)
        {
            if (amount > 0) Money += amount;
        }

        // ==========================================================
        //  UPGRADES
        // ==========================================================

        public bool UpgradeEngine(int cost) { if (!SpendMoney(cost)) return false; EngineLevel = Math.Min(EngineLevel + 1, 4); return true; }
        public bool UpgradeTires(int cost) { if (!SpendMoney(cost)) return false; TiresLevel = Math.Min(TiresLevel + 1, 4); return true; }
        public bool UpgradeTurbo(int cost) { if (!SpendMoney(cost)) return false; TurboLevel = Math.Min(TurboLevel + 1, 4); return true; }
        public bool UpgradeNitro(int cost) { if (!SpendMoney(cost)) return false; NitroLevel = Math.Min(NitroLevel + 1, 4); return true; }

        // ==========================================================
        //  PERSONALIZAÇÃO
        // ==========================================================

        public void SetCarColor(int index) => CarColorIndex = Math.Clamp(index, 0, 7);
        public void SetRimStyle(int index) => RimStyleIndex = Math.Clamp(index, 0, 3);
        public void SetBodykit(int index) => BodykitIndex = Math.Clamp(index, 0, 2);
        public void SetActiveCar(string id) => ActiveCarId = id;

        // ==========================================================
        //  ESTADO DO CARRO
        // ==========================================================

        public void SetDamage(float damage) => CarDamage = Math.Clamp(damage, 0f, 100f);
        public void RepairCar() => CarDamage = 0f;

        public void SavePosition(float x, float y)
        {
            WorldPositionX = x;
            WorldPositionY = y;
        }

        // ==========================================================
        //  TEST TRACK
        // ==========================================================

        /// <summary>Regista um tempo de volta. Mantém apenas os 5 melhores.</summary>
        public bool RegisterLapTime(float seconds)
        {
            BestLapTimes.Add(seconds);
            BestLapTimes.Sort();
            if (BestLapTimes.Count > MaxBestTimes)
                BestLapTimes.RemoveRange(MaxBestTimes, BestLapTimes.Count - MaxBestTimes);
            return BestLapTimes.Count > 0 && BestLapTimes[0] == seconds; // true se é novo recorde
        }

        // ==========================================================
        //  DEBUG / TESTES
        // ==========================================================
        public void LoadFrom(SaveData save)
        {
            Level = Math.Clamp(save.Level, 1, 20);
            XP = Math.Max(0, save.XP);
            Money = Math.Max(0, save.Money);
            TotalWins = Math.Max(0, save.TotalWins);
            TotalLosses = Math.Max(0, save.TotalLosses);
            DefeatedRivals = new List<string>(save.DefeatedRivals ?? new List<string>());
            EngineLevel = Math.Clamp(save.EngineLevel, 0, 4);
            TiresLevel = Math.Clamp(save.TiresLevel, 0, 4);
            TurboLevel = Math.Clamp(save.TurboLevel, 0, 4);
            NitroLevel = Math.Clamp(save.NitroLevel, 0, 4);
            CarColorIndex = Math.Clamp(save.CarColorIndex, 0, 7);
            ActiveCarId = save.ActiveCarId ?? "default";
            WorldPositionX = save.WorldPositionX;
            WorldPositionY = save.WorldPositionY;
            CarDamage = Math.Clamp(save.CarDamage, 0f, 100f);
            BestLapTimes = new List<float>(save.BestLapTimes ?? new List<float>());
            BestLapTimes.Sort();
            if (BestLapTimes.Count > 5)
                BestLapTimes.RemoveRange(5, BestLapTimes.Count - 5); 
            Console.WriteLine("[PlayerData] Dados carregados do SaveData.");
        }
        public void PrintStatus()
        {
            Console.WriteLine($"[PlayerData] Nível: {Level} | XP: {XP} (próximo nível: {XPForNextLevel()}) | Dinheiro: {Money}€");
            Console.WriteLine($"[PlayerData] Vitórias: {TotalWins} | Derrotas: {TotalLosses}");
            Console.WriteLine($"[PlayerData] Engine:{EngineLevel} Tires:{TiresLevel} Turbo:{TurboLevel} Nitro:{NitroLevel}");
            Console.WriteLine($"[PlayerData] Dano: {CarDamage:F1}%");
        }

        // ==========================================================
        //  TABELA DE XP PÚBLICA (para HUD / UI)
        // ==========================================================

        /// <summary>Devolve toda a tabela de XP (nível 1→2 até 19→20) para debug ou UI.</summary>
        public static IReadOnlyList<int> GetXpTable() => _xpTable;
    }

    // =========================================================
    //  LevelUpInfo — Informação retornada ao subir de nível
    // =========================================================
    public readonly struct LevelUpInfo
    {
        public bool LeveledUp { get; }
        public int NewLevel { get; }
        public int PreviousLevel { get; }

        public LevelUpInfo(bool leveledUp, int newLevel, int previousLevel = 0)
        {
            LeveledUp = leveledUp;
            NewLevel = newLevel;
            PreviousLevel = previousLevel;
        }
    }
}