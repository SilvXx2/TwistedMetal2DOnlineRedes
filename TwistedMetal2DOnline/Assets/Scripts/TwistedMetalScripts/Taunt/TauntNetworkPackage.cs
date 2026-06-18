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
