using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RezPoint
{
    public class RezTotem : MonoBehaviour
    {
        public static float ReviveTime = 10f;
        public static float ReviveRadius = 3f;

        private CharacterMaster deadPlayer;
        private float progress = 0f;
        private bool reviveTriggered = false;

        public static void SpawnForPlayer(CharacterMaster master, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 1.5f;
            Object.Destroy(go.GetComponent<Collider>());

            var totem = go.AddComponent<RezTotem>();
            totem.deadPlayer = master;

            Log.Info($"Totem spawned at {position} for {master.playerCharacterMasterController?.networkUser?.userName}");
        }

        private void Update()
        {
            if (reviveTriggered || !NetworkServer.active) return;
            if (Run.instance == null) { Destroy(gameObject); return; }

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
            deadPlayer.RespawnExtraLife();
            // RespawnExtraLife adds ExtraLifeConsumed regardless of whether the player had ExtraLife
            deadPlayer.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
            Destroy(gameObject);
        }
    }
}
