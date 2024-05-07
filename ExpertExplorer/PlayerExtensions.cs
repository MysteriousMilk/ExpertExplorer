using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertExplorer
{
    public static class PlayerExtensions
    {
        public static void InitializeExplorationData(this Player player)
        {
            if (player == null || player.HasExplorationData())
                return;

            var explorerData = player.gameObject.AddComponent<PlayerExplorationData>();
            explorerData.Load(player);
        }

        public static bool HasExplorationData(this Player player)
        {
            return player?.gameObject.GetComponent<PlayerExplorationData>() != null;
        }

        public static PlayerExplorationData ExplorationData(this Player player)
        {
            return player?.gameObject.GetComponent<PlayerExplorationData>();
        }
    }
}
