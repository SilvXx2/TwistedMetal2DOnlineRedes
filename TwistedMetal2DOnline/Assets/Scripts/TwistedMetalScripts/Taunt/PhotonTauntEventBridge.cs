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

    /// <summary>
    /// Crea, serializa, cifra y envía el paquete de burla a través de la red usando RaiseEvent.
    /// </summary>
    public void BroadcastTaunt(int attackerActorNumber, int victimActorNumber, int tauntId)
    {
        // 1. Crear el paquete con los datos compactos
        TauntNetworkPackage package = new TauntNetworkPackage(attackerActorNumber, victimActorNumber, tauntId);

        // 2. Serializar a bytes (binario)
        byte[] serializedData = package.Serialize();

        // 3. Cifrar los bytes mediante XOR
        byte[] encryptedData = TauntNetworkPackage.EncryptDecrypt(serializedData);

        // 4. Configurar opciones del evento para enviar a todos los clientes en la sala
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        // 5. Enviar el paquete binario de bajo overhead a través de Photon
        PhotonNetwork.RaiseEvent(tauntEventCode, encryptedData, raiseEventOptions, SendOptions.SendReliable);
        
        Debug.Log($"[PhotonTauntEventBridge] Burla enviada. Attacker: {attackerActorNumber}, Victim: {victimActorNumber}, TauntId: {tauntId}. Tamaño payload: {encryptedData.Length} bytes.");
    }

    /// <summary>
    /// Intenta descifrar, deserializar y desempaquetar los datos recibidos de un evento de red.
    /// </summary>
    public static bool TryUnpack(object customData, out int attackerActorNumber, out int victimActorNumber, out int tauntId)
    {
        if (customData is byte[] encryptedData)
        {
            try
            {
                // 1. Descifrar los bytes mediante XOR (al ser reflexivo se usa la misma función)
                byte[] decryptedData = TauntNetworkPackage.EncryptDecrypt(encryptedData);

                // 2. Deserializar los datos binarios en la estructura original
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
