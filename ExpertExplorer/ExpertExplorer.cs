using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

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

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("ExpertExplorer has landed");

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
            ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationAvailable;
        }

        private void OnVanillaLocationAvailable()
        {
            throw new System.NotImplementedException();
        }

        // Called every frame
        private void Update()
        {
            if (ZInput.instance == null)
                return;


        }
    }
}

