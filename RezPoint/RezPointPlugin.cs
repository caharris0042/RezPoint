using System.Collections.Generic;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RezPoint
{
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RezPointPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "LuckyBread";
        public const string PluginName = "RezPoint";
        public const string PluginVersion = "0.1.0";

        public static RezPointPlugin Instance;
        public static GameObject TotemPrefab;

        public void Awake()
        {
            Instance = this;
            Log.Init(Logger);

            TotemPrefab = BuildTotemPrefab();

            On.RoR2.GlobalEventManager.OnPlayerCharacterDeath += OnPlayerCharacterDeath;
            On.RoR2.Run.AdvanceStage += OnAdvanceStage;
            On.RoR2.Run.BeginGameOver += OnBeginGameOver;

            Log.Info("RezPoint loaded.");
        }

        private static GameObject BuildTotemPrefab()
        {
            var root = new GameObject("RezTotemBase");
            root.AddComponent<NetworkIdentity>();
            root.AddComponent<RezTotem>();

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localScale = Vector3.one * 1.5f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());

            // PrefabAPI assigns a stable assetId and registers the prefab on all clients
            var prefab = PrefabAPI.InstantiateClone(root, "RezTotem", registerNetwork: true);
            Object.Destroy(root);
            return prefab;
        }

        private void OnPlayerCharacterDeath(
            On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig,
            GlobalEventManager self,
            DamageReport damageReport,
            NetworkUser victimNetworkUser)
        {
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

        private void OnAdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene)
        {
            orig(self, nextScene);
            if (NetworkServer.active) ClearTotems();
        }

        private void OnBeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            orig(self, gameEndingDef);
            if (NetworkServer.active) ClearTotems();
        }

        private static void ClearTotems()
        {
            foreach (var totem in new List<RezTotem>(RezTotem.ActiveTotems))
                NetworkServer.Destroy(totem.gameObject);
        }
    }
}
