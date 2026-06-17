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

    [Header("Leaderboard/Scores UI")]
    [SerializeField] private TMPro.TMP_Text victoryScoreText;
    [SerializeField] private TMPro.TMP_Text defeatScoreText;

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

        // Update and show the scoreboard
        string scoreBoardText = GetScoreBoardText();
        if (victoryScoreText != null) victoryScoreText.text = scoreBoardText;
        if (defeatScoreText != null) defeatScoreText.text = scoreBoardText;

        RefreshRestartButtonInteractivity();
    }

    private string GetScoreBoardText()
    {
        if (!PhotonNetwork.InRoom)
        {
            return "Modo Offline - Sin puntuaciones.";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("TABLA DE POSICIONES");
        sb.AppendLine("-----------------------");

        System.Collections.Generic.List<Player> sortedPlayers = new System.Collections.Generic.List<Player>(PhotonNetwork.PlayerList);
        sortedPlayers.Sort((p1, p2) => {
            int score1 = p1.CustomProperties.ContainsKey("Score") ? (int)p1.CustomProperties["Score"] : 0;
            int score2 = p2.CustomProperties.ContainsKey("Score") ? (int)p2.CustomProperties["Score"] : 0;
            return score2.CompareTo(score1);
        });

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            Player p = sortedPlayers[i];
            string name = string.IsNullOrEmpty(p.NickName) ? $"Jugador {p.ActorNumber}" : p.NickName;
            int score = p.CustomProperties.ContainsKey("Score") ? (int)p.CustomProperties["Score"] : 0;
            int kills = p.CustomProperties.ContainsKey("Kills") ? (int)p.CustomProperties["Kills"] : 0;
            int deaths = p.CustomProperties.ContainsKey("Deaths") ? (int)p.CustomProperties["Deaths"] : 0;

            sb.AppendLine($"{i + 1}. {name}: {score} pts (Kills: {kills}, Deaths: {deaths})");
        }

        return sb.ToString();
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

    private void ShowMatchEndedState()
    {
        canvasView.ShowResult(false);
        string scoreBoardText = GetScoreBoardText();
        if (victoryScoreText != null) victoryScoreText.text = scoreBoardText;
        if (defeatScoreText != null) defeatScoreText.text = scoreBoardText;
        RefreshRestartButtonInteractivity();
    }

    private IEnumerator ResetCanvasAfterLoad()
    {
        yield return null;
        TryBindManagers();
        if (gameManager != null && gameManager.IsMatchEnded)
        {
            ShowMatchEndedState();
        }
        else
        {
            canvasView.ShowGameplayOnly();
        }
        RefreshRestartButtonInteractivity();
    }
}