using HarmonyLib;

namespace ExpertExplorer.Patches
{
    [HarmonyPatch]
    public static class PlayerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Load")]
        private static void Load(ref Player __instance, ref ZPackage pkg)
        {
            __instance.InitializeExplorationData();
            __instance.ExplorationData().Load(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "Save")]
        private static void Save(ref Player __instance, ref ZPackage pkg)
        {
            __instance.InitializeExplorationData();
            __instance.ExplorationData().Save(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "AddKnownBiome")]
        private static void AddKnownBiome(ref Player __instance, ref Heightmap.Biome biome)
        {
            if (!__instance.IsBiomeKnown(biome))
            {
                var explorationData = Player.m_localPlayer.ExplorationData();

                if (explorationData != null)
                {
                    explorationData.FlagAsDiscovered(biome);
                    __instance.RaiseSkill(ExpertExplorer.ExplorationSkillType);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        private static void OnSpawned(ref Player __instance)
        {
            if (__instance.InIntro())
                return;

            if (Game.instance.m_firstSpawn)
                return;

            // make sure we get the zone info as soon as the player spawns
            Vector2i zone = ZoneSystem.GetZone(__instance.transform.position);
            ZoneHelper.Instance.Client_RequestZoneData(zone);
        }
    }
}
