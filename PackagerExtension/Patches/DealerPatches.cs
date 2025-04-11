using HarmonyLib; 
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Storage;
using PackagerExtension;

namespace PackagerExtension.Patches
{
    [HarmonyPatch(typeof(Dealer), nameof(Dealer.CustomerContractStarted))]
    public static class CustomerContractStartedPatch
    {
        public static void Postfix(Dealer __instance, Contract contract)
        {
            Core.MelonLogger.Msg($"Customer contract started for dealer: {__instance.fullName}, Contract: {contract.Customer.name}");
        }
    }

    [HarmonyPatch(typeof(Dealer), nameof(Dealer.CustomerContractEnded))]
    public static class CustomerContractEndeddPatch
    {
        public static void Postfix(Dealer __instance, Contract contract)
        {
            Core.MelonLogger.Msg($"Customer contract ended for dealer: {__instance.fullName}, Contract: {contract.Customer.name}");
        }
    }
}
