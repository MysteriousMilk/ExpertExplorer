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
    }
}
