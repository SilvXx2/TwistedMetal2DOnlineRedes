using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnityLoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class GameConditionManager : MonoBehaviourPunCallbacks
{
    [Header("Canvases")]
    [SerializeField] private GameObject gameplayCanvas;
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private GameObject defeatCanvas;

    [Header("Botones")]
    [SerializeField] private Button[] restartMatchButtons;

    private GameManager gameManager;
    private PhotonManager photonManager;
    private MatchResultCanvasView canvasView;
    private RestartMatchButtonsController restartButtonsController;

    private void Awake()
    {
        canvasView = new MatchResultCanvasView(gameplayCanvas, victoryCanvas, defeatCanvas);
        restartButtonsController = new RestartMatchButtonsController(
            restartMatchButtons,
            this,
            nameof(OnClickRestartMatch),
            OnClickRestartMatch);

        UnitySceneManager.sceneLoaded += OnUnitySceneLoaded;
    }

    private void OnEnable()
    {
        Debug.Log("[GameConditionManager] OnEnable");
        restartButtonsController.SetListeners(true);
        TryBindManagers();
        canvasView.ShowGameplayOnly();
        RefreshRestartButtonInteractivity();
    }

    private void OnDisable()
    {
        restartButtonsController.SetListeners(false);
        UnbindGameManager();
    }

    private void Update()
    {
        if (gameManager != null && photonManager != null) return;

        TryBindManagers();
        RefreshRestartButtonInteractivity();
    }

    private void OnDestroy()
    {
        UnitySceneManager.sceneLoaded -= OnUnitySceneLoaded;
    }

    public override void OnJoinedRoom()          => RefreshRestartButtonInteractivity();
    public override void OnLeftRoom()            => RefreshRestartButtonInteractivity();
    public override void OnMasterClientSwitched(Player newMasterClient) => RefreshRestartButtonInteractivity();

    private void OnUnitySceneLoaded(UnityScene scene, UnityLoadSceneMode mode)
    {
        StartCoroutine(ResetCanvasAfterLoad());
    }

    public void OnClickRestartMatch()
    {
        TryBindManagers();

        if (photonManager == null) return;

        photonManager.RequestRestartGame();
        RefreshRestartButtonInteractivity();
    }

    private void TryBindManagers()
    {
        if (photonManager == null)
            photonManager = PhotonManager.Instance;

        if (gameManager == GameManager.Instance && gameManager != null) return;

        UnbindGameManager();
        gameManager = GameManager.Instance;

        if (gameManager == null) return;

        gameManager.LocalMatchResultResolved += OnLocalMatchResultResolved;
        gameManager.MatchStateReset          += OnMatchStateReset;
        Debug.Log($"[GameConditionManager] TryBindManagers - gameManager:{gameManager != null} photonManager:{photonManager != null}");
    }

    private void UnbindGameManager()
    {
        if (gameManager == null) return;

        gameManager.LocalMatchResultResolved -= OnLocalMatchResultResolved;
        gameManager.MatchStateReset          -= OnMatchStateReset;
        gameManager = null;
    }

    private void OnLocalMatchResultResolved(bool localPlayerWon)
    {
        Debug.Log($"[GameConditionManager] OnLocalMatchResultResolved - won:{localPlayerWon}");
        canvasView.ShowResult(localPlayerWon);
        RefreshRestartButtonInteractivity();
    }

    private void OnMatchStateReset()
    {
        Debug.Log("[GameConditionManager] OnMatchStateReset recibido");
        canvasView.ShowGameplayOnly();
        RefreshRestartButtonInteractivity();
    }

    private void RefreshRestartButtonInteractivity()
    {
        restartButtonsController.SetInteractable(CanRestartMatch());
    }

    private bool CanRestartMatch()
    {
        return photonManager != null
            && photonManager.IsInRoom
            && photonManager.IsMasterClient;
    }

    private IEnumerator ResetCanvasAfterLoad()
    {
        yield return null;
        TryBindManagers();
        canvasView.ShowGameplayOnly();
        RefreshRestartButtonInteractivity();
    }
}