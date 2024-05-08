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

namespace ExpertExplorer
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class ExpertExplorer : BaseUnityPlugin
    {
        public const string PluginGUID = "com.milkwyzard.ExpertExplorer";
        public const string PluginName = "ExpertExplorer";
        public const string PluginVersion = "1.0.0";
        public const string SkillId = $"{PluginGUID}.Exploration";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private Harmony harmony;

        private const float POST_INTRO_DELAY = 1.0f;        // Delay after the intro before the mod starts checking for locations
        private const float ZONE_CHECK_FREQUENCY = 1.0f;    // Frequency at which the zone is checked

        private static ZoneData currentZoneData = null;
        private static Vector2i currentZone;
        private static string lastEvaluatedLocation = string.Empty;
        private static float zoneCheckTimer = 0.0f;
        private static bool introLastFrame = false;
        private static bool locationsAvailable = false;

        private static List<Chat.WorldTextInstance> discoverTexts = new List<Chat.WorldTextInstance>();

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
            // Load config
            SkillXpFactor = Config.Bind("General", "SkillXpFactor", 1.2f, "Factor applied to skill gain. Higher number means faster skill gain.");
            MaxExploreRadius = Config.Bind("Exploration", "MaxExploreRadius", 200f, "Max explore radius used when the Exploration Skill is at 100.");
            DiscoverDistance = Config.Bind("Exploration", "DiscoverDistance", 10f, "Distance between the player and the bounds of a location required to mark the location as discovered.");

            ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationAvailable;

            // Add the Exploration Skill
            SkillConfig explorerSkill = new SkillConfig();
            explorerSkill.Identifier = SkillId;
            explorerSkill.Name = "$skill_exploration";
            explorerSkill.Description = "$skill_exploration_desc";
            explorerSkill.IncreaseStep = SkillXpFactor.Value;
            ExplorationSkillType = SkillManager.Instance.AddSkill(explorerSkill);

            Jotunn.Logger.LogInfo($"ExpertExplorer v{PluginVersion} loaded and patched.");
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

            // check to see if the timer has elapsed
            if (zoneCheckTimer == 0.0f)
            {
                Vector2i zone = ZoneSystem.instance.GetZone(playerPos);

                // if the zone has changed, update the zone information
                if (zone != currentZone)
                {
                    currentZone = zone;
                    currentZoneData = ZoneHelper.GetZoneData(zone);

                    Jotunn.Logger.LogDebug($"Entering zone ({zone.x}, {zone.y}) - Location {currentZoneData?.ZoneLocation?.m_prefabName}");
                }

                zoneCheckTimer = ZONE_CHECK_FREQUENCY;
            }

            if (currentZoneData != null && currentZoneData.IsValid() && !explorationData.IsZoneLocationAlreadyDiscovered(currentZone))
            {
                if (IsLookingAtLocation(Player.m_localPlayer, currentZoneData.ZoneLocation) ||
                    IsInsideLocation(Player.m_localPlayer, currentZoneData.ZoneLocation))
                {
                    if (currentZoneData.ZoneLocation.m_prefabName != lastEvaluatedLocation)
                    {
                        // location discovered
                        lastEvaluatedLocation = currentZoneData.ZoneLocation.m_prefabName;
                        explorationData.FlagAsDiscovered(currentZoneData);
                        explorationData.Save(Player.m_localPlayer);
                        Player.m_localPlayer.RaiseSkill(ExplorationSkillType);

                        string text = $"Discovered {currentZoneData.LocalizedLocationName}";

                        // Set the position of the notification text. Here we find the point on the terrain at the center of the location
                        // prefab. The text (in the world) can start at player height above the terrain.
                        Vector3 textPos = currentZoneData.LocationPosition;
                        textPos.y = ZoneSystem.instance.GetGroundHeight(currentZoneData.LocationPosition) + Player.m_localPlayer.GetHeight();

                        // add the text to the world
                        AddDiscoverTextToWorld(text, textPos);
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
                if (IsLookingAtLocation(player, currentZoneData.ZoneLocation) ||
                    IsInsideLocation(player, currentZoneData.ZoneLocation))
                {
                    return currentZoneData;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks to see if the player is looking at a specific location.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="location">The location to check.</param>
        /// <returns>True if the player is looking at the given location, False if not.</returns>
        private static bool IsLookingAtLocation(Player player, ZoneSystem.ZoneLocation location)
        {
            float locationRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
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
        private static bool IsInsideLocation(Player player, ZoneSystem.ZoneLocation location)
        {
            float locationRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
            return Vector3.Distance(player.transform.position, currentZoneData.LocationPosition) < locationRadius;
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

