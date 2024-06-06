using HarmonyLib;
using modweaver.core;
using Unity.Collections;
using Unity.Netcode;

namespace PersistentSwords;

[ModMainClass]
public class ModMain : Mod
{
    public override void Init()
    {
        Harmony harmony = new Harmony(Metadata.id);
        harmony.PatchAll();
    }

    public override void Ready() { }

    public override void OnGUI(ModsMenuPopup ui) { }
}

[HarmonyPatch]
public class PatchClass
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ParticleBlade), nameof(ParticleBlade.OnThrowClientRpc))]
    public static bool SwordThrowPatch(ParticleBlade __instance)
    {
        NetworkManager networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
        {
            return false;
        }
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
        {
            FastBufferWriter fastBufferWriter = new FastBufferWriter(1285, Allocator.Temp, 63985);
            ClientRpcParams clientRpcParams = new();
            __instance.__sendClientRpc(fastBufferWriter, 3782494935U, clientRpcParams, RpcDelivery.Reliable);
            fastBufferWriter.Dispose();
        }
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost))
        {
            return false;
        }
        __instance._charging = false;
        __instance.chargeParticles.Stop();
        __instance.ResetBlade();
        __instance.Invoke("TurnOff", 0.2f);
        __instance.Invoke("TurnOn", 0.1f);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ParticleBlade), nameof(ParticleBlade.OnNetworkSpawn))]
    public static void CrossbowPatch(ParticleBlade __instance)
    {
        if (__instance.preArmed)
        {
            __instance.CancelInvoke("TurnOff");
        }
    }
}