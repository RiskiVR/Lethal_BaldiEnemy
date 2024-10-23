using UnityEngine;

namespace BaldiEnemy;

internal static class BaldiHearingManager
{

    private static BaldiEnemy? Baldi;

    internal static void AlertBaldiToDoorSound(Vector3 position)
    {
        //Alert baldi here
        Baldi?.HearDoorStateChange(position);
    }

    internal static void RegisterSpawnedBaldi(BaldiEnemy IncomingBaldi)
    {
        //register Baldi here
        Baldi ??= IncomingBaldi;
    }
}
