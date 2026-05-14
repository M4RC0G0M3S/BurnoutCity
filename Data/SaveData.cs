using System;
using System.Collections.Generic;

namespace BurnoutCity.Data
{
    // Estrutura serializada como JSON nos slots:
    // Saves/slot_1.json, Saves/slot_2.json, Saves/slot_3.json.
    public class SaveData
    {
        public DateTime LastSaveTime { get; set; } = DateTime.Now;
        public int SaveVersion { get; set; } = 1;

        // ── Progresso do jogador ───────────────────────────────────────
        public int Level { get; set; } = 1;
        public int XP { get; set; } = 0;
        public int Money { get; set; } = 1000;

        public int TotalWins { get; set; } = 0;
        public int TotalLosses { get; set; } = 0;
        public List<string> DefeatedRivals { get; set; } = new();

        // ── Upgrades instalados ────────────────────────────────────────
        public int EngineLevel { get; set; } = 0;
        public int TiresLevel { get; set; } = 0;
        public int NitroLevel { get; set; } = 0;
        public int TurboLevel { get; set; } = 0;

        // ── Personalização ─────────────────────────────────────────────
        public int CarColorIndex { get; set; } = 0;
        public string ActiveCarId { get; set; } = "default";

        // ── Posição no mundo ao gravar ─────────────────────────────────
        public float WorldPositionX { get; set; } = 1792f;
        public float WorldPositionY { get; set; } = 900f;
        public float WorldRotation { get; set; } = 0f;

        public float CarDamage { get; set; } = 0f;

        public List<float> BestLapTimes { get; set; } = new();
    }
}
