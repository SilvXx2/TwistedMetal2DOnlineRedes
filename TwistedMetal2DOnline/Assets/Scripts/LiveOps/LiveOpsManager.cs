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
    }

    private async void Start() => await InitializeLiveOpsAsync();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private async Task InitializeLiveOpsAsync()
    {
        try
        {
            Debug.Log("[LiveOpsManager] Inicializando Unity Gaming Services...");

            Task initTask = DoInitializeLiveOpsAsync();
            if (await Task.WhenAny(initTask, Task.Delay(4000)) != initTask)
                throw new TimeoutException("La inicialización de LiveOps superó el límite de tiempo.");

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
        ParseConfig();
        IsReady = true;
        OnLiveOpsReady?.Invoke();
    }

    private void ParseConfig()
    {
        GameConfig defaults = GameConfig.Default;
        var cfg = RemoteConfigService.Instance.appConfig;

        Config = new GameConfig
        {
            PlayerMaxHealth        = cfg.GetInt("player_max_health",         defaults.PlayerMaxHealth),
            CarMoveSpeed           = cfg.GetFloat("car_move_speed",          defaults.CarMoveSpeed),
            CarTurnSpeed           = cfg.GetFloat("car_turn_speed",          defaults.CarTurnSpeed),
            PickupSpawnInterval    = cfg.GetFloat("pickup_spawn_interval",   defaults.PickupSpawnInterval),
            MatchGracePeriod       = cfg.GetFloat("match_grace_period",      defaults.MatchGracePeriod),
            EventModeActive        = cfg.GetBool("event_mode_active",        defaults.EventModeActive),
            EventModeName          = cfg.GetString("event_mode_name",        defaults.EventModeName),
            ActiveMapPartitionJson = cfg.GetJson("active_map_partition",     defaults.ActiveMapPartitionJson)
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