using System.IO;

[System.Serializable]
public struct RoomPasswordNetworkPackage
{
    public string roomName;
    public string encryptedPassword;
    public string action;

    public RoomPasswordNetworkPackage(string roomName, string encryptedPassword, string action)
    {
        this.roomName = roomName;
        this.encryptedPassword = encryptedPassword;
        this.action = action;
    }

    public byte[] Serialize()
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(roomName ?? string.Empty);
                writer.Write(encryptedPassword ?? string.Empty);
                writer.Write(action ?? string.Empty);
            }
            return ms.ToArray();
        }
    }

    public static RoomPasswordNetworkPackage Deserialize(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(ms))
            {
                string roomName = reader.ReadString();
                string encryptedPassword = reader.ReadString();
                string action = reader.ReadString();
                return new RoomPasswordNetworkPackage(roomName, encryptedPassword, action);
            }
        }
    }
}
