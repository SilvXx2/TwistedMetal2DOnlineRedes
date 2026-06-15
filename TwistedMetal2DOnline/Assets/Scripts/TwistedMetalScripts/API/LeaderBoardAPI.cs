using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class LeaderBoardAPI : MonoBehaviour
{
    private static LeaderBoardAPI instance;
    public static LeaderBoardAPI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LeaderBoardAPI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("LeaderBoardAPI");
                    instance = go.AddComponent<LeaderBoardAPI>();
                }
            }
            return instance;
        }
    }

    [SerializeField]
    private string url = "https://script.google.com/macros/s/AKfycbx66j_Fz7Iaqk5lYAwEqP3NthXOsheGQU7Z98NlYzbt2mzWIpaeCRL-OYrl9kLq0Fmc/exec";

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

    public void EnviarScore(string nombre, int score, string macId = null, System.Action<bool> callback = null)
    {
        if (string.IsNullOrEmpty(macId))
        {
            macId = GetMacAddress();
        }
        StartCoroutine(PostScore(nombre, score, macId, callback));
    }

    public void ObtenerScores(System.Action<List<ScoreData>> callback)
    {
        StartCoroutine(GetScores(callback));
    }

    private IEnumerator PostScore(string nombre, int score, string macId, System.Action<bool> callback)
    {
        string json = JsonUtility.ToJson(new ScoreData(nombre, score, macId));

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error al enviar score: " + www.error);
                callback?.Invoke(false);
            }
            else
            {
                Debug.Log("Score enviado con éxito: " + www.downloadHandler.text);
                callback?.Invoke(true);
            }
        }
    }

    private IEnumerator GetScores(System.Action<List<ScoreData>> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error al obtener scores: " + www.error);
                callback?.Invoke(null);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Scores obtenidos: " + jsonResponse);

                try
                {
                    ScoreListWrapper listWrapper = JsonUtility.FromJson<ScoreListWrapper>(jsonResponse);
                    callback?.Invoke(listWrapper.scores);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error al parsear JSON del leaderboard: " + e.Message);
                    callback?.Invoke(null);
                }
            }
        }
    }

    public string GetMacAddress()
    {
        try
        {
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                {
                    string mac = nic.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac))
                    {
                        return FormatMacAddress(mac);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al obtener la MAC ID: " + e.Message);
        }
        return "Unknown_MAC";
    }

    private string FormatMacAddress(string mac)
    {
        if (mac.Length != 12) return mac;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < mac.Length; i++)
        {
            sb.Append(mac[i]);
            if (i % 2 == 1 && i < mac.Length - 1)
            {
                sb.Append(":");
            }
        }
        return sb.ToString();
    }
}

[System.Serializable]
public class ScoreData
{
    public string nombre;
    public int score;
    public string macId;

    public ScoreData(string n, int s, string mac)
    {
        nombre = n;
        score = s;
        macId = mac;
    }
}

[System.Serializable]
public class ScoreListWrapper
{
    public List<ScoreData> scores;
}
