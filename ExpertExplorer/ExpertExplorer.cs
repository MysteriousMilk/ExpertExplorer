using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Reflection;
using UnityEngine;
using Jotunn.Configs;
using BepInEx.Configuration;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using static ZoneSystem;
using static MessageHud;
using Jotunn;

namespace ExpertExplorer
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class ExpertExplorer : BaseUnityPlugin
    {
        public const string PluginGUID = "com.milkwyzard.ExpertExplorer";
        public const string PluginName = "ExpertExplorer";
        public const string PluginVersion = "1.2";
        public const string SkillId = $"{PluginGUID}.Exploration";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private Harmony harmony;

        private const float POST_INTRO_DELAY = 1.0f;        // Delay after the intro before the mod starts checking for locations
        private const float ZONE_CHECK_FREQUENCY = 1.5f;    // Frequency at which the zone is checked
        private const float ZONE_RPC_REQUEST_FREQUENCY = 2.0f;

        private static ZoneData currentZoneData = null;
        private static Vector2i currentZone;
        private static string lastEvaluatedLocation = string.Empty;
        private static float zoneCheckTimer = 0.0f;
        private static float zoneRpcRequestTimer = 0.0f;
        private static bool introLastFrame = false;
        private static bool locationsAvailable = false;

        private static List<Chat.WorldTextInstance> discoverTexts = new List<Chat.WorldTextInstance>();
        private static Dictionary<Vector2i, ZoneData> zoneDataCache = new Dictionary<Vector2i, ZoneData>();
        private static Dictionary<Vector2i, Sprite> locationSpriteMap = new Dictionary<Vector2i, Sprite>();
        private static Dictionary<Vector2i, Minimap.PinData> locationPins = new Dictionary<Vector2i, Minimap.PinData>();

        private static List<string> specialLocations = new List<string>()
        {
            "StartTemple",
            "Eikthyrnir",
            "Dragonqueen",
            "GoblinKing",
            "GDKing",
            "Bonemass",
            "Vendor_BlackForest",
            "Hildir_camp",
            "Mistlands_DvergrBossEntrance1"
        };

        #region Config Variables
        /// <summary>
        /// Factor applied to skill gain. Higher number means faster skill gain.
        /// </summary>
        public static ConfigEntry<float> SkillXpFactor;

        /// <summary>
        /// Max explore radius used when the Exploration Skill is at 100.
        /// </summary>
        public static ConfigEntry<float> MaxExploreRadius;

        /// <summary>
        /// Distance between the player and the bounds of a location required to mark the location as discovered.
        /// </summary>
        public static ConfigEntry<float> DiscoverDistance;

        /// <summary>
        /// Key used to pin a location to the minimap.
        /// </summary>
        public static ConfigEntry<KeyboardShortcut> PinKey;
        #endregion

        #region Public Variables
        /// <summary>
        /// Skill type identifier for the exploration skill. Used to retreive the exploration skill
        /// from the Valheim Skill registry.
        /// </summary>
        public static Skills.SkillType ExplorationSkillType;
        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ExpertExplorer()
        {
            harmony = new Harmony(PluginGUID);
        }

        public void Start()
        {
            // patch this dll
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void Awake()
        {
            ConfigurationManagerAttributes isAdminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

            // Load config
            SkillXpFactor = Config.Bind("General", "SkillXpFactor", 1.5f, new ConfigDescription("Factor applied to skill gain. Higher number means faster skill gain. Range 0-100.", new AcceptableValueRange<float>(0f, 100f), isAdminOnly));
            MaxExploreRadius = Config.Bind("Exploration", "MaxExploreRadius", 200f, new ConfigDescription("Max explore radius used when the Exploration Skill is at 100. Range 100-300.", new AcceptableValueRange<float>(100f, 300f), isAdminOnly));
            DiscoverDistance = Config.Bind("Exploration", "DiscoverDistance", 10f, new ConfigDescription("Distance between the player and the bounds of a location required to mark the location as discovered. Range 0-50.", new AcceptableValueRange<float>(0f, 50f), isAdminOnly));
            PinKey = Config.Bind("Hotkeys", "Pin to Mini-Map Key", new KeyboardShortcut(KeyCode.P), "Hotkey used to add a pin to the mini-map when a new location is discovered.");

            AddLocalizations();

            ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationAvailable;

            // Add the Exploration Skill
            SkillConfig explorerSkill = new SkillConfig();
            explorerSkill.Identifier = SkillId;
            explorerSkill.Name = "$skill_exploration";
            explorerSkill.Description = "$skill_exploration_desc";
            explorerSkill.IncreaseStep = SkillXpFactor.Value;
            ExplorationSkillType = SkillManager.Instance.AddSkill(explorerSkill);

            //ZoneHelper.Instance.RegisterRPC();
            ZoneHelper.Instance.SetZoneDataAction = (zoneData) => SetZoneData(zoneData);

            Jotunn.Logger.LogInfo($"ExpertExplorer v{PluginVersion} loaded and patched.");
        }

        private void AddLocalizations()
        {
            // Add translations for our custom skill
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                { "skill_exploration", "Exploration" },
                { "skill_exploration_desc", "Discovering new areas in the world will improve your sight range." },
                { "location_StartTemple", "Sacrificial Stones" },
                { "location_Eikthyrnir", "Alter of Eikthyr, the Forsaken" },
                { "location_Dragonqueen", "The Mount of Moder" },
                { "location_Hildir_cave", "Howling Caverns" },
                { "location_Hildir_crypt", "Smouldering Tombs" },
                { "location_Hildir_plainsfortress", "The Sealed Tower" },
                { "location_GoblinKing", "Stones of Yagluth, the Forsaken" },
                { "location_GDKing", "Temple of the Elder, the Forsaken" },
                { "location_Bonemass", "Grave of Bonemass, the Forsaken" },
                { "location_Vendor_BlackForest", "Haldor, the Trader" },
                { "location_Hildir_camp", "Hildir's Camp" },
                { "location_WoodHouse1", "Old Wooden Hut" },
                { "location_WoodHouse2", "Abandoned Wooden Hut" },
                { "location_WoodHouse3", "Abandoned Camp" },
                { "location_WoodHouse4", "Wooden Ruins" },
                { "location_WoodHouse5", "Old Wooden House" },
                { "location_WoodHouse6", "Old Wooden Tower" },
                { "location_WoodHouse7", "Rundown Wooden Hut" },
                { "location_WoodHouse8", "Abandoned Farm" },
                { "location_WoodHouse9", "Old Hut" },
                { "location_WoodHouse10", "Ruined Wooden Hut" },
                { "location_WoodHouse11", "Old Wood House" },
                { "location_WoodHouse12", "Old Wood Tower" },
                { "location_WoodHouse13", "Ruined Wood House" },
                { "location_StoneCircle", "Circle of Stones" },
                { "location_SwampWell1", "Swamp Well" },
                { "location_MountainWell1", "Mountain Well" },
                { "location_Dolmen03", "Tomb" },
                { "location_Waymarker01", "Waymarker" },
                { "location_Waymarker02", "Waymarker" },
                { "location_Dolmen01", "Dolmen" },
                { "location_Dolmen02", "Dolmen" },
                { "location_ShipSetting01", "Ship Rocks" },
                { "location_MountainGrave01", "Mountain Grave" },
                { "location_Grave1", "Grave Stones" },
                { "location_Crypt2", "Burial Chambers" },
                { "location_Ruin1", "Forest Ruins" },
                { "location_Ruin2", "Forest Tower" },
                { "location_StoneHouse3", "Dilapidated Stone Hut" },
                { "location_StoneHouse4", "Ruined Stone Hut" },
                { "location_GoblinCamp2", "Fuling Camp" },
                { "location_TrollCave02", "Forest Cave" },
                { "location_Crypt3", "Burial Chambers" },
                { "location_Crypt4", "Burial Chambers" },
                { "location_DrakeNest01", "Drake Nest" },
                { "location_Mistlands_RockSpire1", "Rock Spire" },
                { "location_Mistlands_StatueGroup1", "Ancient Pillars" },
                { "location_Mistlands_Statue1", "Ruined Pillar" },
                { "location_Mistlands_Statue2", "Ruined Pillar" },
                { "location_Greydwarf_camp1", "Greydwarf Camp" },
                { "location_Mistlands_RoadPost1", "Dvergr Road Post" },
                { "location_InfestedTree01", "Infested Tree" },
                { "location_AbandonedLogCabin02", "Ruined Cabin" },
                { "location_AbandonedLogCabin03", "Abandoned Cabin" },
                { "location_AbandonedLogCabin04", "Old Mountain Hut" },
                { "location_Mistlands_GuardTower1_ruined_new2", "Ruined Guard Tower" },
                { "location_Mistlands_GuardTower3_new", "Dvergr Guard Tower" },
                { "location_Mistlands_GuardTower3_ruined_new", "Ruined Dvergr Guard Tower" },
                { "location_Mistlands_GuardTower1_new", "Dvergr Guard Tower" },
                { "location_Mistlands_GuardTower2_new", "Dvergr Guard Tower" },
                { "location_Mistlands_GuardTower1_ruined_new", "Ruined Dvergr Guard Tower" },
                { "location_Mistlands_Lighthouse1_new", "Dvergr Lighthouse" },
                { "location_Mistlands_DvergrBossEntrance1", "Lair of the Queen" },
                { "location_Mistlands_DvergrTownEntrance1", "Ruined Dvergr Mines" },
                { "location_Mistlands_DvergrTownEntrance2", "Infested Mines" },
                { "location_Mistlands_Excavation1", "Excavation Site" },
                { "location_Mistlands_Excavation2", "Excavation Site" },
                { "location_Mistlands_Excavation3", "Excavation Site" },
                { "location_FireHole", "Surtling Geyser" },
                { "location_Mistlands_Giant2", "Acient Jotun Remains" },
                { "location_Mistlands_Giant1", "Fallen Jotun" },
                { "location_Mistlands_Swords1", "Ancient Jotun Greatsword" },
                { "location_Mistlands_Swords2", "Ancient Jotun Equipment" },
                { "location_Mistlands_Swords3", "Foregone Jotun Greatsword" },
                { "location_Ruin3", "Primordial Stone Ruins" },
                { "location_StoneTower1", "Fuling Stone Ruins" },
                { "location_StoneTower3", "Fuling Stone Tower" },
                { "location_Mistlands_Harbour1", "Dvergr Harbour" },
                { "location_Mistlands_Viaduct1", "Dvergr Bridge" },
                { "location_Mistlands_Viaduct2", "Ruined Dvergr Bridge" },
                { "location_MountainCave02", "Mountain Cave" },
                { "location_StoneTowerRuins04", "Ancient Stone Tower" },
                { "location_StoneTowerRuins05", "Ancient Mountain Fortress" },
                { "location_Runestone_Greydwarfs", "Greydwarf Runestone" },
                { "location_Runestone_Draugr", "Draugr Runestone" },
                { "location_DrakeLorestone", "Drake Runestone" },
                { "location_Runestone_Boars", "Boar Runestone" },
                { "location_Runestone_BlackForest", "Black Forest Runestone" },
                { "location_Runestone_Mistlands", "Mistlands Runestone" },
                { "location_Runestone_Meadows", "Meadows Runestone" },
                { "location_Runestone_Swamps", "Swamp Runestone" },
                { "location_Runestone_Mountains", "Mountain Runestone" },
                { "location_Runestone_Plains", "Plains Runestone" },
                { "location_ShipWreck01", "Shipwreck" },
                { "location_ShipWreck02", "Capsized Ship" },
                { "location_ShipWreck03", "Capsized Ship" },
                { "location_ShipWreck04", "Shipwreck" },
                { "location_StoneHenge1", "Stonehenge" },
                { "location_StoneHenge2", "Ancient Stones" },
                { "location_StoneHenge3", "Waystones" },
                { "location_StoneHenge4", "Burial Stones" },
                { "location_StoneHenge5", "Stone Waymarker" },
                { "location_StoneHenge6", "Ancient Stone Burial Site" },
                { "location_StoneTowerRuins03", "Ruined Stone Tower" },
                { "location_StoneTowerRuins07", "Black Forest Tower" },
                { "location_StoneTowerRuins08", "Ancient Stone Tower" },
                { "location_StoneTowerRuins09", "Stone Tower" },
                { "location_StoneTowerRuins10", "Old Stone Tower" },
                { "location_SunkenCrypt4", "Sunken Crypt" },
                { "location_SwampHut5", "Swamp Ruins" },
                { "location_SwampHut1", "Haunted Swamp Hut" },
                { "location_SwampHut2", "Primeval Swamp Hut" },
                { "location_SwampHut3", "Haunted Swamp Dock" },
                { "location_SwampHut4", "Draugr Dock" },
                { "location_SwampRuin1", "Ruined Draugr Tower" },
                { "location_SwampRuin2", "Ancient Draugr Tower" },
                { "location_TarPit1", "Tar Pit" },
                { "location_TarPit2", "Tar Pit" },
                { "location_TarPit3", "Tar Pit" },
                { "location_WoodFarm1", "Abandoned Farm" },
                { "location_WoodVillage1", "Abandoned Village" },
                { "location_CharredFortress", "Charred Fortress" },
                { "location_CharredStone_Spawner", "Charred Stones" },
                { "location_CharredTowerRuins2", "Charred Tower Ruins" },
                { "location_CharredTowerRuins3", "Grausten Tower Ruins" },
                { "location_FortressRuins", "Fortress Ruins" },
                { "location_LeviathanLava", "Flametal Vein" },
                { "location_SulfurArch", "Sulfur Arch" },
                { "location_AshlandRuins", "Ash Ruins" },
                { "location_CharredRuins2", "Ruined Charred Fortress" },
                { "location_CharredRuins3", "Ruined Charred Tower" },
                { "location_CharredRuins4", "Charred Fortress Ruins" },
                { "location_VoltureNest", "Volture Nest" },
                { "location_FaderLocation", "Fortress of Fader, the Forsaken" },
                { "location_MorgenHole1", "Morgen Hole" },
                { "location_MorgenHole2", "Morgen Hole" },
                { "location_MorgenHole3", "Morgen Hole" },
                { "location_PlaceofMystery1", "Mysterious Ruins" },
                { "location_PlaceofMystery2", "Mysterious Ruins" },
                { "location_PlaceofMystery3", "Mysterious Ruins" },
                { "location_Runestone_Ashlands", "Ashlands Runestone" },
                { "location_CharredTowerRuins1", "Charred Ruins" },
                { "location_CharredTowerRuins1_dvergr", "Charred Ruins" },
                { "location_CharredRuins1", "Ruined Charred Fortress" }
            });
        }

        private void OnVanillaLocationAvailable()
        {
            locationsAvailable = true;
        }

        // Called every frame
        private void Update()
        {
            if (ZInput.instance == null)
                return;

            if (!locationsAvailable)
                return;

            if (Player.m_localPlayer == null)
                return;

            if (Player.m_localPlayer.InIntro())
            {
                introLastFrame = true;
                return;
            }

            if (Player.m_localPlayer.InCutscene())
                return;

            var playerPos = Player.m_localPlayer.transform.position;
            var explorationData = Player.m_localPlayer.ExplorationData();

            if (explorationData == null)
            {
                Jotunn.Logger.LogWarning("Update skipped because there is no exploration data.");
                return;
            }

            // If we just exited the intro sequence, add a slight delay before checking locations.
            // This will issure that we don't immediately "discover" the sacrificial stones as the player
            // is "dropping in".
            if (introLastFrame)
            {
                zoneCheckTimer = POST_INTRO_DELAY;
                introLastFrame = false;
            }

            // reduce the timer this frame but don't let it fall below 0
            zoneCheckTimer = Mathf.Max(0.0f, zoneCheckTimer - Time.deltaTime);
            zoneRpcRequestTimer = Mathf.Max(0.0f, zoneRpcRequestTimer - Time.deltaTime);

            if (!Player.m_localPlayer.InInterior())
            {
                // check to see if the timer has elapsed
                if (zoneCheckTimer == 0.0f)
                {
                    Vector2i zone = ZoneSystem.instance.GetZone(playerPos);

                    if (zone != currentZone)
                    {
#if DEBUG
                        Jotunn.Logger.LogInfo($"Entering zone ({zone.x}, {zone.y})");
#endif

                        currentZone = zone;
                        zoneDataCache.TryGetValue(currentZone, out currentZoneData);
                    }

                    if (currentZoneData == null && zoneRpcRequestTimer == 0f)
                    {
                        zoneRpcRequestTimer = ZONE_RPC_REQUEST_FREQUENCY;
                        ZoneHelper.Instance.Client_RequestZoneData(currentZone);
                    }

                    zoneCheckTimer = ZONE_CHECK_FREQUENCY;
                }
            }

            if (currentZoneData != null && currentZoneData.IsValid() && !explorationData.IsZoneLocationAlreadyDiscovered(currentZone))
            {
                if (IsLookingAtLocation(Player.m_localPlayer, currentZoneData) ||
                    IsInsideLocation(Player.m_localPlayer, currentZoneData))
                {
                    if (currentZoneData.LocationPrefab != lastEvaluatedLocation)
                    {
                        // location discovered
                        lastEvaluatedLocation = currentZoneData.LocationPrefab;
                        explorationData.FlagAsDiscovered(currentZoneData);
                        explorationData.Save(Player.m_localPlayer);
                        Player.m_localPlayer.RaiseSkill(ExplorationSkillType);

                        string text = $"Discovered {currentZoneData.LocalizedLocationName}";

                        // Set the position of the notification text. Here we find the point on the terrain at the center of the location
                        // prefab. The text (in the world) can start at player height above the terrain.
                        Vector3 textPos = currentZoneData.LocationPosition;
                        textPos.y = ZoneSystem.instance.GetGroundHeight(currentZoneData.LocationPosition) + Player.m_localPlayer.GetHeight();

                        Sprite icon = null;
                        locationSpriteMap.TryGetValue(currentZone, out icon);

                        if (icon == null)
                            Jotunn.Logger.LogInfo($"No sprite icon for location {currentZoneData.LocationPrefab}");

                        QueueFoundLocationMsg(icon, "Location Discovered", currentZoneData.LocalizedLocationName, IsSpecialLocation(currentZoneData.LocationPrefab));
                    }
                }
            }

            // check for key input
            if (Player.m_localPlayer.TakeInput())
            {
                if (ZInput.GetKeyDown(PinKey.Value.MainKey))
                {
                    if (IsLookingAtLocation(Player.m_localPlayer, currentZoneData) ||
                        IsInsideLocation(Player.m_localPlayer, currentZoneData))
                    {
                        Vector3 pinPos = currentZoneData.LocationPosition;
                        string pinName = currentZoneData.LocalizedLocationName;

                        if (!locationPins.ContainsKey(currentZone) &&
                            !IsSpecialLocation(currentZoneData.LocationPrefab))
                        {
                            var pinData = Minimap.instance.AddPin(pinPos, Minimap.PinType.Icon3, pinName, true, false, 0L);
                            locationPins[currentZone] = pinData;
                            explorationData.FlagAsPinned(currentZone, pinData);
                            Player.m_localPlayer.Message(MessageType.TopLeft, $"Location pinned to minimap.");

#if DEBUG
                            Jotunn.Logger.LogInfo("Pin Added.");
#endif
                        }
                    }
                }
            }

            UpdateDiscoverTexts(Time.deltaTime);
        }

        /// <summary>
        /// Gets the current location that the player is standing in or looking at.
        /// If the player is not in (or looking at) a location, the value retuned is null.
        /// </summary>
        /// <param name="player">The player to get the location for.</param>
        /// <returns>The location that the player is in.</returns>
        public static ZoneData GetCurrentLocation(Player player)
        {
            if (currentZoneData != null && currentZoneData.IsValid())
            {
                if (player.InInterior() ||
                    IsLookingAtLocation(player, currentZoneData) ||
                    IsInsideLocation(player, currentZoneData))
                {
                    return currentZoneData;
                }
            }
            return null;
        }

        /// <summary>
        /// Check to see if a location is considered a "special" location. Aka, boss spawn or starting area, etc.
        /// </summary>
        /// <param name="locationPrefabName">Name of the location prefab.</param>
        /// <returns>True if the location is considered "special", False if not.</returns>
        public static bool IsSpecialLocation(string locationPrefabName)
        {
            return specialLocations.Contains(locationPrefabName);
        }

        /// <summary>
        /// Called when a pin is removed from the minimap. This will check the removed pin was the 
        /// pinned location from this zone. If so, update our list of tracked pins.
        /// </summary>
        /// <param name="pinData">The PinData of the pin that was just removed.</param>
        public static void OnPinRemoved(Minimap.PinData pinData)
        {
#if DEBUG
            Jotunn.Logger.LogInfo("Pin Removed.");
#endif

            var zone = ZoneSystem.instance.GetZone(pinData.m_pos);

            if (locationPins.ContainsKey(zone) &&
                locationPins[zone].m_name == pinData.m_name)
            {
                locationPins.Remove(zone);
            }
        }

        /// <summary>
        /// Called when the server responses with the data for the current zone.
        /// This cache off the data for this zone and render an icon for the location.
        /// </summary>
        /// <param name="zoneData">The data for the current zone.</param>
        private static void SetZoneData(ZoneData zoneData)
        {
            if (currentZone == zoneData.ZoneId)
            {
                // Update the client-side record of the zone information
                zoneDataCache[currentZone] = zoneData;
                currentZoneData = zoneData;

                if (!locationSpriteMap.ContainsKey(currentZone))
                {
                    var zoneLocation = ZoneSystem.instance.m_locations.FirstOrDefault(l => l.m_prefabName == zoneData.LocationPrefab);
                    if (zoneLocation != null)
                    {
                        GameObject exteriorPrefab = zoneLocation.m_prefab.Asset;
                        Location location = exteriorPrefab.GetComponent<Location>();

                        if (location && location.m_hasInterior)
                        {
                            var locationTransform = location.transform;
                            for (int i = 0; i < locationTransform.childCount; i++)
                            {
                                var childTransform = locationTransform.GetChild(i);
                                if (childTransform.name.ToLowerInvariant().Equals("exterior"))
                                {
                                    exteriorPrefab = childTransform.gameObject;
                                    break;
                                }
                            }
                        }

                        var sprite = RenderManager.Instance.Render(new RenderManager.RenderRequest(exteriorPrefab)
                        {
                            Rotation = RenderManager.IsometricRotation,
                            FieldOfView = 20f,
                            DistanceMultiplier = 1.1f,
                            Width = 256,
                            Height = 256
                        });

                        if (sprite != null)
                            locationSpriteMap[currentZone] = sprite;
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the player is looking at a specific location.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="location">The location to check.</param>
        /// <returns>True if the player is looking at the given location, False if not.</returns>
        private static bool IsLookingAtLocation(Player player, ZoneData zoneData)
        {
            float locationRadius = zoneData.LocationRadiusMax;
            var playerPos = player.transform.position;

            float totalDistance = locationRadius + DiscoverDistance.Value;
            bool lookingAt = false;

            // check to see if the player is in near the location and "looking" at it
            if (Vector3.Distance(playerPos, currentZoneData.LocationPosition) < totalDistance)
            {
                // make sure the player is looking in that direction
                Vector3 delta = currentZoneData.LocationPosition - playerPos;
                float angle = Vector3.Angle(delta, Player.m_localPlayer.transform.forward);

                if (angle < 20.0f)
                    lookingAt = true;
            }

            return lookingAt;
        }

        /// <summary>
        /// Checks to see if the player is standing inside a location.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="location">The location to check.</param>
        /// <returns>True if the player is in the given location, false if not.</returns>
        private static bool IsInsideLocation(Player player, ZoneData zoneData)
        {
            return Utils.DistanceXZ(player.transform.position, currentZoneData.LocationPosition) < zoneData.LocationRadiusMax;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="topic"></param>
        /// <param name="description"></param>
        public void QueueFoundLocationMsg(Sprite icon, string topic, string description, bool isSpecialLocation)
        {
            string desc = description;
            if (!isSpecialLocation)
                desc += "\nPress [<color=yellow><b>P</b></color>] to add a pin";

            UnlockMsg unlockMsg = new UnlockMsg();
            unlockMsg.m_icon = icon;
            unlockMsg.m_topic = topic;
            unlockMsg.m_description = desc;
            MessageHud.instance.m_unlockMsgQueue.Enqueue(unlockMsg);
            MessageHud.instance.AddLog(topic + ": " + description);
        }

        private void AddDiscoverTextToWorld(string text, Vector3 pos)
        {
            Chat.WorldTextInstance worldTextInstance = new Chat.WorldTextInstance();
            worldTextInstance.m_gui = UnityEngine.Object.Instantiate(Chat.instance.m_worldTextBase, Chat.instance.transform);
            worldTextInstance.m_gui.gameObject.SetActive(value: true);
            worldTextInstance.m_text = text;
            worldTextInstance.m_position = pos;
            worldTextInstance.m_go = Player.m_localPlayer.gameObject;
            Transform transform = worldTextInstance.m_gui.transform.Find("Text");
            worldTextInstance.m_textMeshField = transform.GetComponent<TextMeshProUGUI>();
            discoverTexts.Add(worldTextInstance);

            worldTextInstance.m_textMeshField.color = Color.cyan;
            worldTextInstance.m_textMeshField.fontSize = 20;
            worldTextInstance.m_textMeshField.text = text;
            worldTextInstance.m_timer = 0f;
        }

        private void UpdateDiscoverTexts(float dt)
        {
            Chat.WorldTextInstance worldTextInstance = null;
            Camera mainCamera = Utils.GetMainCamera();
            float textDuration = Chat.instance.m_worldTextTTL;

            foreach (var discoverText in discoverTexts)
            {
                // update timer
                discoverText.m_timer += dt;
                if (discoverText.m_timer > textDuration && worldTextInstance == null)
                    worldTextInstance = discoverText;

                var playerPos = Player.m_localPlayer.GetTopPoint();

                // update text position
                discoverText.m_position.y += dt * 0.5f;

                // update color (fade out)
                float f = Mathf.Clamp01(discoverText.m_timer / textDuration);
                Color color = discoverText.m_textMeshField.color;
                color.a = 1f - Mathf.Pow(f, 3f);
                discoverText.m_textMeshField.color = color;

                Vector3 position = mainCamera.WorldToScreenPointScaled(discoverText.m_position);
                if (position.x < 0f ||
                    position.x > Screen.width ||
                    position.y < 0f ||
                    position.y > Screen.height ||
                    position.z < 0f)
                {
                    discoverText.m_gui.SetActive(value: false);
                    continue;
                }

                discoverText.m_gui.SetActive(value: true);
                discoverText.m_gui.transform.position = position;
            }

            if (worldTextInstance != null)
            {
                UnityEngine.Object.Destroy(worldTextInstance.m_gui);
                discoverTexts.Remove(worldTextInstance);
            }
        }
    }
}

