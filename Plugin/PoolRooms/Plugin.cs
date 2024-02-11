using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using static LethalLib.Modules.Levels;
using LethalLevelLoader;
using DunGen.Graph;
using UnityEngine.UIElements.Collections;

namespace PoolRooms
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.HardDependency)]
    public class PoolRooms : BaseUnityPlugin
    {
        private const string modGUID = "skidz.PoolRooms";
        private const string modName = "PoolRooms";
        private const string modVersion = "0.1.5";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static PoolRooms Instance;

        internal ManualLogSource mls;

        public static AssetBundle DungeonAssets;

        // Configs
        private ConfigEntry<int> configBaseRarity;
        private ConfigEntry<string> configMoons;
        private ConfigEntry<bool> configGuaranteed;

        // The loaded dungeon flow
        private static DunGen.Graph.DungeonFlow DungeonFlow = null;

        // Special Dungeon Items
        private static List<Item> PoolItems = new List<Item>();
        private static List<int> PoolItemRarities = new List<int>();
        private static int PoolItemsIndex = 0;

        private string[] MoonIdentifiers = 
        {
            "All",
            "Free",
            "Paid",
            "Tier1",
            "Tier2",
            "Tier3",
            "Titan",
            "Rend",
            "Dine",
            "Experimentation",
            "Assurance",
            "Vow",
            "Offense",
            "March",
        };

        private Dictionary<string, string[]> MoonIDToMoonsMapping = new Dictionary<string, string[]>
        {
            { "all", new string[] { "Experimentation", "Assurance", "Vow", "Offense", "March", "Rend", "Dine", "Titan" } },
            { "free", new string[] { "Experimentation", "Assurance", "Vow", "Offense", "March" } },
            { "paid", new string[] { "Rend", "Dine", "Titan" } },
            { "tier1", new string[] { "Experimentation", "Assurance", "Vow" } },
            { "tier2", new string[] { "Offense", "March" } },
            { "tier3", new string[] { "Rend", "Dine", "Titan" } },

            { "titan", new string[] { "Titan" } },
            { "rend", new string[] { "Rend" } },
            { "dine", new string[] { "Dine" } },
            { "experimentation", new string[] { "Experimentation" } },
            { "assurance", new string[] { "Assurance" } },
            { "vow", new string[] { "Vow" } },
            { "offense", new string[] { "Offense" } },
            { "march", new string[] { "March" } },
        };

        private void Awake()
        {
            if (Instance == null) 
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modName);

            mls.LogInfo($"Behaviors Loaded: {PoolRoomsWaterBehaviour.BehaviorsVer}");

            // Unity Netcode patcher requirement
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            harmony.PatchAll(typeof(PoolRooms));
            harmony.PatchAll(typeof(RoundManagerPatch));

            // Config setup
            configMoons = Config.Bind("General",
                "Moons",
                "All:100",
                new ConfigDescription("The moon(s) that the dungeon can spawn on with rarity weight, " +
                                      "from the given presets or combined comma sep list ex 'Vow:100,March:50,Paid:30'. Custom moons should be supported if you know the name. " +
                                      $"A list of acceptable IDs not including custom: {string.Join(", ", MoonIdentifiers)}",
                                      null));
            configBaseRarity = Config.Bind("General",
                "BaseRarity",
                100,
                new ConfigDescription("A baseline rarity weight for each moon. Only used if Guaranteed is false and a moon doesn't have an explicit rarity weight.",
                new AcceptableValueRange<int>(1, 500)));
            configGuaranteed = Config.Bind("General", 
                "Guaranteed", 
                false, 
                new ConfigDescription("If true the dungeons rarity will be defaulted to a high weighting which will most likely trump all other weights and guarantee this dungeon flow."));

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DungeonAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "poolrooms"));
            if (DungeonAssets == null)
            {
                mls.LogError("Failed to load Dungeon assets.");
                return;
            }

            DungeonFlow = DungeonAssets.LoadAsset<DunGen.Graph.DungeonFlow>("assets/PoolRooms/Flow/PoolRoomsFlow.asset");
            if (DungeonFlow == null) 
            {
                mls.LogError("Failed to load Dungeon Flow.");
                return;
            }

            AudioClip FirstTimeDungeonAudio = DungeonAssets.LoadAsset<AudioClip>("Assets/PoolRooms/Sound/PoolRoomsFirstTime.wav");

            // Lethal Lib version of dungeon addition
            //string sMoonType = configMoons.Value.ToLower();
            //LevelTypes LevelType = GetLevelTypeFromMoonConfig(sMoonType);
            //if (LevelType == LevelTypes.None)
            //{
            //    mls.LogError("Config file invalid, moon config does not match one of the preset values.");
            //    return;
            //}
            //mls.LogInfo($"Moon type string \"{sMoonType}\" got type(s) {LevelType}");
            //LethalLib.Extras.DungeonDef DungeonDef = ScriptableObject.CreateInstance<LethalLib.Extras.DungeonDef>();
            //DungeonDef.dungeonFlow = DungeonFlow;
            //DungeonDef.rarity = configGuaranteed.Value ? 99999 : configRarity.Value; // Set to a value so high it is pretty hard for it not to be chosen.
            //DungeonDef.firstTimeDungeonAudio = DungeonAssets.LoadAsset<AudioClip>("TODO?");
            //LethalLib.Modules.Dungeon.AddDungeon(DungeonDef, LevelType);

            // Lethal Level Loader Version
            ExtendedDungeonFlow myExtendedDungeonFlow = ScriptableObject.CreateInstance<ExtendedDungeonFlow>();
            myExtendedDungeonFlow.dungeonFlow = DungeonFlow;
            myExtendedDungeonFlow.dungeonFirstTimeAudio = FirstTimeDungeonAudio;
            myExtendedDungeonFlow.dungeonDisplayName = modName;
            myExtendedDungeonFlow.contentSourceName = modName;
            myExtendedDungeonFlow.generateAutomaticConfigurationOptions = false;

            // Setup levels to spawn in
            List<StringWithRarity> levels = GetLevelStringsWithRarity(configMoons.Value.ToLower(), configBaseRarity.Value, configGuaranteed.Value ? 9999 : -1);
            foreach (StringWithRarity level in levels)
            {
                myExtendedDungeonFlow.manualPlanetNameReferenceList.Add(level);
                mls.LogInfo($"Added to moon '{level.Name}' with a rarity weight of {level.Rarity}");
            }
            PatchedContent.RegisterExtendedDungeonFlow(myExtendedDungeonFlow);


            // Register our special dungeon items
            Item LifeBuoyItem = DungeonAssets.LoadAsset<Item>("Assets/PoolRooms/Scrap/LifeBuoy.asset");
            LethalLib.Modules.Utilities.FixMixerGroups(LifeBuoyItem.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(LifeBuoyItem.spawnPrefab);
            PoolItems.Add(LifeBuoyItem);
            PoolItemRarities.Add(60);
            LethalLib.Modules.Items.RegisterScrap(LifeBuoyItem, 60, LevelTypes.None);

            Item PoolNetItem = DungeonAssets.LoadAsset<Item>("Assets/PoolRooms/Scrap/PoolNet.asset");
            LethalLib.Modules.Utilities.FixMixerGroups(PoolNetItem.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(PoolNetItem.spawnPrefab);
            PoolItems.Add(PoolNetItem);
            PoolItemRarities.Add(100);
            LethalLib.Modules.Items.RegisterScrap(PoolNetItem, 100, LevelTypes.None);

            Item PoolBallItem = DungeonAssets.LoadAsset<Item>("Assets/PoolRooms/Scrap/PoolBall.asset");
            LethalLib.Modules.Utilities.FixMixerGroups(PoolBallItem.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(PoolBallItem.spawnPrefab);
            PoolItems.Add(PoolBallItem);
            PoolItemRarities.Add(100);
            LethalLib.Modules.Items.RegisterScrap(PoolBallItem, 100, LevelTypes.None);

            // Pool Rooms Doors
            GameObject LockerDoor = DungeonAssets.LoadAsset<GameObject>("Assets/PoolRooms/Prefabs/LockerDoor.prefab");
            LethalLib.Modules.Utilities.FixMixerGroups(LockerDoor);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(LockerDoor);

            GameObject PoolRoomsDoor = DungeonAssets.LoadAsset<GameObject>("Assets/PoolRooms/Prefabs/PoolRoomsDoor.prefab");
            LethalLib.Modules.Utilities.FixMixerGroups(PoolRoomsDoor);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(PoolRoomsDoor);

            // Fixup room water stuff
            GameObject RoomWater = DungeonAssets.LoadAsset<GameObject>("Assets/PoolRooms/Prefabs/RoomWater.prefab");
            LethalLib.Modules.Utilities.FixMixerGroups(RoomWater);

            GameObject WaterBehavior = DungeonAssets.LoadAsset<GameObject>("Assets/PoolRooms/Prefabs/WaterBehavior.prefab");
            LethalLib.Modules.Utilities.FixMixerGroups(WaterBehavior);


            mls.LogInfo($"Pool Rooms [Version {modVersion}] successfully loaded.");
        }

        // Converts a string 'vow:100,march:50,paid:100' to a list of maps with rarity weight
        private List<StringWithRarity> GetLevelStringsWithRarity(string delimitedList, int baseRarity, int fixedRarity)
        {
            List<StringWithRarity> listOut = new List<StringWithRarity>();
            HashSet<string> lookup = new HashSet<string>();

            string[] names = delimitedList.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach(string name in names)
            {
                string[] nameAndRarityStr = name.RemoveWhitespace().Split(':');
                try
                {
                    string levelID = nameAndRarityStr[0];
                    int rarityWeight = baseRarity;
                    if(fixedRarity > 0)
                    {
                        rarityWeight = fixedRarity;
                    }
                    else if(nameAndRarityStr.Length >= 2)
                    {
                        rarityWeight = int.Parse(nameAndRarityStr[1]);
                    }

                    string[] levelsToAdd = MoonIDToMoonsMapping.Get(levelID.ToLower());
                    if(levelsToAdd != null)
                    {
                        foreach(string mapLevelID in levelsToAdd)
                        {
                            if (!lookup.Contains(mapLevelID))
                            {
                                listOut.Add(new StringWithRarity(mapLevelID, rarityWeight));
                                lookup.Add(mapLevelID);
                            }
                        }
                    }
                    else
                    {
                        // Allow adding custom moons, they need to have the ID right for it to work I suspect...
                        if (!lookup.Contains(levelID))
                        {
                            listOut.Add(new StringWithRarity(levelID, rarityWeight));
                            lookup.Add(levelID);
                        }
                    }
                }
                finally { }
            }

            return listOut;
        }

        /*private LevelTypes GetLevelTypeFromMoonConfig(string sConfigName)
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
        }*/

        // Patch to update our dummy objects (entrances, vents, turrets, mines, scrap, storage shelving) with the real prefab references
        [HarmonyPatch(typeof(RoundManager))]
        internal class RoundManagerPatch
        {
            // Before generating, update the Pool Rooms flow to have three exits if heading to march, otherwise 1
            [HarmonyPatch("GenerateNewFloor")]
            [HarmonyPrefix]
            static void GenerateNewFloorPre(ref SelectableLevel ___currentLevel)
            {
                if (DungeonFlow)
                {
                    foreach (DunGen.Graph.DungeonFlow.GlobalPropSettings propSetting in DungeonFlow.GlobalProps)
                    {
                        if (propSetting.ID == 1231)
                        {
                            if (___currentLevel.sceneName == "Level4March")
                            {
                                Instance.mls.LogWarning("Setting Pool Rooms Dungeon Flow to have 3 exits for March");
                                propSetting.Count = new DunGen.IntRange(3, 3);
                            }
                            else
                            {
                                Instance.mls.LogWarning("Setting Pool Rooms Dungeon Flow to have 1 exit");
                                propSetting.Count = new DunGen.IntRange(1, 1);
                            }
                            break;
                        }
                    }
                }
            }

            // After generating the dungeon fix up the sync'd objects which contain our dummies with the real prefabs
            [HarmonyPatch("GenerateNewFloor")]
            [HarmonyPostfix]
            static void GenerateNewFloorPost(ref RuntimeDungeon ___dungeonGenerator)
            {
                if (___dungeonGenerator.Generator.DungeonFlow.name != "PoolRoomsFlow")
                {
                    return;
                }

                Instance.mls.LogWarning("Using Pool Rooms Dungeon Flow!");

                Instance.mls.LogInfo("Fixing SpawnSyncedObject network prefabs...");
                SpawnSyncedObject[] SyncedObjects = FindObjectsOfType<SpawnSyncedObject>();
                NetworkManager networkManager = FindObjectOfType<NetworkManager>();
                NetworkPrefab realVentPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "VentEntrance");
                if (realVentPrefab == null)
                {
                    Instance.mls.LogError("Failed to find VentEntrance prefab.");
                    return;
                }

                NetworkPrefab realEntranceAPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "EntranceTeleportA");
                if (realEntranceAPrefab == null)
                {
                    Instance.mls.LogError("Failed to find EntranceTeleportA prefab.");
                    return;
                }

                NetworkPrefab realEntranceBPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "EntranceTeleportB");
                if (realEntranceBPrefab == null)
                {
                    Instance.mls.LogError("Failed to find EntranceTeleportB prefab.");
                    return;
                }

                NetworkPrefab realStorageShelfPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "StorageShelfContainer");
                if (realStorageShelfPrefab == null)
                {
                    Instance.mls.LogError("Failed to find StorageShelfContainer prefab.");
                    return;
                }

                NetworkPrefab realSteelDoorMapModelPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "SteelDoorMapModel");
                if (realSteelDoorMapModelPrefab == null)
                {
                    Instance.mls.LogError("Failed to find SteelDoorMapModel prefab.");
                    return;
                }

                NetworkPrefab realSteamValvePrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "SteamValve");
                if (realSteamValvePrefab == null)
                {
                    Instance.mls.LogError("Failed to find SteamValve prefab.");
                    return;
                }

                NetworkPrefab realLungApparatusPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "LungApparatus");
                if (realLungApparatusPrefab == null)
                {
                    Instance.mls.LogError("Failed to find LungApparatus prefab.");
                    return;
                }

                NetworkPrefab realBreakerBoxPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "BreakerBox");
                if (realBreakerBoxPrefab == null)
                {
                    Instance.mls.LogError("Failed to find BreakerBox prefab.");
                    return;
                }

                NetworkPrefab realBigDoorPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "BigDoor");
                if (realBigDoorPrefab == null)
                {
                    Instance.mls.LogError("Failed to find BigDoor prefab.");
                    return;
                }

                bool bFoundEntranceA = false;
                bool bFoundEntranceB = false;
                int iVentsFound = 0;
                int iDoorsFound = 0;
                foreach (SpawnSyncedObject syncedObject in SyncedObjects)
                {
                    if (!syncedObject || !syncedObject.spawnPrefab)
                    {
                        if (syncedObject)
                        {
                            Instance.mls.LogWarning($"{syncedObject.name} has no spawn prefab set...");
                        }
                        continue;
                    }
                    if (syncedObject.spawnPrefab.name == "PoolRooms_EntranceTeleportA_DUMMY")
                    {
                        Instance.mls.LogInfo("Found and replaced EntranceTeleportA prefab.");
                        bFoundEntranceA = true;
                        syncedObject.spawnPrefab = realEntranceAPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_EntranceTeleportB_DUMMY")
                    {
                        Instance.mls.LogInfo("Found and replaced EntranceTeleportB prefab.");
                        bFoundEntranceB = true;
                        syncedObject.spawnPrefab = realEntranceBPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_Vent_DUMMY")
                    {
                        //Instance.mls.LogInfo("Found and replaced VentEntrance prefab.");
                        iVentsFound++;
                        syncedObject.spawnPrefab = realVentPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_StorageShelf_DUMMY")
                    {
                        //Instance.mls.LogInfo("Found and replaced StorageShelfContainer prefab.");
                        syncedObject.spawnPrefab = realStorageShelfPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_SteelDoorMapModel_DUMMY")
                    {
                        //Instance.mls.LogInfo("Found and replaced SteelDoorMapModel prefab.");
                        iDoorsFound++;
                        syncedObject.spawnPrefab = realSteelDoorMapModelPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_SteamValve_DUMMY")
                    {
                        //Instance.mls.LogInfo("Found and replaced SteamValve prefab.");
                        syncedObject.spawnPrefab = realSteamValvePrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_LungApparatus_DUMMY")
                    {
                        Instance.mls.LogInfo("Found and replaced LungApparatus prefab.");
                        syncedObject.spawnPrefab = realLungApparatusPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_BreakerBox_DUMMY")
                    {
                        Instance.mls.LogInfo("Found and replaced BreakerBox prefab.");
                        syncedObject.spawnPrefab = realBreakerBoxPrefab.Prefab;
                    }
                    else if (syncedObject.spawnPrefab.name == "PoolRooms_BigDoor_DUMMY")
                    {
                        Instance.mls.LogInfo("Found and replaced BigDoor prefab.");
                        syncedObject.spawnPrefab = realBigDoorPrefab.Prefab;
                    }
                }
                if (!bFoundEntranceA && !bFoundEntranceB)
                {
                    Instance.mls.LogError("Failed to find entrance teleporters to replace. Map will not be playable!");
                    return;
                }
                if (iVentsFound == 0)
                {
                    Instance.mls.LogWarning("No vents found to replace.");
                }
                else
                {
                    Instance.mls.LogInfo($"{iVentsFound} vents found and replaced with network prefab.");
                }
                if (iDoorsFound == 0)
                {
                    Instance.mls.LogWarning("No doors found to replace.");
                }
                else
                {
                    Instance.mls.LogInfo($"{iDoorsFound} doors found and replaced with network prefab.");
                }
            }

            // Fix up turret and landmine prefab references before trying to spawn map objects
            [HarmonyPatch("SpawnMapObjects")]
            [HarmonyPrefix]
            static void SpawnMapObjects(ref SelectableLevel ___currentLevel, ref RuntimeDungeon ___dungeonGenerator)
            {
                if (___dungeonGenerator.Generator.DungeonFlow.name != "PoolRoomsFlow")
                {
                    return;
                }

                RandomMapObject[] RandomObjects = FindObjectsOfType<RandomMapObject>();
                NetworkManager networkManager = FindObjectOfType<NetworkManager>();
                NetworkPrefab realLandminePrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "Landmine");
                if (realLandminePrefab == null)
                {
                    Instance.mls.LogError("Failed to find Landmine prefab.");
                    return;
                }

                NetworkPrefab realTurretContainerPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "TurretContainer");
                if (realTurretContainerPrefab == null)
                {
                    Instance.mls.LogError("Failed to find TurretContainer prefab.");
                    return;
                }

                foreach (RandomMapObject randomObject in RandomObjects)
                {
                    List<GameObject> props = randomObject.spawnablePrefabs;
                    List<GameObject> newProps = new List<GameObject>();

                    foreach (GameObject prop in props)
                    {
                        if (prop.name == "PoolRooms_Turret_DUMMY")
                        {
                            newProps.Add(realTurretContainerPrefab.Prefab);
                        }
                        else if (prop.name == "PoolRooms_Landmine_DUMMY")
                        {
                            newProps.Add(realLandminePrefab.Prefab);
                        }
                    }

                    if(newProps.Count() > 0)
                    {
                        randomObject.spawnablePrefabs = newProps;
                    }
                }
            }


            // Just before spawning the scrap (the level is ready at this point) fix up our referenes to the item groups
            [HarmonyPatch("SpawnScrapInLevel")]
            [HarmonyPrefix]
            static void SpawnScrapInLevelPre(ref SelectableLevel ___currentLevel, ref RuntimeDungeon ___dungeonGenerator)
            {
                if (___dungeonGenerator.Generator.DungeonFlow.name != "PoolRoomsFlow")
                {
                    return;
                }
                // Look for items with stored classes.
                SpawnableItemWithRarity itemWithClasses = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Bottles");
                if (itemWithClasses == null)
                {
                    itemWithClasses = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Cash register");
                    if (itemWithClasses == null)
                    {
                        itemWithClasses = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Chemical jug");
                        if (itemWithClasses == null)
                        {
                            itemWithClasses = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Gift");
                            if (itemWithClasses == null)
                            {
                                itemWithClasses = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Tea kettle");
                                if (itemWithClasses == null)
                                {
                                    Instance.mls.LogError("Unable to find an item with spawn positions to pull from. No junk will spawn in this dungeon!");
                                    return;
                                }
                            }
                        }
                    }
                }

                // Grab the item groups
                ItemGroup itemGroupGeneral = itemWithClasses.spawnableItem.spawnPositionTypes.Find(x => x.name == "GeneralItemClass");
                ItemGroup itemGroupTabletop = itemWithClasses.spawnableItem.spawnPositionTypes.Find(x => x.name == "TabletopItems");
                if (!itemGroupGeneral || !itemGroupTabletop)
                {
                    Instance.mls.LogError($"Found an item '{itemWithClasses.spawnableItem.name}' that is suppose to have both general and table top items but no longer does...");
                    return;
                }

                // Grab the small item group from the fancy glass. It is the only item that uses it and if it isn't used will default to table top items which is similar.
                SpawnableItemWithRarity itemWithSmallItems = ___currentLevel.spawnableScrap.Find(x => x.spawnableItem.itemName == "Golden cup");
                ItemGroup itemGroupSmall = (itemWithSmallItems == null) ? itemGroupTabletop : itemWithSmallItems.spawnableItem.spawnPositionTypes.Find(x => x.name == "SmallItems");

                // Fix the item groups in our special scrap items and add them to the current moon temporarily
                Int32 rarityIndex = 0;
                foreach (Item itemToAdd in PoolItems)
                {
                    List<ItemGroup> spawnPositionTypes = new List<ItemGroup>();
                    foreach (ItemGroup group in itemToAdd.spawnPositionTypes)
                    {
                        switch (group.name)
                        {
                            case "PoolRooms_GeneralItemClass_DUMMY": spawnPositionTypes.Add(itemGroupGeneral); break;
                            case "PoolRooms_TabletopItems_DUMMY": spawnPositionTypes.Add(itemGroupTabletop); break;
                            case "PoolRooms_SmallItems_DUMMY": spawnPositionTypes.Add(itemGroupSmall); break;
                        }
                    }
                    if(spawnPositionTypes.Count > 0)
                    {
                        Instance.mls.LogInfo($"Fixing pool item '{itemToAdd.name}' item groups");
                        itemToAdd.spawnPositionTypes = spawnPositionTypes;
                    }

                    SpawnableItemWithRarity itemRarity = new SpawnableItemWithRarity();
                    itemRarity.spawnableItem = itemToAdd;
                    itemRarity.rarity = PoolItemRarities[rarityIndex];
                    ___currentLevel.spawnableScrap.Add(itemRarity);
                    ++rarityIndex;
                }

                // Store the current items index so we can remove the items once all the scrap has been spawned
                PoolItemsIndex = ___currentLevel.spawnableScrap.Count;

                // Fix all scrap spawners
                RandomScrapSpawn[] scrapSpawns = FindObjectsOfType<RandomScrapSpawn>();
                foreach (RandomScrapSpawn scrapSpawn in scrapSpawns)
                {
                    switch (scrapSpawn.spawnableItems.name)
                    {
                        case "PoolRooms_GeneralItemClass_DUMMY": scrapSpawn.spawnableItems = itemGroupGeneral; break;
                        case "PoolRooms_TabletopItems_DUMMY": scrapSpawn.spawnableItems = itemGroupTabletop; break;
                        case "PoolRooms_SmallItems_DUMMY": scrapSpawn.spawnableItems = itemGroupSmall; break;
                    }
                }
            }

            [HarmonyPatch("SpawnScrapInLevel")]
            [HarmonyPostfix]
            static void SpawnScrapInLevelPost(ref SelectableLevel ___currentLevel, ref RuntimeDungeon ___dungeonGenerator)
            {
                if (___dungeonGenerator.Generator.DungeonFlow.name != "PoolRoomsFlow")
                {
                    return;
                }

                // Remove our pool items from the scrap list so they don't show up on this moon until the next time this dungeon layout is chosen.
                ___currentLevel.spawnableScrap.RemoveRange(PoolItemsIndex - PoolItems.Count, PoolItems.Count);
            }
        }
    }

    // https://stackoverflow.com/questions/4135317/make-first-letter-of-a-string-upper-case-with-maximum-performance
    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => input[0].ToString().ToUpper() + input.Substring(1)
            };
    }
}
