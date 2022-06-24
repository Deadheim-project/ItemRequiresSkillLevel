using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ItemRequiresSkillLevel
{
    [BepInPlugin(PluginGUID, PluginGUID, Version)]
    [HarmonyPatch]
    public class ItemRequiresSkillLevel : BaseUnityPlugin
    {
        public const string Version = "1.1.1";
        public const string PluginGUID = "Detalhes.ItemRequiresSkillLevel";
        static ConfigSync configSync = new ConfigSync(PluginGUID) { DisplayName = PluginGUID, CurrentVersion = Version, MinimumRequiredVersion = Version };

        public static CustomSyncedValue<Dictionary<string, string>> YamlData = new CustomSyncedValue<Dictionary<string, string>>(configSync, "ItemRequiresSkillLevel yaml");
        internal static ConfigEntry<bool>? serverSyncLock;
        internal static ConfigEntry<bool> GenerateListWithAllEquipableItems;
        internal static ConfigEntry<string> RequiresText;
        internal static ConfigEntry<string> cantEquipColor;
        internal static ConfigEntry<string> canEquipColor;

        Harmony _harmony = new Harmony(PluginGUID);

        internal static string ConfigFileName = PluginGUID + ".yml";
        public static string ConfigPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        public static string AllItemsConfigPath = Paths.ConfigPath + Path.DirectorySeparatorChar + PluginGUID + "ALLITEMS" + ".yml";

        private void Awake()
        {
            RequirementService.Init();
            _harmony.PatchAll();
            YamlData.ValueChanged += RequirementService.Load;
            var val = (new string[] { ConfigPath }.ToDictionary(f => f, File.ReadAllText));
            YamlData.AssignLocalValue(val);
            SetupWatcher();

            serverSyncLock = config("General", "Lock Configuration", true, "Lock Configuration");
            GenerateListWithAllEquipableItems = config("General", "GenerateListWithAllEquipableItems", false, "GenerateListWithAllEquipableItems");
            canEquipColor = config("General", "canEquipColor", "green", "canEquipColor");
            cantEquipColor = config("General", "cantEquipColor", "red", "cantEquipColor");
            GenerateListWithAllEquipableItems = config("General", "GenerateListWithAllEquipableItems", false, "GenerateListWithAllEquipableItems");
            RequiresText = config("General", "RequiresText", "\nRequires <color={0}>{1} {2}</color>", "RequiresText");

            configSync.AddLockingConfigEntry(serverSyncLock);
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadFile;
            watcher.Created += ReadFile;
            watcher.Renamed += ReadFile;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }
        private void ReadFile(object sender, FileSystemEventArgs e)
        {
            var val = new string[] { ConfigPath }.ToDictionary(f => f, File.ReadAllText);
            YamlData.AssignLocalValue(val);
        }

        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }
}
