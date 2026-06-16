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
    private bool isLoaded = false;

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

    void Start()
    {
        StartCoroutine(FetchAllTaunts());
    }

    private IEnumerator FetchAllTaunts()
    {
        Debug.Log("[TauntList] Iniciando la precarga de todos los taunts desde la API...");
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("[TauntList] Error al precargar taunts, se usarán fallbacks. Error: " + www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("[TauntList] Respuesta JSON recibida: " + jsonResponse);

                try
                {
                    string trimmed = jsonResponse != null ? jsonResponse.Trim() : "";
                    if (trimmed.StartsWith("["))
                    {
                        string wrappedJson = "{\"taunts\":" + trimmed + "}";
                        TauntListWrapper wrapper = JsonUtility.FromJson<TauntListWrapper>(wrappedJson);
                        if (wrapper != null && wrapper.taunts != null)
                        {
                            taunts = wrapper.taunts.ToArray();
                            isLoaded = true;
                            Debug.Log($"[TauntList] Precargados con éxito {taunts.Length} taunts desde la API.");
                        }
                    }
                    else if (trimmed.StartsWith("{"))
                    {
                        Taunt parsed = JsonUtility.FromJson<Taunt>(trimmed);
                        if (parsed != null)
                        {
                            taunts = new Taunt[] { parsed };
                            isLoaded = true;
                            Debug.Log("[TauntList] Precargado 1 taunt desde la API.");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[TauntList] Error al parsear JSON precargado: " + e.Message);
                }
            }
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
        if (isLoaded && taunts != null && taunts.Length > 0)
        {
            Taunt found = System.Array.Find(taunts, t => t.id == id);
            if (found != null && !string.IsNullOrEmpty(found.GetMessage()))
            {
                callback?.Invoke(found.GetMessage());
                return;
            }
        }

        callback?.Invoke(GetFallbackTaunt(id));
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