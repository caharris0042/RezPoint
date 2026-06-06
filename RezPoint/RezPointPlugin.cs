using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RezPoint
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RezPointPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "YourName";
        public const string PluginName = "RezPoint";
        public const string PluginVersion = "0.1.0";

        public static RezPointPlugin Instance;

        public void Awake()
        {
            Instance = this;
            Log.Init(Logger);

            On.RoR2.GlobalEventManager.OnPlayerCharacterDeath += OnPlayerCharacterDeath;

            Log.Info("RezPoint loaded.");
        }

        private void OnPlayerCharacterDeath(
            On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig,
            GlobalEventManager self,
            DamageReport damageReport,
            NetworkUser victimNetworkUser)
        {
            // Capture foot position before orig may destroy the body
            var deathPosition = damageReport.victimBody != null
                ? damageReport.victimBody.footPosition
                : Vector3.zero;

            orig(self, damageReport, victimNetworkUser);

            if (!NetworkServer.active) return;
            if (Run.instance == null) return;

            var master = victimNetworkUser?.masterController?.master;
            if (master == null) return;

            RezTotem.SpawnForPlayer(master, deathPosition);
        }
    }
}
