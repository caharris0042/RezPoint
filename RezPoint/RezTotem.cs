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
        public static float ReviveRadius = 5f;

        private const float MinLightIntensity = 1f;
        private const float MaxLightIntensity = 8f;

        private CharacterMaster deadPlayer;
        private GameObject originalBodyPrefab;
        private Vector3 deathPosition;

        [SyncVar]
        private float progress = 0f;
        private bool reviveTriggered = false;

        private Light reviveLight;

        protected void Awake()
        {
            reviveLight = GetComponent<Light>();
        }

        public static void SpawnForPlayer(CharacterMaster master, Vector3 position)
        {
            var go = Object.Instantiate(RezPointPlugin.TotemPrefab, position, Quaternion.identity);
            var totem = go.GetComponent<RezTotem>();
            totem.deadPlayer = master;
            totem.deathPosition = position;
            totem.originalBodyPrefab = master.bodyPrefab;
            NetworkServer.Spawn(go);
            Log.Info($"Totem spawned at {position} for {master.playerCharacterMasterController?.networkUser?.userName}");
        }

        private void OnEnable() => ActiveTotems.Add(this);
        private void OnDisable() => ActiveTotems.Remove(this);

        private void Update()
        {
            UpdateVisuals();

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

        private void UpdateVisuals()
        {
            if (reviveLight == null) return;
            reviveLight.intensity = Mathf.Lerp(MinLightIntensity, MaxLightIntensity, progress);
            reviveLight.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.3f), progress);
            // TODO: play looping channeling sound keyed to progress
        }

        private void TriggerRevive()
        {
            reviveTriggered = true;
            Log.Info($"Reviving {deadPlayer?.playerCharacterMasterController?.networkUser?.userName}");

            if (deadPlayer.hasBody)
                NetworkServer.Destroy(deadPlayer.GetBodyObject());

            // Restore the body prefab captured at death — drone mode changes this on the master
            deadPlayer.bodyPrefab = originalBodyPrefab;
            deadPlayer.Respawn(deathPosition, Quaternion.identity);
            // TODO: play revival completion sound here

            NetworkServer.Destroy(gameObject);
        }
    }
}
