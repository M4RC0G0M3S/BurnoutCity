using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using BurnoutCity.Data;

namespace BurnoutCity.Core
{
    public class SaveManager
    {
        public static SaveManager Instance { get; private set; } = new SaveManager();

        private static readonly string SaveDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");

        public const int SlotCount = 3;

        public int ActiveSlot { get; private set; } = 1;

        public SaveData CurrentSave { get; private set; } = new SaveData();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private string GetSlotPath(int slot)
        {
            return Path.Combine(SaveDirectory, $"slot_{slot}.json");
        }

        private string GetBackupPath(int slot)
        {
            return Path.Combine(SaveDirectory, $"slot_{slot}.bak");
        }

        public bool HasSave()
        {
            return HasAnySave();
        }

        public bool HasAnySave()
        {
            for (int i = 1; i <= SlotCount; i++)
            {
                if (HasSave(i))
                    return true;
            }

            return false;
        }

        public bool HasSave(int slot)
        {
            slot = Math.Clamp(slot, 1, SlotCount);
            return File.Exists(GetSlotPath(slot));
        }

        public string GetSlotLabel(int slot)
        {
            slot = Math.Clamp(slot, 1, SlotCount);

            string path = GetSlotPath(slot);

            if (!File.Exists(path))
                return $"SLOT {slot} - VAZIO";

            if (TryDeserialize(path, out SaveData? data) && data != null)
                return $"SLOT {slot} - LVL {data.Level} - {data.LastSaveTime:dd/MM/yyyy HH:mm}";

            return $"SLOT {slot} - CORROMPIDO";
        }

        public void Load()
        {
            LoadSlot(ActiveSlot);
        }

        public void LoadSlot(int slot)
        {
            ActiveSlot = Math.Clamp(slot, 1, SlotCount);
            Directory.CreateDirectory(SaveDirectory);

            string savePath = GetSlotPath(ActiveSlot);
            string backupPath = GetBackupPath(ActiveSlot);

            Console.WriteLine($"[SaveManager] A carregar slot {ActiveSlot}...");

            if (File.Exists(savePath) && TryDeserialize(savePath, out SaveData? data))
            {
                CurrentSave = data!;
                Console.WriteLine($"[SaveManager] Slot {ActiveSlot} carregado com sucesso.");
                return;
            }

            if (File.Exists(backupPath) && TryDeserialize(backupPath, out SaveData? backupData))
            {
                CurrentSave = backupData!;
                File.Copy(backupPath, savePath, overwrite: true);
                Console.WriteLine($"[SaveManager] Backup do slot {ActiveSlot} carregado com sucesso.");
                return;
            }

            Console.WriteLine($"[SaveManager] Slot {ActiveSlot} vazio. A criar novo save.");
            CurrentSave = new SaveData();
            SaveSlot(ActiveSlot);
        }

        public void Save()
        {
            SaveSlot(ActiveSlot);
        }

        public void SaveSlot(int slot)
        {
            ActiveSlot = Math.Clamp(slot, 1, SlotCount);

            try
            {
                Directory.CreateDirectory(SaveDirectory);

                string savePath = GetSlotPath(ActiveSlot);
                string backupPath = GetBackupPath(ActiveSlot);
                string tempPath = savePath + ".tmp";

                CurrentSave.LastSaveTime = DateTime.Now;

                string json = JsonSerializer.Serialize(CurrentSave, _jsonOptions);
                File.WriteAllText(tempPath, json);

                if (File.Exists(savePath))
                    File.Copy(savePath, backupPath, overwrite: true);

                File.Move(tempPath, savePath, overwrite: true);

                Console.WriteLine($"[SaveManager] Slot {ActiveSlot} guardado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveManager] ERRO ao guardar slot {ActiveSlot}: {ex.Message}");
            }
        }

        public void SaveFromPlayerData(PlayerData playerData, float worldX = 0f, float worldY = 0f)
        {
            SaveFromPlayerDataToSlot(playerData, ActiveSlot, worldX, worldY);
        }

        public void SaveFromPlayerDataToSlot(PlayerData playerData, int slot, float worldX = 0f, float worldY = 0f)
        {
            ActiveSlot = Math.Clamp(slot, 1, SlotCount);

            CurrentSave.Level = playerData.Level;
            CurrentSave.XP = playerData.XP;
            CurrentSave.Money = playerData.Money;
            CurrentSave.TotalWins = playerData.TotalWins;
            CurrentSave.TotalLosses = playerData.TotalLosses;

            CurrentSave.DefeatedRivals = new List<string>(playerData.DefeatedRivals);

            CurrentSave.EngineLevel = playerData.EngineLevel;
            CurrentSave.TiresLevel = playerData.TiresLevel;
            CurrentSave.TurboLevel = playerData.TurboLevel;
            CurrentSave.NitroLevel = playerData.NitroLevel;

            CurrentSave.CarColorIndex = playerData.CarColorIndex;
            CurrentSave.ActiveCarId = playerData.ActiveCarId;

            CurrentSave.WorldPositionX = worldX != 0f ? worldX : playerData.WorldPositionX;
            CurrentSave.WorldPositionY = worldY != 0f ? worldY : playerData.WorldPositionY;
            CurrentSave.WorldRotation = playerData.WorldRotation;

            CurrentSave.CarDamage = playerData.CarDamage;
            CurrentSave.BestLapTimes = new List<float>(playerData.BestLapTimes);
            CurrentSave.LastSaveTime = DateTime.Now;

            SaveSlot(ActiveSlot);
        }

        public void AutoSaveAfterRace(PlayerData playerData, float worldX, float worldY)
        {
            Console.WriteLine("[SaveManager] Auto-save após corrida...");
            SaveFromPlayerData(playerData, worldX, worldY);
        }

        public void AutoSaveAfterPurchase(PlayerData playerData, float worldX, float worldY)
        {
            Console.WriteLine("[SaveManager] Auto-save após compra...");
            SaveFromPlayerData(playerData, worldX, worldY);
        }

        public void AutoSaveAfterRepair(PlayerData playerData, float worldX, float worldY)
        {
            Console.WriteLine("[SaveManager] Auto-save após reparação...");
            SaveFromPlayerData(playerData, worldX, worldY);
        }

        public void NewGame()
        {
            NewGameSlot(ActiveSlot);
        }

        public void NewGameSlot(int slot)
        {
            ActiveSlot = Math.Clamp(slot, 1, SlotCount);
            CurrentSave = new SaveData();
            SaveSlot(ActiveSlot);
        }

        private bool TryDeserialize(string path, out SaveData? result)
        {
            try
            {
                string json = File.ReadAllText(path);

                if (string.IsNullOrWhiteSpace(json))
                {
                    result = null;
                    return false;
                }

                result = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);

                if (result == null || result.Level < 1 || result.Level > 20)
                {
                    result = null;
                    return false;
                }

                return true;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[SaveManager] JSON inválido em {path}: {ex.Message}");
                result = null;
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveManager] ERRO ao ler {path}: {ex.Message}");
                result = null;
                return false;
            }
        }
    }
}
