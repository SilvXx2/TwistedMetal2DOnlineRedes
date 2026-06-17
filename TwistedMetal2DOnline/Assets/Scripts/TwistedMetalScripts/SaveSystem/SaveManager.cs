using UnityEngine;
using System.IO;
using System.Text;

public class SaveManager
{
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SaveManager();
                instance.Load();
            }
            return instance;
        }
    }

    private readonly string saveFilePath;
    private const string SecretKey = "TwistedMetalSecretXorKey";

    public LocalSaveData CurrentSave { get; private set; }

    private SaveManager()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "player_save.dat");
        CurrentSave = new LocalSaveData();
    }

    /// <summary>
    /// Guarda el nickname serializándolo a JSON y encriptándolo con XOR byte-a-byte con clave rotativa.
    /// </summary>
    public void Save()
    {
        try
        {
            // 1. Serializar el perfil a JSON (Diapositiva 21)
            string json = JsonUtility.ToJson(CurrentSave);
            
            // 2. Convertir el texto JSON a bytes UTF-8 (Diapositiva 14/15)
            byte[] rawBytes = Encoding.UTF8.GetBytes(json);

            // 3. Cifrar los bytes mediante XOR rotativo (Diapositivas 31-40)
            byte[] encryptedBytes = EncryptDecrypt(rawBytes);

            // 4. Guardar los bytes crudos en disco
            File.WriteAllBytes(saveFilePath, encryptedBytes);
            Debug.Log($"[SaveManager] Perfil guardado exitosamente en: {saveFilePath}. Tamaño: {encryptedBytes.Length} bytes.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Error al guardar datos de sesión: {e.Message}");
        }
    }

    /// <summary>
    /// Carga el perfil, descifra los bytes usando XOR y deserializa el texto JSON de vuelta al objeto C#.
    /// </summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(saveFilePath))
            {
                Debug.Log("[SaveManager] No se encontró archivo de guardado previo. Inicializando perfil nuevo.");
                CurrentSave = new LocalSaveData();
                return;
            }

            // 1. Leer los bytes cifrados de disco
            byte[] encryptedBytes = File.ReadAllBytes(saveFilePath);

            // 2. Descifrar los bytes mediante XOR rotativo (mismo método por reflexividad)
            byte[] decryptedBytes = EncryptDecrypt(encryptedBytes);

            // 3. Convertir de bytes a string JSON
            string json = Encoding.UTF8.GetString(decryptedBytes);

            // 4. Deserializar el JSON al perfil actual (Diapositiva 21)
            CurrentSave = JsonUtility.FromJson<LocalSaveData>(json);
            if (CurrentSave == null)
            {
                CurrentSave = new LocalSaveData();
            }
            Debug.Log($"[SaveManager] Perfil cargado de sesión anterior: {CurrentSave.PlayerNickname}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Error al cargar datos de sesión: {e.Message}. Inicializando valores por defecto.");
            CurrentSave = new LocalSaveData();
        }
    }

    /// <summary>
    /// Aplica encriptación XOR byte por byte con clave rotativa de caracteres (Diapositivas 36-39).
    /// </summary>
    private byte[] EncryptDecrypt(byte[] data)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
        }
        return result;
    }
}
