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
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves"); // pasta "Saves" dentro do diretório do jogo
        private static readonly string SaveFilePath =
            Path.Combine(SaveDirectory, "burnoutcity_save.json"); // arquivo "save.json" dentro da pasta "Saves"
        private static readonly string BackupFilePath =
            Path.Combine(SaveDirectory, "burnoutcity_save.bak"); // backup para casos de corrupção
        
        public SaveData CurrentSave { get; private set; } = new SaveData();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // para facilitar leitura manual
            PropertyNameCaseInsensitive = true // para evitar problemas com maiúsculas/minúsculas
        };

        public void Load()
        {
            Console.WriteLine("[SaveManager] A Carregar Save...");
            Directory.CreateDirectory(SaveDirectory);
            if (File.Exists(SaveFilePath))
            {
                if(TryDeserialize(SaveFilePath, out SaveData data))
                {
                    CurrentSave = data!;
                    Console.WriteLine("[SaveManager] Save Carregado com Sucesso!");
                    return;
                }

                Console.WriteLine("[SaveManager] AVISO: Falha ao Carregar Save Principal. A tentar Backup...");
                if(File.Exists(BackupFilePath) && TryDeserialize(BackupFilePath, out SaveData? backupData))
                {
                    CurrentSave = backupData!;
                    File.Copy(BackupFilePath, SaveFilePath, overwrite: true); // restaurar backup para o arquivo principal
                    Console.WriteLine("[SaveManager] Backup Carregado com Sucesso!");
                    return;
                }

                Console.WriteLine("[SaveManager] ERRO: Falha a Carregar Save Principal e Backup. A Criar Novo Save...");
                DeleteCorruptedFiles();
            }
            else
            {
                Console.WriteLine("[SaveManager] Nenhum Save Encontrado. A Criar Novo Save...");
                CurrentSave = new SaveData();
            }

            CurrentSave = new SaveData();
            Save(); // salvar imediatamente para criar os arquivos

        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SaveDirectory);
                string tempPath = SaveFilePath + ".tmp";
                string json = JsonSerializer.Serialize(CurrentSave, _jsonOptions);
                File.WriteAllText(tempPath, json);
                
                if(File.Exists(SaveFilePath))
                {
                    File.Copy(SaveFilePath, BackupFilePath, overwrite: true); // criar backup do save atual
                }
                File.Move(tempPath, SaveFilePath, overwrite: true); // mover temp para o save final
                Console.WriteLine("[SaveManager] Save Salvo com Sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveManager] ERRO ao Salvar: {ex.Message}");
            }
        }

        public void SaveFromPlayerData(PlayerData playerData, float worldX = 0f, float worldY = 0f)
        {
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
            CurrentSave.CarDamage = playerData.CarDamage;
            CurrentSave.WorldPositionX = worldX != 0 ? worldX : playerData.WorldPositionX;
            CurrentSave.WorldPositionY = worldY != 0 ? worldY : playerData.WorldPositionY;
            CurrentSave.BestLapTimes = new List<float>(playerData.BestLapTimes);
            CurrentSave.LastSaveTime = DateTime.Now;

            Save();
        }

        public void AutoSaveAfterRace(PlayerData playerData, float worldX, float worldY)
        {
            Console.WriteLine("[SaveManager] Auto-Saving após corrida...");
            SaveFromPlayerData(playerData, worldX, worldY);
        }

        public void AutoSaveAfterPurchase(PlayerData playerData, float worldX, float worldY)
        {
            Console.WriteLine("[SaveManager] Auto-Saving após compra...");
            SaveFromPlayerData(playerData, worldX, worldY);
        }

        public void AutoSaveAfterRepair(PlayerData playerData, float worldX, float worldY)
        {
            Console.WriteLine("[SaveManager] Auto-Saving após reparacao...");
            SaveFromPlayerData(playerData, worldX, worldY);
        }

        public void NewGame()
        {
            Console.WriteLine("[SaveManager] Iniciando Novo Jogo...");
            CurrentSave = new SaveData();
            Save();
        }

        private bool TryDeserialize(string path, out SaveData? result)
        {
            try
            {
                string json = File.ReadAllText(path);
                if(string.IsNullOrWhiteSpace(json))
                {
                    result = null;
                    return false;
                }
                result = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);

                if(result == null || result.Level < 1 || result.Level > 20)
                {
                    result = null;
                    return false;
                }
                return true;
            }   
            catch (JsonException ex)
            {
                Console.WriteLine($"[SaveManager] JsonInvalido em {path}: {ex.Message}");
                result = null;
                return false;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[SaveManager] ERRO ao Ler {path}: {ex.Message}");
                result = null;
                return false;
            }
        }

    
    private void DeleteCorruptedFiles()
    {
        try
        {
            if(File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
        }
        catch{}

        try
        {
            if(File.Exists(BackupFilePath))
            {
                File.Delete(BackupFilePath);
            }
        }
        catch{}
    }  

    }
}
