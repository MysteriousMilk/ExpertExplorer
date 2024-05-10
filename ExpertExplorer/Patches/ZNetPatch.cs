using HarmonyLib;

namespace ExpertExplorer.Patches
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNetPatch_Awake
    {
        public static void Postfix(ZNet __instance)
        {
            ZoneHelper.Instance.RegisterRPC(__instance.m_routedRpc);
        }
    }
}
