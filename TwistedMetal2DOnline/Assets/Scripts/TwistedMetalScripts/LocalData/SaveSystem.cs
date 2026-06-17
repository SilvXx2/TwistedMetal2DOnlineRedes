using System.IO;
using System.Text;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "player_save.dat";
    private const string Key = "TwistedMetal_Save_Key";

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(LocalSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        string encryptedJson = EncryptDecrypt(json);

        File.WriteAllText(SavePath, encryptedJson);

        //System.Diagnostics.Process.Start(Application.persistentDataPath);

        Debug.Log("JSON original: " + json);
        Debug.Log("JSON encriptado: " + encryptedJson);
        Debug.Log("Ruta save: " + SavePath);
    }

    public static LocalSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new LocalSaveData();
        }

        string encryptedJson = File.ReadAllText(SavePath);
        string json = EncryptDecrypt(encryptedJson);

        Debug.Log("JSON desencriptado: " + json);

        return JsonUtility.FromJson<LocalSaveData>(json);
    }

    private static string EncryptDecrypt(string text)
    {
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            char c = (char)(text[i] ^ Key[i % Key.Length]);
            result.Append(c);
        }

        return result.ToString();
    }
}