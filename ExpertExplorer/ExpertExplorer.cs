using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace ExpertExplorer
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ExpertExplorer : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.ExpertExplorer";
        public const string PluginName = "ExpertExplorer";
        public const string PluginVersion = "0.0.1";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private const float ZONE_CHECK_FREQUENCY = 1.0f;    // Frequency at which the zone is checked
        private const float DISCOVER_DISTANCE = 10.0f;      // 10 meter discover distance

        private ZoneData currentZoneData = null;
        private Vector2i currentZone;
        private float zoneCheckTimer = 0.0f;
        private bool locationsAvailable = false;

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("ExpertExplorer has landed");

            ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationAvailable;
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

            Vector3 playerPos = Player.m_localPlayer.transform.position;

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
                }

                zoneCheckTimer = ZONE_CHECK_FREQUENCY;
            }

            if (currentZoneData != null && currentZoneData.IsValid())
            {
                // check to see if the player is in near the location and "looking" at it
                if (Vector3.Distance(playerPos, currentZoneData.LocationPosition) < DISCOVER_DISTANCE)
                {
                    // make sure the player is looking in that direction
                    Vector3 delta = currentZoneData.LocationPosition - playerPos;
                    float angle = Vector3.Angle(delta, Player.m_localPlayer.transform.forward);

                    if (angle < 20.0f)
                    {
                        // location discovered
                        Jotunn.Logger.LogInfo($"Discovered location {currentZoneData.ZoneLocation.m_prefabName}");
                    }
                }
            }
        }
    }
}

