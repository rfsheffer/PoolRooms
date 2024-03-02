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
using GameNetcodeStuff;
using LethalLib.Modules;
using System.Runtime.CompilerServices;

namespace PoolRooms
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.HardDependency)]
    public class PoolRooms : BaseUnityPlugin
    {
        private const string modGUID = "skidz.PoolRooms";
        private const string modName = "PoolRooms";
        private const string modVersion = "0.1.18";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static PoolRooms Instance;

        internal ManualLogSource mls;

        public static AssetBundle DungeonAssets;

        // Configs
        private ConfigEntry<bool> configUsePoolRoomsMoonsConfig;
        private ConfigEntry<int> configBaseRarity;
        private ConfigEntry<float> configMinGenerationScale;
        private ConfigEntry<float> configMaxGenerationScale;
        private ConfigEntry<string> configMoons;
        private ConfigEntry<bool> configGuaranteed;
        private ConfigEntry<bool> configEnableCustomScrap;
        private ConfigEntry<bool> configUseCustomScrapGlobally;

        // Config for the custom scrap weights
        private ConfigEntry<int> configPoolBallWeighting;
        private ConfigEntry<int> configPoolNetWeighting;
        private ConfigEntry<int> configLifeBuoyWeighting;
        private ConfigEntry<int> configWetFloorSignWeighting;

        // The loaded dungeon flow
        private static DunGen.Graph.DungeonFlow DungeonFlow = null;

        // Special Dungeon Items
        private static List<Item> PoolItems = new List<Item>();
        private static List<int> PoolItemRarities = new List<int>();
        private static List<SpawnableItemWithRarity> PoolItemsAdded = new List<SpawnableItemWithRarity>();

        private string[] MoonIdentifiers =
        {
            "Vanilla",
            "All",
            "Custom",
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

            mls.LogInfo($"Behaviors Version {PoolRoomsWaterBehaviour.BehaviorsVer} Loaded!");

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
            harmony.PatchAll(typeof(EntranceTeleportPatch));

            // Config setup
            configUsePoolRoomsMoonsConfig = Config.Bind("General",
                "UsePoolRoomsMoonsConfig",
                false,
                new ConfigDescription("If true, will use the plugins moon config over Lethal Level Loaders config generation."));
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
                new AcceptableValueRange<int>(0, 9999)));
            configMinGenerationScale = Config.Bind("General",
                "MinGenerationScale",
                1.0f,
                new ConfigDescription("The minimum scale to generate the dungeon.",
                new AcceptableValueRange<float>(0.1f, 10.0f)));
            configMaxGenerationScale = Config.Bind("General",
                "MaxGenerationScale",
                2.5f,
                new ConfigDescription("The maximum scale to generate the dungeon.",
                new AcceptableValueRange<float>(0.1f, 10.0f)));
            configGuaranteed = Config.Bind("General",
                "Guaranteed",
                false,
                new ConfigDescription("If true the dungeons rarity will be defaulted to a high weighting which will most likely trump all other weights and guarantee this dungeon flow."));
            configEnableCustomScrap = Config.Bind("General",
                "EnableCustomScrap",
                true,
                new ConfigDescription("If true, custom pool rooms scrap will be spawned. EnableCustomScrap must also be true for this to function."));
            configUseCustomScrapGlobally = Config.Bind("General",
                "UseCustomScrapGlobally",
                false,
                new ConfigDescription("If true, the custom pool rooms scrap will be added to all interiors."));

            configPoolBallWeighting = Config.Bind("General",
                "PoolBallWeighting",
                100,
                new ConfigDescription("The pool ball spawning weight",
                new AcceptableValueRange<int>(1, 9999)));
            configPoolNetWeighting = Config.Bind("General",
                "PoolNetWeighting",
                100,
                new ConfigDescription("The pool net spawning weight",
                new AcceptableValueRange<int>(1, 9999)));
            configLifeBuoyWeighting = Config.Bind("General",
                "LifeBuoyWeighting",
                60,
                new ConfigDescription("The Life Buoy spawning weight",
                new AcceptableValueRange<int>(1, 9999)));
            configWetFloorSignWeighting = Config.Bind("General",
                "WetFloorSignWeighting",
                60,
                new ConfigDescription("The Wet Floor Sign spawning weight",
                new AcceptableValueRange<int>(1, 9999)));

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

            // Lethal Level Loader setup
            ExtendedDungeonFlow myExtendedDungeonFlow = ScriptableObject.CreateInstance<ExtendedDungeonFlow>();
            myExtendedDungeonFlow.dungeonFlow = DungeonFlow;
            myExtendedDungeonFlow.dungeonFirstTimeAudio = FirstTimeDungeonAudio;
            myExtendedDungeonFlow.dungeonSizeMin = configMinGenerationScale.Value;
            myExtendedDungeonFlow.dungeonSizeMax = Math.Max(configMinGenerationScale.Value, configMaxGenerationScale.Value);
            myExtendedDungeonFlow.contentSourceName = modName;
            myExtendedDungeonFlow.dungeonDisplayName = modName;
            myExtendedDungeonFlow.generateAutomaticConfigurationOptions = !configUsePoolRoomsMoonsConfig.Value;

            // Setup levels to spawn in
            List<StringWithRarity> levels = GetLevelStringsWithRarity(configMoons.Value.ToLowerInvariant(), configBaseRarity.Value, configGuaranteed.Value ? 99999 : -1);
            foreach (StringWithRarity level in levels)
            {
                string levelNameLower = level.Name.ToLowerInvariant();
                if (levelNameLower == "all")
                {
                    myExtendedDungeonFlow.dynamicLevelTagsList.Add(new StringWithRarity("Vanilla", level.Rarity));
                    myExtendedDungeonFlow.dynamicLevelTagsList.Add(new StringWithRarity("Custom", level.Rarity));
                    mls.LogInfo($"Added all moons + custom with a rarity weight of {level.Rarity}");
                }
                else if (levelNameLower == "custom" || levelNameLower == "modded")
                {
                    myExtendedDungeonFlow.dynamicLevelTagsList.Add(new StringWithRarity("Custom", level.Rarity));
                    mls.LogInfo($"Added all custom moons with a rarity weight of {level.Rarity}");
                }
                else if (levelNameLower == "vanilla")
                {
                    myExtendedDungeonFlow.dynamicLevelTagsList.Add(new StringWithRarity("Vanilla", level.Rarity));
                    mls.LogInfo($"Added all vanilla moons with a rarity weight of {level.Rarity}");
                }
                else
                {
                    myExtendedDungeonFlow.manualPlanetNameReferenceList.Add(level);
                    mls.LogInfo($"Added to moon '{level.Name}' with a rarity weight of {level.Rarity}");
                }
            }

            // Make the pool lights red when apparatus taken
            myExtendedDungeonFlow.dungeonEvents.onApparatusTaken.AddListener(OnDungeonApparatusTaken);

            PatchedContent.RegisterExtendedDungeonFlow(myExtendedDungeonFlow);

            if (configEnableCustomScrap.Value)
            {
                // Register our special dungeon items
                Item LifeBuoyItem = DungeonAssets.LoadAsset<Item>("Assets/PoolRooms/Scrap/LifeBuoy.asset");
                LethalLib.Modules.Utilities.FixMixerGroups(LifeBuoyItem.spawnPrefab);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(LifeBuoyItem.spawnPrefab);
                PoolItems.Add(LifeBuoyItem);
                PoolItemRarities.Add(configLifeBuoyWeighting.Value);
                LethalLib.Modules.Items.RegisterScrap(LifeBuoyItem, configLifeBuoyWeighting.Value, configUseCustomScrapGlobally.Value ? LevelTypes.All : LevelTypes.None);

                Item PoolNetItem = DungeonAssets.LoadAsset<Item>("Assets/PoolRooms/Scrap/PoolNet.asset");
                LethalLib.Modules.Utilities.FixMixerGroups(PoolNetItem.spawnPrefab);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(PoolNetItem.spawnPrefab);
                PoolItems.Add(PoolNetItem);
                PoolItemRarities.Add(configPoolNetWeighting.Value);
                LethalLib.Modules.Items.RegisterScrap(PoolNetItem, configPoolNetWeighting.Value, configUseCustomScrapGlobally.Value ? LevelTypes.All : LevelTypes.None);

                Item PoolBallItem = DungeonAssets.LoadAsset<Item>("Assets/PoolRooms/Scrap/PoolBall.asset");
                LethalLib.Modules.Utilities.FixMixerGroups(PoolBallItem.spawnPrefab);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(PoolBallItem.spawnPrefab);
                PoolItems.Add(PoolBallItem);
                PoolItemRarities.Add(configPoolBallWeighting.Value);
                LethalLib.Modules.Items.RegisterScrap(PoolBallItem, configPoolBallWeighting.Value, configUseCustomScrapGlobally.Value ? LevelTypes.All : LevelTypes.None);

                Item WetFloorSignItem = DungeonAssets.LoadAsset<Item>("Assets/PoolRooms/Scrap/WetFloorSign.asset");
                LethalLib.Modules.Utilities.FixMixerGroups(WetFloorSignItem.spawnPrefab);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(WetFloorSignItem.spawnPrefab);
                PoolItems.Add(WetFloorSignItem);
                PoolItemRarities.Add(configWetFloorSignWeighting.Value);
                LethalLib.Modules.Items.RegisterScrap(WetFloorSignItem, configWetFloorSignWeighting.Value, configUseCustomScrapGlobally.Value ? LevelTypes.All : LevelTypes.None);
            }

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

        private void OnDungeonApparatusTaken(LungProp lung)
        {
            PoolLightBehaviour[] poolLights = FindObjectsOfType<PoolLightBehaviour>();
            foreach (PoolLightBehaviour light in poolLights)
            {
                light.OnApparatusPulled();
            }
        }

        // Converts a string 'vow:100,march:50,paid:100' to a list of maps with rarity weight
        private List<StringWithRarity> GetLevelStringsWithRarity(string delimitedList, int baseRarity, int fixedRarity)
        {
            List<StringWithRarity> listOut = new List<StringWithRarity>();
            Dictionary<string, int> lookup = new Dictionary<string, int>();

            void AddRarity(string mapLevelID, int rarity)
            {
                int elmIndex = lookup.Get(mapLevelID, -1);
                if (elmIndex == -1)
                {
                    listOut.Add(new StringWithRarity(mapLevelID, rarity));
                    lookup.Add(mapLevelID, listOut.Count - 1);
                }
                else
                {
                    listOut[elmIndex].Rarity = rarity;
                }
            }

            string[] names = delimitedList.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string name in names)
            {
                string[] nameAndRarityStr = name.Split(':');
                try
                {
                    string levelID = nameAndRarityStr[0].Trim();
                    int rarityWeight = baseRarity;
                    if (fixedRarity > 0)
                    {
                        rarityWeight = fixedRarity;
                    }
                    else if (nameAndRarityStr.Length >= 2)
                    {
                        rarityWeight = int.Parse(nameAndRarityStr[1]);
                    }

                    string[] levelsToAdd = MoonIDToMoonsMapping.Get(levelID);
                    if (levelsToAdd != null)
                    {
                        foreach (string mapLevelID in levelsToAdd)
                        {
                            AddRarity(mapLevelID, rarityWeight);
                        }
                    }
                    else
                    {
                        // Allow adding custom moons, they need to have the ID right for it to work I suspect...
                        AddRarity(levelID, rarityWeight);
                    }
                }
                finally { }
            }

            return listOut;
        }

        // Patch to update our dummy objects (entrances, vents, turrets, mines, scrap, storage shelving) with the real prefab references
        [HarmonyPatch(typeof(RoundManager))]
        internal class RoundManagerPatch
        {
            // Just before spawning the scrap (the level is ready at this point) fix up our referenes to the item groups
            [HarmonyPatch("SpawnScrapInLevel")]
            [HarmonyPrefix]
            static void SpawnScrapInLevelPre(ref SelectableLevel ___currentLevel, ref RuntimeDungeon ___dungeonGenerator)
            {
                if (___dungeonGenerator.Generator.DungeonFlow.name != "PoolRoomsFlow")
                {
                    return;
                }

                StartOfRound playersManager = FindObjectOfType<StartOfRound>();

                Item BottlesItem = null;
                Item GoldenCupItem = null;
                foreach (Item item in playersManager.allItemsList.itemsList)
                {
                    if (item.itemName == "Bottles")
                    {
                        BottlesItem = item;
                    }
                    else if (item.itemName == "Golden cup")
                    {
                        GoldenCupItem = item;
                    }
                    if (BottlesItem && GoldenCupItem)
                    {
                        break;
                    }
                }
                if (BottlesItem == null)
                {
                    Instance.mls.LogError("Unable to find the Bottles item with spawn positions to pull from. No junk will spawn in this dungeon!");
                    return;
                }
                if (GoldenCupItem == null)
                {
                    Instance.mls.LogWarning("Unable to find the GoldenCup item with spawn positions to pull from. No small items will be able to spawn!");
                }

                // Grab the item groups
                ItemGroup itemGroupGeneral = BottlesItem.spawnPositionTypes.Find(x => x.name == "GeneralItemClass");
                ItemGroup itemGroupTabletop = BottlesItem.spawnPositionTypes.Find(x => x.name == "TabletopItems");
                if (!itemGroupGeneral || !itemGroupTabletop)
                {
                    Instance.mls.LogError($"Found an item '{BottlesItem.name}' that is suppose to have both general and table top items but no longer does...");
                    return;
                }

                // Grab the small item group from the fancy glass. It is the only item that uses it and if it isn't used will default to table top items which is similar.
                ItemGroup itemGroupSmall = (GoldenCupItem == null) ? itemGroupTabletop : GoldenCupItem.spawnPositionTypes.Find(x => x.name == "SmallItems");

                if (Instance.configEnableCustomScrap.Value && !Instance.configUseCustomScrapGlobally.Value)
                {
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
                        if (spawnPositionTypes.Count > 0)
                        {
                            Instance.mls.LogInfo($"Fixing pool item '{itemToAdd.name}' item groups");
                            itemToAdd.spawnPositionTypes = spawnPositionTypes;
                        }

                        SpawnableItemWithRarity itemRarity = new SpawnableItemWithRarity();
                        itemRarity.spawnableItem = itemToAdd;
                        itemRarity.rarity = PoolItemRarities[rarityIndex];
                        ___currentLevel.spawnableScrap.Add(itemRarity);
                        PoolItemsAdded.Add(itemRarity);
                        Instance.mls.LogInfo($"Added pool rooms item '{itemToAdd.name}' to spawnable scrap!");
                        ++rarityIndex;
                    }
                }

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

                if (Instance.configEnableCustomScrap.Value && !Instance.configUseCustomScrapGlobally.Value)
                {
                    foreach (SpawnableItemWithRarity item in PoolItemsAdded)
                    {
                        if (___currentLevel.spawnableScrap.Remove(item))
                        {
                            Instance.mls.LogInfo($"Removed pool rooms item '{item.spawnableItem.name}' from spawnable scrap!");
                        }
                        else
                        {
                            Instance.mls.LogError($"Unable to remove pool rooms item '{item.spawnableItem.name}' from spawnable scrap!");
                        }
                    }

                    PoolItemsAdded.Clear();
                }
            }

            /*[HarmonyPatch("SyncScrapValuesClientRpc")]
            [HarmonyPrefix]
            static void SyncScrapValuesClientRpc(NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
            {
                Vector3 mainEntranceLocation = RoundManager.FindMainEntrancePosition();

                for (int i = 0; i < spawnedScrap.Length; i++)
                {
                    if (spawnedScrap[i].TryGet(out var networkObject))
                    {
                        Instance.mls.LogInfo($"Added scrap item '{networkObject.gameObject.name}' {Vector3.Distance(mainEntranceLocation, networkObject.gameObject.transform.position)} Meters from the entrance");
                    }
                }
            }*/
        }

        [HarmonyPatch(typeof(EntranceTeleport))]
        internal class EntranceTeleportPatch
        {
            // Called on the local client
            [HarmonyPatch("TeleportPlayer")]
            [HarmonyPrefix]
            static void TeleportPlayerPre()
            {
                Instance.mls.LogInfo("Local Player Teleporting! Broadcasting!");
                BroadcastPlayerTeleported(GameNetworkManager.Instance.localPlayerController);
            }

            // Called on the client from the server
            [HarmonyPatch("TeleportPlayerClientRpc")]
            [HarmonyPrefix]
            static void TeleportPlayerClientPre(int playerObj)
            {
                Instance.mls.LogInfo($"Client Player '{playerObj}' Teleporting...");
                StartOfRound playersManager = FindObjectOfType<StartOfRound>();

                if (playersManager && playerObj >= 0 && playerObj < playersManager.allPlayerScripts.Length)
                {
                    Instance.mls.LogInfo("Found Client Player! Broadcasting!");
                    BroadcastPlayerTeleported(playersManager.allPlayerScripts[playerObj]);
                }
                else
                {
                    Instance.mls.LogWarning("Unable to find the client player script!");
                }
            }

            static void BroadcastPlayerTeleported(PlayerControllerB player)
            {
                // When the player is about to teleport let the water triggers know they are moving far away and should be removed from their lists
                // Zeekers does this as well with Quicksand/Water so players don't teleport into the facility drowning.
                PoolRoomsWaterTrigger[] waterTriggers = FindObjectsOfType<PoolRoomsWaterTrigger>();
                foreach (PoolRoomsWaterTrigger trigger in waterTriggers)
                {
                    if (trigger != null)
                    {
                        trigger.OnLocalPlayerTeleported(player);
                    }
                }
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
