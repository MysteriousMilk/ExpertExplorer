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
using System.Linq;
using static MessageHud;
using System.Collections;
using System.IO;
using System;
using BepInEx.Bootstrap;

namespace ExpertExplorer
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class ExpertExplorer : BaseUnityPlugin
    {
        public const string PluginGUID = "com.milkwyzard.ExpertExplorer";
        public const string PluginName = "ExpertExplorer";
        public const string PluginVersion = "1.4.7";
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
            "Mistlands_DvergrBossEntrance1",
            "MWL_PlainsTavern1",                // Extra vendor location from More World Locations (MWL) mod(s)
            "MWL_PlainsCamp1",                  // Extra vendor location from More World Locations (MWL) mod(s)
            "MWL_BlackForestBlacksmith1",       // Extra vendor location from More World Locations (MWL) mod(s)
            "MWL_BlackForestBlacksmith2",       // Extra vendor location from More World Locations (MWL) mod(s)
            "MWL_MountainsBlacksmith1",         // Extra vendor location from More World Locations (MWL) mod(s)
            "MWL_MistlandsBlacksmith1",         // Extra vendor location from More World Locations (MWL) mod(s)
            "MWL_OceanTavern1"                  // Extra vendor location from More World Locations (MWL) mod(s)
        };

        private static List<string> dungeonLocations = new List<string>()
        {
            "Mistlands_DvergrBossEntrance1",
            "Hildir_cave",
            "Hildir_crypt",
            "Crypt2",
            "Crypt3",
            "Crypt4",
            "Mistlands_DvergrTownEntrance1",
            "Mistlands_DvergrTownEntrance2",
            "MountainCave02",
            "SunkenCrypt4",
            "BFD_Exterior"                  // // Extra dungion location from Underground Ruins mod
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

        /// <summary>
        /// Key used to pin a "point of interst" to the minimap.
        /// </summary>
        public static ConfigEntry<KeyboardShortcut> PinPointOfInterest;

        /// <summary>
        /// Key used to pin a an ore location to the minimap.
        /// </summary>
        public static ConfigEntry<KeyboardShortcut> PinOre;

        /// <summary>
        /// Key used to pin home or town location to the minimap.
        /// </summary>
        public static ConfigEntry<KeyboardShortcut> PinHome;

        /// <summary>
        /// Key used to pin a camp location to the minimap.
        /// </summary>
        public static ConfigEntry<KeyboardShortcut> PinCamp;

        /// <summary>
        /// Key used to pin a dungeon/crypt location to the minimap.
        /// </summary>
        public static ConfigEntry<KeyboardShortcut> PinDungeon;

        /// <summary>
        /// Key used to pin a portal to the minimap.
        /// </summary>
        public static ConfigEntry<KeyboardShortcut> PinPortal;

        /// <summary>
        /// Minimap text to associate with a "point of interest" quick-pin.
        /// </summary>
        public static ConfigEntry<string> PinTextPointOfInterest;

        /// <summary>
        /// Minimap text to associate with a an ore location quick-pin.
        /// </summary>
        public static ConfigEntry<string> PinTextOre;

        /// <summary>
        /// Minimap text to associate with a home/town quick-pin.
        /// </summary>
        public static ConfigEntry<string> PinTextHome;

        /// <summary>
        /// Minimap text to associate with a camp location quick-pin.
        /// </summary>
        public static ConfigEntry<string> PinTextCamp;

        /// <summary>
        /// Minimap text to associate with a dungeon/crypt quick-pin.
        /// </summary>
        public static ConfigEntry<string> PinTextDungeon;

        /// <summary>
        /// Minimap text to associate with a portal quick-pin.
        /// </summary>
        public static ConfigEntry<string> PinTextPortal;

        /// <summary>
        /// Flag that can be set to have dungeons auto-pin to the map when discovered.
        /// </summary>
        public static ConfigEntry<bool> AutoPinDungeonLocations;

        /// <summary>
        /// Flag taht can be used to show or hide the ingame message/ui received when discovering a location.
        /// </summary>
        public static ConfigEntry<bool> ShowLocationDiscoveryNotification;

        /// <summary>
        /// When the Sailing mod is present, this flag indicates that the sailing mod's explore
        /// radius should be used when sailing instead of the radius from this mod. Only affects sailing.
        /// </summary>
        public static ConfigEntry<bool> PreferSailingModExploreRadius;
        #endregion

        #region Public Variables
        /// <summary>
        /// Skill type identifier for the exploration skill. Used to retreive the exploration skill
        /// from the Valheim Skill registry.
        /// </summary>
        public static Skills.SkillType ExplorationSkillType;

        /// <summary>
        /// Flag that indicates if Smoothbrain's Sailing mod was detected or not.
        /// https://thunderstore.io/c/valheim/p/Smoothbrain/Sailing/
        /// </summary>
        public static bool SailingModDetected = false;
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
            AutoPinDungeonLocations = Config.Bind("General", "Auto-Pin Dungeon Locations", true, "Flag that can be set to have dungeons auto-pin to the map when discovered.");
            ShowLocationDiscoveryNotification = Config.Bind("General", "Show Location Discovery Notification", true, new ConfigDescription("Flag that can be set toggle whether a ui notification occurs when a location is discovered.", null, isAdminOnly));

            PinHome = Config.Bind("Hotkeys", "Pin Home Key", new KeyboardShortcut(KeyCode.Keypad0, KeyCode.RightControl), "Hotkey used to add a home/town pin to the mini-map.");
            PinPointOfInterest = Config.Bind("Hotkeys", "Pin Point of Interest Key", new KeyboardShortcut(KeyCode.Keypad1, KeyCode.RightControl), "Hotkey used to add a point of interest pin to the mini-map.");
            PinOre = Config.Bind("Hotkeys", "Pin Ore Key", new KeyboardShortcut(KeyCode.Keypad2, KeyCode.RightControl), "Hotkey used to add an ore deposit pin to the mini-map.");
            PinCamp = Config.Bind("Hotkeys", "Pin Camp Key", new KeyboardShortcut(KeyCode.Keypad3, KeyCode.RightControl), "Hotkey used to add a camp pin to the mini-map.");
            PinDungeon = Config.Bind("Hotkeys", "Pin Dungeon Key", new KeyboardShortcut(KeyCode.Keypad4, KeyCode.RightControl), "Hotkey used to add a dungeon pin to the mini-map.");
            PinPortal = Config.Bind("Hotkeys", "Pin Portal Key", new KeyboardShortcut(KeyCode.Keypad5, KeyCode.RightControl), "Hotkey used to add a portal pin to the mini-map.");

            PinTextHome = Config.Bind("Map Pin Text", "Pin Text Home", "Home or Town", "Text displayed when pinning a home location to the minimap with the keyboard shortcut.");
            PinTextPointOfInterest = Config.Bind("Map Pin Text", "Pin Text Point of Interest", "Point of Interest", "Text displayed when pinning a point of interest to the minimap with the keyboard shortcut.");
            PinTextOre = Config.Bind("Map Pin Text", "Pin Text Ore", "Ore Deposit", "Text displayed when pinning an ore deposit location to the minimap with the keyboard shortcut.");
            PinTextCamp = Config.Bind("Map Pin Text", "Pin Text Camp", "Camp", "Text displayed when pinning a camp location to the minimap with the keyboard shortcut.");
            PinTextDungeon = Config.Bind("Map Pin Text", "Pin Text Dungeon", "Crypt or Dungeon", "Text displayed when pinning a dungeon location to the minimap with the keyboard shortcut.");
            PinTextPortal = Config.Bind("Map Pin Text", "Pin Text Portal", "Portal", "Text displayed when pinning a portal to the minimap with the keyboard shortcut.");

            PreferSailingModExploreRadius = Config.Bind("Compatibility", "PreferSailingModExploreRadius", true,
                new ConfigDescription("When the Sailing mod is present, this flag indicates that the sailing mod's explore radius should be used when sailing instead of the radius from this mod. Only affects sailing.", null, isAdminOnly));

            LocalizationManager.OnLocalizationAdded += OnLocalizationsAdded;
            ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationAvailable;

            // Add the Exploration Skill
            SkillConfig explorerSkill = new SkillConfig();
            explorerSkill.Identifier = SkillId;
            explorerSkill.Name = "$skill_exploration";
            explorerSkill.Description = "$skill_exploration_desc";
            explorerSkill.IncreaseStep = SkillXpFactor.Value;
            ExplorationSkillType = SkillManager.Instance.AddSkill(explorerSkill);

            ZoneHelper.Instance.SetZoneDataAction = (zoneData) => SetZoneData(zoneData);

            Jotunn.Logger.LogInfo($"ExpertExplorer v{PluginVersion} loaded and patched.");
        }

        private void OnLocalizationsAdded()
        {
            ResolveLocalizations();
            //DebugTestLocalizations();

            // Check for Smoothbrain's sailing mod (which also increases sight radius while sailing)
            // Set a flag if it is enabled, so we can add compatibilty.
            // Odd to do it in localization, but this will ensure it happens once after the BepInEx Chainloader is finished.
            var plugin = Chainloader.PluginInfos.Values.FirstOrDefault(p => p.Metadata.GUID == "org.bepinex.plugins.sailing");
            if (plugin != null)
            {
                SailingModDetected = true;
                Jotunn.Logger.LogWarning($"{plugin.Metadata.Name} mod detected. Using compatibility.");
            }
            else
            {
                SailingModDetected = false;
            }
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
                    Vector2i zone = ZoneSystem.GetZone(playerPos);

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

                        if (ShowLocationDiscoveryNotification.Value)
                            QueueFoundLocationMsg(icon, "Location Discovered", currentZoneData.LocalizedLocationName, IsSpecialLocation(currentZoneData.LocationPrefab));

                        if (AutoPinDungeonLocations.Value &&
                            IsDungeonLocation(currentZoneData.LocationPrefab))
                        {
                            StartCoroutine(AutoPinLocation(currentZoneData, explorationData));
                        }
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
                        if (!locationPins.ContainsKey(currentZone) &&
                            !IsSpecialLocation(currentZoneData.LocationPrefab))
                        {
                            PinLocation(currentZoneData, explorationData);
                        }
                    }
                }

                if (IsPinKeyPressed(PinHome.Value))
                {
                    Minimap.instance.AddPin(Player.m_localPlayer.transform.position, Minimap.PinType.Icon1, PinTextHome.Value, true, false, 0L);
                    Player.m_localPlayer.Message(MessageType.TopLeft, $"{PinTextHome.Value} pinned to minimap.");
                }

                if (IsPinKeyPressed(PinPointOfInterest.Value))
                {
                    Minimap.instance.AddPin(Player.m_localPlayer.transform.position, Minimap.PinType.Icon3, PinTextPointOfInterest.Value, true, false, 0L);
                    Player.m_localPlayer.Message(MessageType.TopLeft, $"{PinTextPointOfInterest.Value} pinned to minimap.");
                }

                if (IsPinKeyPressed(PinOre.Value))
                {
                    Minimap.instance.AddPin(Player.m_localPlayer.transform.position, Minimap.PinType.Icon2, PinTextOre.Value, true, false, 0L);
                    Player.m_localPlayer.Message(MessageType.TopLeft, $"{PinTextOre.Value} pinned to minimap.");
                }

                if (IsPinKeyPressed(PinCamp.Value))
                {
                    Minimap.instance.AddPin(Player.m_localPlayer.transform.position, Minimap.PinType.Icon0, PinTextCamp.Value, true, false, 0L);
                    Player.m_localPlayer.Message(MessageType.TopLeft, $"{PinTextCamp.Value} pinned to minimap.");
                }

                if (IsPinKeyPressed(PinDungeon.Value))
                {
                    Minimap.instance.AddPin(Player.m_localPlayer.transform.position, Minimap.PinType.Icon3, PinTextDungeon.Value, true, false, 0L);
                    Player.m_localPlayer.Message(MessageType.TopLeft, $"{PinTextDungeon.Value} pinned to minimap.");
                }

                if (IsPinKeyPressed(PinPortal.Value))
                {
                    Minimap.instance.AddPin(Player.m_localPlayer.transform.position, Minimap.PinType.Icon4, PinTextPortal.Value, true, false, 0L);
                    Player.m_localPlayer.Message(MessageType.TopLeft, $"{PinTextPortal.Value} pinned to minimap.");
                }
            }
        }

        private bool IsPinKeyPressed(KeyboardShortcut pinKey)
        {
            if (!ZInput.GetKeyDown(pinKey.MainKey))
                return false;

            bool pressed = true;
            foreach (var modKey in pinKey.Modifiers)
            {
                if (!ZInput.GetKey(modKey))
                {
                    pressed = false;
                    Jotunn.Logger.LogInfo($"Missing key modifier {modKey}");
                    break;
                }    
            }

            return pressed;
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
        /// Check to see if a location is considered a "dungeon" location.
        /// </summary>
        /// <param name="locationPrefabName">Name of the location prefab.</param>
        /// <returns>True if the location is considered a "dungeon", False if not.</returns>
        public static bool IsDungeonLocation(string locationPrefabName)
        {
            return dungeonLocations.Contains(locationPrefabName);
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

            var zone = ZoneSystem.GetZone(pinData.m_pos);

            if (locationPins.ContainsKey(zone) &&
                locationPins[zone].m_name == pinData.m_name)
            {
                locationPins.Remove(zone);

                // remove from custom player data
                var explorationData = Player.m_localPlayer.ExplorationData();
                explorationData?.RemovePin(zone);
            }
        }

        private static IEnumerator AutoPinLocation(ZoneData zoneData, PlayerExplorationData explorationData)
        {
            yield return new WaitForSeconds(1.0f);
            PinLocation(zoneData, explorationData);
        }

        private static void PinLocation(ZoneData zoneData, PlayerExplorationData explorationData)
        {
            Vector3 pinPos = zoneData.LocationPosition;
            Vector2i zone = zoneData.ZoneId;
            string pinName = zoneData.LocalizedLocationName;

            var pinData = Minimap.instance.AddPin(pinPos, Minimap.PinType.Icon3, pinName, true, false, 0L);
            locationPins[zone] = pinData;
            explorationData.FlagAsPinned(zone, pinData);
            Player.m_localPlayer.Message(MessageType.TopLeft, $"Location pinned to minimap.");

#if DEBUG
            Jotunn.Logger.LogInfo("Pin Added.");
#endif
        }

        /// <summary>
        /// Called when the server responds with the data for the current zone.
        /// This cache off the data for this zone and render an icon for the location.
        /// </summary>
        /// <param name="zoneData">The data for the current zone.</param>
        private static void SetZoneData(ZoneData zoneData)
        {
            if (zoneData == null)
            {
                Jotunn.Logger.LogWarning("SetZoneData called but ZoneData was null.");
                return;
            }

            if (currentZone == zoneData.ZoneId)
            {
                // Update the client-side record of the zone information
                zoneDataCache[currentZone] = zoneData;
                currentZoneData = zoneData;

                if (!locationSpriteMap.ContainsKey(currentZone))
                {
                    if (!string.IsNullOrEmpty(zoneData.LocationPrefab))
                    {
                        var zoneLocation = ZoneHelper.Instance.GetZoneLocation(zoneData.LocationPrefab);
                        if (zoneLocation != null)
                        {
                            GameObject exteriorPrefab = zoneLocation.GetLocationAsset();
                            Location location = exteriorPrefab?.GetComponent<Location>() ?? null;

                            if (location != null && location.m_hasInterior)
                            {
                                var locationTransform = location.transform;

                                if (locationTransform != null)
                                {
                                    for (int i = 0; i < locationTransform.childCount; i++)
                                    {
                                        var childTransform = locationTransform.GetChild(i);
                                        if (childTransform != null && childTransform.name.ToLowerInvariant().Equals("exterior"))
                                        {
                                            exteriorPrefab = childTransform.gameObject;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (exteriorPrefab != null)
                            {
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
                            else
                            {
                                Jotunn.Logger.LogWarning($"No asset prefab found for location {zoneData.LocationPrefab}.");
                            }
                        }
                        else
                        {
                            Jotunn.Logger.LogWarning($"No zone location found for {zoneData.LocationPrefab}.");
                        }
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
        public static void QueueFoundLocationMsg(Sprite icon, string topic, string description, bool isSpecialLocation)
        {
            string desc = description;
            string keyCode = PinKey.Value.ToString();

            if (!isSpecialLocation)
                desc += $"\nPress [<color=yellow><b>{keyCode}</b></color>] to add a pin";

            UnlockMsg unlockMsg = new UnlockMsg();
            unlockMsg.m_icon = icon;
            unlockMsg.m_topic = topic;
            unlockMsg.m_description = desc;
            MessageHud.instance.m_unlockMsgQueue.Enqueue(unlockMsg);
            MessageHud.instance.AddLog(topic + ": " + description);
        }

        /// <summary>
        /// Wrapper around <see cref="CustomLocalization.TryTranslate(string)"/> to ensure it
        /// is not null.
        /// </summary>
        /// <param name="word">The word to translate.</param>
        /// <returns>The translated word, or the original word if <see cref="CustomLocalization"/> is null or localization verb not found.</returns>
        public static string TryTranslate(string word)
        {
            return Localization?.TryTranslate(word) ?? word;
        }

        private void ResolveLocalizations()
        {
            try
            {
                DirectoryInfo pluginDir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                Dictionary<string, List<FileInfo>> languageFiles = new Dictionary<string, List<FileInfo>>();

                foreach (var jsonFile in pluginDir.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly))
                {
                    string[] nameTokens = jsonFile.Name.Split(new char[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (nameTokens.Length > 2 &&
                        nameTokens.Last() == "json")
                    {
                        string lang = nameTokens[nameTokens.Length - 2].Trim();
                        //Logger.LogInfo($"{jsonFile.Name} - {lang}");
                        if (LocalizationHelper.IsLanguageSupported(lang))
                        {
                            if (!languageFiles.ContainsKey(lang))
                                languageFiles.Add(lang, new List<FileInfo>());

                            languageFiles[lang].Add(jsonFile);
                        }
                    }
                }

                foreach (var lang in  languageFiles.Keys)
                {
                    if (!Localization.GetLanguages().Contains(lang))
                    {
                        foreach (var jsonFile in languageFiles[lang])
                        {
                            Localization.AddJsonFile(lang, File.ReadAllText(jsonFile.FullName));
                            Logger.LogInfo($"Added localization file [{jsonFile.Name}] from non-standard location.");
                        }
                    }
                }

                foreach (var lang in Localization.GetLanguages())
                    Logger.LogInfo($"{lang} localization loaded and available.");
            }
            catch (Exception)
            {
                Logger.LogError("Error resolving localizations.");
            }
        }

        private void DebugTestLocalizations()
        {
            foreach (var lang in Localization.GetLanguages())
            {
                Logger.LogInfo($"---------------------------------------------------------------");
                Logger.LogInfo($"Localizations for {lang}");
                foreach (var translate in Localization.GetTranslations(lang))
                    Logger.LogInfo($"\"{translate.Key}\": {translate.Value}");
                Logger.LogInfo($"---------------------------------------------------------------");
            }

            string localStrTest = TryTranslate("$location_StartTemple");
            Logger.LogInfo($"Localization Test: {localStrTest}");
        }
    }
}

