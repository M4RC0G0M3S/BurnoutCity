using System;
using System.Collections.Generic;

namespace BurnoutCity.Data
{
    // Estrutura serializada como JSON em Saves/burnoutcity_save.json.
    // Contém apenas campos simples — sem lógica; PlayerData faz a leitura e validação.
    // O SaveManager escreve um .tmp antes de substituir o ficheiro principal,
    // e mantém um .bak para recuperação em caso de corrupção.
    public class SaveData
    {
        public DateTime LastSaveTime { get; set; } = DateTime.Now;
        public int SaveVersion { get; set; } = 1;       // reservado para migrações futuras

        // ── Progresso do jogador ───────────────────────────────────────
        public int Level { get; set; } = 1;
        public int XP { get; set; } = 0;
        public int Money { get; set; } = 1000;

        public int TotalWins { get; set; } = 0;
        public int TotalLosses { get; set; } = 0;
        public List<string> DefeatedRivals { get; set; } = new();  // IDs dos rivais já derrotados

        // ── Upgrades instalados (0 = stock, 1–4 = tiers) ──────────────
        public int EngineLevel { get; set; } = 0;
        public int TiresLevel { get; set; } = 0;
        public int NitroLevel { get; set; } = 0;
        public int TurboLevel { get; set; } = 0;

        // ── Personalização ─────────────────────────────────────────────
        public int CarColorIndex { get; set; } = 0;
        public string ActiveCarId { get; set; } = "default";

        // ── Posição no mundo ao gravar (spawn point no próximo load) ───
        public float WorldPositionX { get; set; } = 1792f;
        public float WorldPositionY { get; set; } = 900f;

        public float CarDamage { get; set; } = 0f;      // 0 = sem dano, 100 = destruído

        public List<float> BestLapTimes { get; set; } = new();  // top 5 voltas no TestTrack (segundos)
    }
}