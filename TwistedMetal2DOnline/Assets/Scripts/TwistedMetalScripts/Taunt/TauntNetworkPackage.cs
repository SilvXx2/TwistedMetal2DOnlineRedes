using System.IO;

public struct TauntNetworkPackage
{
    public int AttackerActorNumber;
    public int VictimActorNumber;
    public int TauntId;

    public TauntNetworkPackage(int attackerActorNumber, int victimActorNumber, int tauntId)
    {
        AttackerActorNumber = attackerActorNumber;
        VictimActorNumber = victimActorNumber;
        TauntId = tauntId;
    }

    /// <summary>
    /// Serializa los datos del paquete en una secuencia binaria de bytes (Diapositiva 15).
    /// </summary>
    public byte[] Serialize()
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(AttackerActorNumber);
                writer.Write(VictimActorNumber);
                writer.Write(TauntId);
            }
            return ms.ToArray();
        }
    }

    /// <summary>
    /// Deserializa la secuencia binaria en un objeto TauntNetworkPackage (Diapositiva 15).
    /// Es fundamental leer en el mismo orden que se escribió.
    /// </summary>
    public static TauntNetworkPackage Deserialize(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int attacker = reader.ReadInt32();
                int victim = reader.ReadInt32();
                int taunt = reader.ReadInt32();
                return new TauntNetworkPackage(attacker, victim, taunt);
            }
        }
    }

    /// <summary>
    /// Aplica encriptación binaria XOR sobre la secuencia de bytes usando la reflexividad (Diapositiva 31-40).
    /// La clave utilizada es 60 (Diapositiva 33).
    /// Al ser reflexivo, el mismo método cifra y descifra.
    /// </summary>
    public static byte[] EncryptDecrypt(byte[] data)
    {
        byte key = 60;
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key);
        }
        return result;
    }
}
