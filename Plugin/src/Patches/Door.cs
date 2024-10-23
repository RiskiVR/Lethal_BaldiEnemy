using DunGen;
using HarmonyLib;

namespace BaldiEnemy.Patches;

public class DoorPatch
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DoorLock), nameof(DoorLock.OpenOrCloseDoor))]
    public static void DoorStatePatch(DoorLock __instance)
    {
        Plugin.Logger.LogInfo($"A door has changed state at position {__instance.transform.position}");
        BaldiHearingManager.AlertBaldiToDoorSound(__instance.transform.position);
    }
}
