using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

internal sealed class PhotonTauntEventBridge
{
    private readonly byte tauntEventCode;

    public PhotonTauntEventBridge(byte tauntEventCode)
    {
        this.tauntEventCode = tauntEventCode;
    }

    public void BroadcastTaunt(int attackerActorNumber, int victimActorNumber, int tauntId)
    {
        TauntNetworkPackage package = new TauntNetworkPackage(attackerActorNumber, victimActorNumber, tauntId);

        byte[] serializedData = package.Serialize();
        byte[] encryptedData = TauntNetworkPackage.EncryptDecrypt(serializedData);

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent(tauntEventCode, encryptedData, raiseEventOptions, SendOptions.SendReliable);
        Debug.Log($"[PhotonTauntEventBridge] Burla enviada. Attacker: {attackerActorNumber}, Victim: {victimActorNumber}, TauntId: {tauntId}. Tamaño payload: {encryptedData.Length} bytes.");
    }

    public static bool TryUnpack(object customData, out int attackerActorNumber, out int victimActorNumber, out int tauntId)
    {
        if (customData is byte[] encryptedData)
        {
            try
            {
                byte[] decryptedData = TauntNetworkPackage.EncryptDecrypt(encryptedData);
                TauntNetworkPackage package = TauntNetworkPackage.Deserialize(decryptedData);

                attackerActorNumber = package.AttackerActorNumber;
                victimActorNumber = package.VictimActorNumber;
                tauntId = package.TauntId;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PhotonTauntEventBridge] Error al deserializar el paquete de burla: {e.Message}");
            }
        }

        attackerActorNumber = 0;
        victimActorNumber = 0;
        tauntId = 0;
        return false;
    }
}
