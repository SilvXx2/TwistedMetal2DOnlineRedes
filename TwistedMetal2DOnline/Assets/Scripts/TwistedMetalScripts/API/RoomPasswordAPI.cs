using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class RoomPasswordAPI : MonoBehaviour
{
    private static RoomPasswordAPI instance;

    public static RoomPasswordAPI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RoomPasswordAPI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("RoomPasswordAPI");
                    instance = go.AddComponent<RoomPasswordAPI>();
                }
            }
            return instance;
        }
    }

    [SerializeField]
    private string url = "https://script.google.com/macros/s/AKfycbwH7_HTgqyBTPkQmsn5DY3evabsmeVKKspbC3GOQtf38bKt_XBt5954d3Jhe_wXlplKBQ/exec";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SetRoomPassword(string roomName, string plainPassword, System.Action<bool> callback = null)
    {
        if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(plainPassword))
        {
            Debug.LogWarning("[RoomPasswordAPI] roomName o password vacíos en SetRoomPassword.");
            callback?.Invoke(false);
            return;
        }

        string encryptedPassword = XorEncryptor.EncryptToBase64(plainPassword);
        string jsonPayload = BuildJsonPayload("setPassword", roomName, encryptedPassword);
        StartCoroutine(PostRequest(jsonPayload, callback));
    }

    public void ValidateRoomPassword(string roomName, string plainPassword, System.Action<bool> callback)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("[RoomPasswordAPI] roomName vacío en ValidateRoomPassword.");
            callback?.Invoke(false);
            return;
        }

        string encryptedPassword = XorEncryptor.EncryptToBase64(plainPassword ?? string.Empty);
        string jsonPayload = BuildJsonPayload("validatePassword", roomName, encryptedPassword);
        StartCoroutine(PostValidationRequest(jsonPayload, callback));
    }

    public void ClearRoomPassword(string roomName, System.Action<bool> callback = null)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("[RoomPasswordAPI] roomName vacío en ClearRoomPassword.");
            callback?.Invoke(false);
            return;
        }

        string jsonPayload = BuildJsonPayload("clearPassword", roomName, string.Empty);
        StartCoroutine(PostRequest(jsonPayload, callback));
    }

    private string BuildJsonPayload(string action, string roomName, string encryptedPassword)
    {
        RoomPasswordRequestPayload payload = new RoomPasswordRequestPayload
        {
            action = action,
            roomName = roomName,
            password = encryptedPassword
        };

        return JsonUtility.ToJson(payload);
    }

    private IEnumerator PostRequest(string jsonPayload, System.Action<bool> callback)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "text/plain");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("[RoomPasswordAPI] Error en request: " + www.error);
                callback?.Invoke(false);
            }
            else
            {
                Debug.Log("[RoomPasswordAPI] Respuesta: " + www.downloadHandler.text);
                RoomPasswordResponsePayload response = ParseResponse(www.downloadHandler.text);
                callback?.Invoke(response.success);
            }
        }
    }

    private IEnumerator PostValidationRequest(string jsonPayload, System.Action<bool> callback)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "text/plain");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("[RoomPasswordAPI] Error en validación: " + www.error);
                callback?.Invoke(false);
            }
            else
            {
                Debug.Log("[RoomPasswordAPI] Respuesta validación: " + www.downloadHandler.text);
                RoomPasswordResponsePayload response = ParseResponse(www.downloadHandler.text);
                callback?.Invoke(response.success && response.valid);
            }
        }
    }

    private static RoomPasswordResponsePayload ParseResponse(string json)
    {
        try
        {
            return JsonUtility.FromJson<RoomPasswordResponsePayload>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[RoomPasswordAPI] Error al parsear respuesta: " + e.Message);
            return new RoomPasswordResponsePayload { success = false, valid = false };
        }
    }
}

[System.Serializable]
internal struct RoomPasswordRequestPayload
{
    public string action;
    public string roomName;
    public string password;
}

[System.Serializable]
internal struct RoomPasswordResponsePayload
{
    public bool success;
    public bool valid;
}
