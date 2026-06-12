using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TauntList : MonoBehaviour
{
    private static TauntList instance;
    public static TauntList Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TauntList>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TauntList");
                    instance = go.AddComponent<TauntList>();
                }
            }
            return instance;
        }
    }

    [SerializeField]
    private string url = "https://script.google.com/macros/s/AKfycbzl1AJHTjE26XYuL7OaHuYQfIMccTHYXNJvnDEt2TCprjBBEvi4tLz7ZHUAI949PyZO/exec";

    public Taunt[] taunts;

    private readonly string[] fallbackTaunts = new string[]
    {
        "¡A casa pete!",
        "¡Demasiado lento para mí!",
        "¡Buen intento, para la próxima!",
        "¡Destruido por completo!",
        "¡Intenta esquivar esto!"
    };

    void Awake()
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

    public Taunt GetRandomTaunt()
    {
        if (taunts == null || taunts.Length == 0)
        {
            return new Taunt(Random.Range(1, 100), GetFallbackTaunt(Random.Range(0, 5)));
        }
        return taunts[Random.Range(0, taunts.Length)];
    }

    public void ObtenerTauntPorId(int id, System.Action<string> callback)
    {
        StartCoroutine(FetchTaunt(id, callback));
    }

    private IEnumerator FetchTaunt(int id, System.Action<string> callback)
    {
        string requestUrl = url + "?id=" + id;
        using (UnityWebRequest www = UnityWebRequest.Get(requestUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("Error al obtener taunt de API, usando fallback. Error: " + www.error);
                callback?.Invoke(GetFallbackTaunt(id));
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Taunt obtenido: " + jsonResponse);

                try
                {
                    string trimmed = jsonResponse != null ? jsonResponse.Trim() : "";
                    if (trimmed.StartsWith("["))
                    {
                        string wrappedJson = "{\"taunts\":" + trimmed + "}";
                        TauntListWrapper wrapper = JsonUtility.FromJson<TauntListWrapper>(wrappedJson);
                        if (wrapper != null && wrapper.taunts != null && wrapper.taunts.Count > 0)
                        {
                            Taunt found = wrapper.taunts.Find(t => t.id == id);
                            if (found != null && !string.IsNullOrEmpty(found.GetMessage()))
                            {
                                callback?.Invoke(found.GetMessage());
                            }
                            else
                            {
                                callback?.Invoke(GetFallbackTaunt(id));
                            }
                        }
                        else
                        {
                            callback?.Invoke(GetFallbackTaunt(id));
                        }
                    }
                    else if (trimmed.StartsWith("{"))
                    {
                        Taunt parsed = JsonUtility.FromJson<Taunt>(trimmed);
                        if (parsed != null && !string.IsNullOrEmpty(parsed.GetMessage()))
                        {
                            callback?.Invoke(parsed.GetMessage());
                        }
                        else
                        {
                            callback?.Invoke(GetFallbackTaunt(id));
                        }
                    }
                    else
                    {
                        Debug.LogWarning("La respuesta de la API no es un JSON válido. Usando fallback. Detalle del error: " + (trimmed.Length > 200 ? trimmed.Substring(0, 200) + "..." : trimmed));
                        callback?.Invoke(GetFallbackTaunt(id));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Error al parsear JSON del taunt, usando fallback. Error: " + e.Message);
                    callback?.Invoke(GetFallbackTaunt(id));
                }
            }
        }
    }

    private string GetFallbackTaunt(int id)
    {
        if (fallbackTaunts == null || fallbackTaunts.Length == 0)
            return "¡Burla!";

        int index = Mathf.Clamp(id % fallbackTaunts.Length, 0, fallbackTaunts.Length - 1);
        return fallbackTaunts[index];
    }
}

[System.Serializable]
public class TauntListWrapper
{
    public List<Taunt> taunts;
}