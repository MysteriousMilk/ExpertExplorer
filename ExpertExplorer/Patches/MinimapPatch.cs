using HarmonyLib;
using System;
using TMPro;
using UnityEngine;

namespace ExpertExplorer.Patches
{
    [HarmonyPatch]
    public static class MinimapPatch
    {
        private static TMP_Text m_LocationNameSmall;
        private static float miniMapHeight;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Minimap), "Explore", new Type[] { typeof(Vector3), typeof(float) })]
        private static void Explore(ref Minimap __instance, ref Vector3 p, ref float radius)
        {
            float lerpFactor = Player.m_localPlayer.GetSkillLevel(ExpertExplorer.ExplorationSkillType) / 100f;
            radius = Mathf.Lerp(__instance.m_exploreRadius, ExpertExplorer.MaxExploreRadius.Value, lerpFactor);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), "Awake")]
        private static void Awake(ref Minimap __instance)
        {
            miniMapHeight = __instance.m_mapImageSmall.rectTransform.rect.height;

            var textGameObject = UnityEngine.Object.Instantiate(__instance.m_biomeNameSmall.gameObject, __instance.m_biomeNameSmall.gameObject.transform.parent);
            m_LocationNameSmall = textGameObject.GetComponent<TMP_Text>();

            if (m_LocationNameSmall == null)
            {
                Jotunn.Logger.LogWarning("Could not duplicate biome name text");
            }
            else
            {
                m_LocationNameSmall.text = "Location Text Test";
                m_LocationNameSmall.fontSize = 12;
                m_LocationNameSmall.rectTransform.position = new Vector3(
                    m_LocationNameSmall.rectTransform.position.x,
                    m_LocationNameSmall.rectTransform.position.y + 25,
                    m_LocationNameSmall.rectTransform.position.z);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), "RemovePin", new Type[] { typeof(Minimap.PinData) })]
        private static void RemovePin(ref Minimap __instance, ref Minimap.PinData pin)
        {
            ExpertExplorer.OnPinRemoved(pin);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), "UpdateBiome")]
        private static void UpdateBiome(ref Minimap __instance, ref Player player)
        {
            // Called from "UpdateBiome" so that all pre-condition checks for "UpdateBiome"
            // also apply for updating the location.
            UpdateLocation(player);
        }

        private static void UpdateLocation(Player localPlayer)
        {
            var currentZoneData = ExpertExplorer.GetCurrentLocation(localPlayer);
            string locationText = currentZoneData != null ? currentZoneData.LocalizedLocationName : string.Empty;

            m_LocationNameSmall.text = locationText;
        }
    }
}
