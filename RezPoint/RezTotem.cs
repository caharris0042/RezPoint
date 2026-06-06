using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RezPoint
{
    public class RezTotem : NetworkBehaviour
    {
        public static readonly List<RezTotem> ActiveTotems = new();

        public static float ReviveTime = 10f;
        public static float ReviveRadius = 3f;

        private CharacterMaster deadPlayer;
        private Vector3 deathPosition;
        private float progress = 0f;
        private bool reviveTriggered = false;

        public static void SpawnForPlayer(CharacterMaster master, Vector3 position)
        {
            var go = Object.Instantiate(RezPointPlugin.TotemPrefab, position, Quaternion.identity);
            var totem = go.GetComponent<RezTotem>();
            totem.deadPlayer = master;
            totem.deathPosition = position;
            NetworkServer.Spawn(go);
            Log.Info($"Totem spawned at {position} for {master.playerCharacterMasterController?.networkUser?.userName}");
        }

        private void OnEnable() => ActiveTotems.Add(this);
        private void OnDisable() => ActiveTotems.Remove(this);

        private void Update()
        {
            if (reviveTriggered || !NetworkServer.active) return;
            if (Run.instance == null) { NetworkServer.Destroy(gameObject); return; }

            bool anyInRange = false;
            foreach (var pmc in PlayerCharacterMasterController.instances)
            {
                if (pmc.master == deadPlayer) continue;
                var body = pmc.master.GetBody();
                if (body == null) continue;
                if (Vector3.Distance(body.footPosition, transform.position) <= ReviveRadius)
                {
                    anyInRange = true;
                    break;
                }
            }

            if (anyInRange)
            {
                progress += Time.deltaTime / ReviveTime;
                if (progress >= 1f)
                    TriggerRevive();
            }
        }

        private void TriggerRevive()
        {
            reviveTriggered = true;
            Log.Info($"Reviving {deadPlayer?.playerCharacterMasterController?.networkUser?.userName}");

            // Destroy any current body (e.g. a ghost drone) before respawning the main character
            if (deadPlayer.hasBody)
                NetworkServer.Destroy(deadPlayer.GetBodyObject());

            deadPlayer.Respawn(deathPosition, Quaternion.identity);
            NetworkServer.Destroy(gameObject);
        }
    }
}
