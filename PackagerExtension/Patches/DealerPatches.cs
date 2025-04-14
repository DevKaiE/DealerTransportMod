using HarmonyLib; 
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Storage;
using DealerSelfSupplySystem;
using UnityEngine;

namespace DealerSelfSupplySystem.Patches
{
    [HarmonyPatch(typeof(NPC), nameof(NPC.Update))]
    public static class NPCUpdatePatch
    {
        public static void Postfix(NPC __instance)
        {
            Vector3 position = __instance.transform.position;
            //Core.MelonLogger.Msg($"NPC Position: x {position.x}, y {position.y}, z {position.z}");
            //Core.MelonLogger.Msg($"NPC Destination: {__instance.Movement.CurrentDestination}");
        }
    }
}
