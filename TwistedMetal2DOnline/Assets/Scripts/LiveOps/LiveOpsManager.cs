using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class LiveOpsManager : MonoBehaviour
{
    public static LiveOpsManager Instance { get; private set; }

    public bool IsReady { get; private set; }
    public GameConfig Config { get; private set; }
    public MapPartitionConfig MapConfig { get; private set; }

    public event Action OnLiveOpsReady;
    public event Action<string> OnLiveOpsFailed;

    private struct UserAttributes { }
    private struct AppAttributes { }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Config = GameConfig.Default;
        IsReady = false;
    }

    private async void Start()
    {
        await InitializeLiveOpsAsync();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private async Task InitializeLiveOpsAsync()
    {
        try
        {
            Debug.Log("[LiveOpsManager] Inicializando Unity Gaming Services...");
            
            // Timeout de 4 segundos para todo el proceso de inicialización de LiveOps
            Task initTask = DoInitializeLiveOpsAsync();
            Task delayTask = Task.Delay(4000);
            
            Task completedTask = await Task.WhenAny(initTask, delayTask);
            if (completedTask == delayTask)
            {
                throw new TimeoutException("La inicialización de LiveOps superó el límite de tiempo.");
            }

            // Await the task to propagate any exceptions (e.g. initialization or connection failure)
            await initTask;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LiveOpsManager] Error/Timeout al inicializar LiveOps: {e.Message}. Usando configuración por defecto.");
            OnLiveOpsFailed?.Invoke(e.Message);

            IsReady = true;
            OnLiveOpsReady?.Invoke();
        }
    }

    private async Task DoInitializeLiveOpsAsync()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[LiveOpsManager] Autenticando anónimamente...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[LiveOpsManager] Autenticado. PlayerID: {AuthenticationService.Instance.PlayerId}");
        }

        RemoteConfigService.Instance.FetchCompleted += OnRemoteConfigFetchCompleted;

        Debug.Log("[LiveOpsManager] Descargando Remote Config...");
        await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
    }

    private void OnRemoteConfigFetchCompleted(ConfigResponse response)
    {
        RemoteConfigService.Instance.FetchCompleted -= OnRemoteConfigFetchCompleted;

        switch (response.requestOrigin)
        {
            case ConfigOrigin.Default:
                Debug.Log("[LiveOpsManager] Remote Config: usando valores por defecto del servidor (sin conexión previa).");
                break;
            case ConfigOrigin.Cached:
                Debug.Log("[LiveOpsManager] Remote Config: usando caché local.");
                break;
            case ConfigOrigin.Remote:
                Debug.Log("[LiveOpsManager] Remote Config: descargado desde servidor exitosamente.");
                break;
        }

        ParseConfig();
        IsReady = true;
        OnLiveOpsReady?.Invoke();
    }

    private void ParseConfig()
    {
        GameConfig defaults = GameConfig.Default;

        Config = new GameConfig
        {
            PlayerMaxHealth       = RemoteConfigService.Instance.appConfig.GetInt("player_max_health",       defaults.PlayerMaxHealth),
            CarMoveSpeed          = RemoteConfigService.Instance.appConfig.GetFloat("car_move_speed",        defaults.CarMoveSpeed),
            CarTurnSpeed          = RemoteConfigService.Instance.appConfig.GetFloat("car_turn_speed",        defaults.CarTurnSpeed),
            PickupSpawnInterval   = RemoteConfigService.Instance.appConfig.GetFloat("pickup_spawn_interval", defaults.PickupSpawnInterval),
            MatchGracePeriod      = RemoteConfigService.Instance.appConfig.GetFloat("match_grace_period",    defaults.MatchGracePeriod),
            EventModeActive       = RemoteConfigService.Instance.appConfig.GetBool("event_mode_active",      defaults.EventModeActive),
            EventModeName         = RemoteConfigService.Instance.appConfig.GetString("event_mode_name",      defaults.EventModeName),
            ActiveMapPartitionJson = RemoteConfigService.Instance.appConfig.GetJson("active_map_partition",  defaults.ActiveMapPartitionJson)
        };

        MapConfig = MapPartitionConfig.TryParse(Config.ActiveMapPartitionJson);

        Debug.Log($"[LiveOpsManager] Config cargada — " +
                  $"HP:{Config.PlayerMaxHealth} Speed:{Config.CarMoveSpeed} Turn:{Config.CarTurnSpeed} " +
                  $"Event:{Config.EventModeActive} Mapa:{MapConfig?.partitionId ?? "default"}");
    }

    public async void RefreshConfig()
    {
        if (!IsReady)
        {
            Debug.LogWarning("[LiveOpsManager] RefreshConfig ignorado: aún no está listo.");
            return;
        }

        try
        {
            IsReady = false;
            RemoteConfigService.Instance.FetchCompleted += OnRemoteConfigFetchCompleted;
            await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LiveOpsManager] Error al refrescar config: {e.Message}");
            IsReady = true;
        }
    }
}
