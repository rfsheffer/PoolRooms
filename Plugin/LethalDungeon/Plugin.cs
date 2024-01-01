using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DunGen;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using static LethalLib.Modules.Levels;

namespace LethalDungeon
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class LethalDungeon : BaseUnityPlugin
    {
        private const string modGUID = "LethalDungeon";
        private const string modName = "LethalDungeon";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static LethalDungeon Instance;

        internal ManualLogSource mls;

        public static AssetBundle DungeonAssets;

        // Configs
        private ConfigEntry<int> configRarity;
        private ConfigEntry<string> configMoons;
        private ConfigEntry<bool> configGuaranteed;

        private string[] MoonConfigs = 
        {
            "all",
            "paid",
            "titan"
        };

        private void Awake()
        {
            if (Instance == null) 
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            DungeonAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "exampledungeon"));
            if (DungeonAssets == null) 
            {
                mls.LogError("Failed to load Dungeon assets.");
                return;
            }

            harmony.PatchAll(typeof(LethalDungeon));
            harmony.PatchAll(typeof(RoundManagerPatch));

            // Config setup
            configRarity = Config.Bind("General", 
                "Rarity", 
                100, 
                new ConfigDescription("How rare it is for the dungeon to be chosen. Higher values increases the chance of spawning the dungeon.", 
                new AcceptableValueRange<int>(0, 300)));
            configMoons = Config.Bind("General", 
                "Moons", 
                "all", 
                new ConfigDescription("The moon(s) that the dungeon can spawn on, from the given presets.", 
                new AcceptableValueList<string>(MoonConfigs)));
            configGuaranteed = Config.Bind("General", 
                "Guaranteed", 
                false, 
                new ConfigDescription("If enabled, the dungeon will be effectively guaranteed to spawn. Only recommended for debugging/sightseeing purposes."));

            DunGen.Graph.DungeonFlow DungeonFlow = DungeonAssets.LoadAsset<DunGen.Graph.DungeonFlow>("assets/Example/Flow/ExampleFlow.asset");
            if (DungeonFlow == null) 
            {
                mls.LogError("Failed to load Dungeon Flow.");
                return;
            }

            string sMoonType = configMoons.Value.ToLower(); // Convert to lower just in case the user put in caps characters by accident, for leniency
            LevelTypes LevelType = GetLevelTypeFromMoonConfig(sMoonType);
            if (LevelType == LevelTypes.None) 
            {
                mls.LogError("Config file invalid, moon config does not match one of the preset values.");
                return;
            }
            mls.LogInfo($"Moon type string \"{sMoonType}\" got type(s) {LevelType}");

            LethalLib.Extras.DungeonDef DungeonDef = ScriptableObject.CreateInstance<LethalLib.Extras.DungeonDef>();
            DungeonDef.dungeonFlow = DungeonFlow;
            DungeonDef.rarity = configGuaranteed.Value ? 99999 : configRarity.Value; // Set to a value so high it is pretty hard for it not to be chosen.
            //DungeonDef.firstTimeDungeonAudio = DungeonAssets.LoadAsset<AudioClip>("TODO?");

            LethalLib.Modules.Dungeon.AddDungeon(DungeonDef, LevelType);

            mls.LogInfo($"Lethal Dungeon for Lethal Company [Version {modVersion}] successfully loaded.");
        }

        private LevelTypes GetLevelTypeFromMoonConfig(string sConfigName)
        {
            switch (sConfigName)
            {
                // Special names to use several at once
                case "all": return (LevelTypes.ExperimentationLevel | LevelTypes.AssuranceLevel | LevelTypes.VowLevel | LevelTypes.OffenseLevel | LevelTypes.MarchLevel |
                                    LevelTypes.RendLevel | LevelTypes.DineLevel | LevelTypes.TitanLevel);
                case "paid": return (LevelTypes.TitanLevel | LevelTypes.DineLevel | LevelTypes.RendLevel);
                case "easy": return (LevelTypes.ExperimentationLevel | LevelTypes.AssuranceLevel | LevelTypes.VowLevel | LevelTypes.OffenseLevel | LevelTypes.MarchLevel);

                // Single moons
                case "titan": 
                    return LevelTypes.TitanLevel;
                case "rend": 
                    return LevelTypes.RendLevel;
                case "dine": 
                    return LevelTypes.DineLevel;
                case "experimentation": 
                    return LevelTypes.ExperimentationLevel;
                case "assurance": 
                    return LevelTypes.AssuranceLevel;
                case "vow": 
                    return LevelTypes.VowLevel;
                case "offense": 
                    return LevelTypes.OffenseLevel;
                case "march": 
                    return LevelTypes.MarchLevel;
                default: 
                    return LevelTypes.None;
            }
        }

        // Patch to update our dummy objects (entrances, vents, turrets, mines, scrap) with the real prefab references
        [HarmonyPatch(typeof(RoundManager))]
        internal class RoundManagerPatch
        {
            // After generating the dungeon 
            [HarmonyPatch("GenerateNewFloor")]
            [HarmonyPostfix]
            static void GenerateNewFloor(ref RuntimeDungeon ___dungeonGenerator)
            {
                if (___dungeonGenerator.Generator.DungeonFlow.name != "ExampleFlow")
                {
                    return;
                }
                Instance.mls.LogInfo("Attempting to fix entrance teleporters.");
                SpawnSyncedObject[] SyncedObjects = FindObjectsOfType<SpawnSyncedObject>();
                NetworkManager networkManager = FindObjectOfType<NetworkManager>();
                NetworkPrefab networkVentPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "VentEntrance");
                if (networkVentPrefab == null) 
                {
                    Instance.mls.LogError("Failed to find VentEntrance prefab.");
                    return;
                }
                bool bFoundEntranceA = false;
                bool bFoundEntranceB = false;
                int iVentsFound = 0;
                foreach (SpawnSyncedObject syncedObject in SyncedObjects) 
                {
                    if (syncedObject.spawnPrefab.name == "EntranceTeleportA_EMPTY") 
                    {
                        NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "EntranceTeleportA");
                        if (networkPrefab == null) 
                        {
                            Instance.mls.LogError("Failed to find EntranceTeleportA prefab.");
                            return;
                        }
                        Instance.mls.LogInfo("Found and replaced EntranceTeleportA prefab.");
                        bFoundEntranceA = true;
                        syncedObject.spawnPrefab = networkPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "EntranceTeleportB_EMPTY") 
                    {
                        NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "EntranceTeleportB");
                        if (networkPrefab == null) 
                        {
                            Instance.mls.LogError("Failed to find EntranceTeleportB prefab.");
                            return;
                        }
                        Instance.mls.LogInfo("Found and replaced EntranceTeleportB prefab.");
                        bFoundEntranceB = true;
                        syncedObject.spawnPrefab = networkPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "VentDummy") 
                    {
                        Instance.mls.LogInfo("Found and replaced VentEntrance prefab.");
                        iVentsFound++;
                        syncedObject.spawnPrefab = networkVentPrefab.Prefab;
                    }
                }
                if (!bFoundEntranceA && !bFoundEntranceB) 
                {
                    Instance.mls.LogError("Failed to find entrance teleporters to replace.");
                    return;
                }
                if (iVentsFound == 0)
                {
                    Instance.mls.LogError("No vents found to replace.");
                }
                else
                {
                    Instance.mls.LogInfo($"{iVentsFound} vents found and replaced with network prefab.");
                }
            }

            // Just before spawning the scrap (the level is ready at this point) fix up our referenes to the item groups
            [HarmonyPatch("SpawnScrapInLevel")]
            [HarmonyPrefix]
            private static bool SpawnScrapInLevel(ref SelectableLevel ___currentLevel, ref RuntimeDungeon ___dungeonGenerator)
            {
                if (___dungeonGenerator.Generator.DungeonFlow.name != "ExampleFlow")
                {
                    return true;
                }
                // Grab the general and tabletop item groups from the bottle bin (a common item across all 8 moons right now)
                SpawnableItemWithRarity bottleItem = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Bottles");
                if (bottleItem == null)
                {
                    Instance.mls.LogError("Failed to find bottle bin item for reference snatching; is this a custom moon without the bottle bin item?");
                    return true;
                }
                // Grab the small item group from the fancy glass (only appears on paid moons, so this one is optional and replaced with tabletop items if invalid)
                SpawnableItemWithRarity fancyGlassItem = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Golden cup");

                // Grab the item groups
                ItemGroup itemGroupGeneral = bottleItem.spawnableItem.spawnPositionTypes.Find(x => x.name == "GeneralItemClass");
                ItemGroup itemGroupTabletop = bottleItem.spawnableItem.spawnPositionTypes.Find(x => x.name == "TabletopItems");

                // Use tabletop items in place of small items if not on a paid moon
                ItemGroup itemGroupSmall = (fancyGlassItem == null) ? itemGroupTabletop : fancyGlassItem.spawnableItem.spawnPositionTypes.Find(x => x.name == "SmallItems");
                RandomScrapSpawn[] scrapSpawns = FindObjectsOfType<RandomScrapSpawn>();
                foreach (RandomScrapSpawn scrapSpawn in scrapSpawns)
                {
                    switch (scrapSpawn.spawnableItems.name)
                    {
                        case "GeneralItemClassDUMMY": scrapSpawn.spawnableItems = itemGroupGeneral; break;
                        case "TabletopItemsDUMMY": scrapSpawn.spawnableItems = itemGroupTabletop; break;
                        case "SmallItemsDUMMY": scrapSpawn.spawnableItems = itemGroupSmall; break;
                    }
                }
                return true;
            }
        }
    }
}
