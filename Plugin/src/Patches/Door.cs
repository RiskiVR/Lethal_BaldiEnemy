using DunGen;
using HarmonyLib;

namespace BaldiEnemy.Patches;

public class DoorPatch
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Door), nameof(Door.SetDoorState))]
    public static void DoorStatePatch(Door __instance)
    {
        Plugin.Logger.LogInfo($"A door has changed state at position {__instance.transform.position}");
        BaldiHearingManager.AlertBaldiToDoorSound(__instance.transform.position);
    }
}
