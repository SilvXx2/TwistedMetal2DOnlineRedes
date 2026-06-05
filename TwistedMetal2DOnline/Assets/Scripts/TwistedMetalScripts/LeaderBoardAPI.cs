using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class LeaderBoardAPI : MonoBehaviour
{
    public static LeaderBoardAPI Instance { get; private set; }

    [SerializeField]
    private string url = "https://script.google.com/macros/s/AKfycbwoxHu4Gec3lG9LXOmTT6IJRuubRQBID2MP8lRMxl9SwtIWIN4S-Z-WPLHSpdj4f8-p/exec";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EnviarScore(string nombre, int score, System.Action<bool> callback = null)
    {
        StartCoroutine(PostScore(nombre, score, callback));
    }

    public void ObtenerScores(System.Action<List<ScoreData>> callback)
    {
        StartCoroutine(GetScores(callback));
    }

    private IEnumerator PostScore(string nombre, int score, System.Action<bool> callback)
    {
        string json = JsonUtility.ToJson(new ScoreData(nombre, score));

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
}

[System.Serializable]
public class ScoreData
{
    public string nombre;
    public int score;

    public ScoreData(string n, int s)
    {
        nombre = n;
        score = s;
    }
}

[System.Serializable]
public class ScoreListWrapper
{
    public List<ScoreData> scores;
}
