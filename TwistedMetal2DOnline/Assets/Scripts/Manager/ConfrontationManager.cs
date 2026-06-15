using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public enum ConfrontationState
{
    Idle,
    Active,
    Resolving
}

public class ConfrontationManager : MonoBehaviour
{
    public static ConfrontationManager Instance { get; private set; }

    [Header("Confrontation Settings")]
    [SerializeField] private float maxDuration = 2.0f;
    [SerializeField] private float separationDistance = 1.6f;
    [SerializeField] private float oscillationSpeed = 5.0f;
    [SerializeField] private int confrontationDamage = 40;
    [SerializeField] private GameObject pedestrianPrefab;

    private ConfrontationState state = ConfrontationState.Idle;
    private CarController localCar;
    private CarController rivalCar;
    private CarController participantA;
    private CarController participantB;

    private float timer;
    private float localScore = -1f;
    private float rivalScore = -1f;
    private bool localLocked;
    private bool rivalLocked;
    private bool localPressedSpace;
    private bool rivalPressedSpace;

    private float localPhaseOffset;
    private float rivalPhaseOffset;

    // UI elements
    private GameObject uiCanvasGo;
    private Image localBarFill;
    private Image rivalBarFill;
    private TMPro.TextMeshProUGUI titleText;
    private TMPro.TextMeshProUGUI timerText;
    private TMPro.TextMeshProUGUI localStatusText;
    private TMPro.TextMeshProUGUI rivalStatusText;

    private GameObject dummyCamTarget;

    public bool IsActive => state != ConfrontationState.Idle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ConfrontationManager");
            go.AddComponent<ConfrontationManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Fallback if reference was lost or dynamically instantiated
        if (pedestrianPrefab == null)
        {
            pedestrianPrefab = Resources.Load<GameObject>("Pedestrian");
            if (pedestrianPrefab == null)
            {
                Debug.LogWarning("[ConfrontationManager] Pedestrian prefab could not be loaded from Resources!");
            }
        }
    }

    public void StartConfrontation(CarController carA, CarController carB)
    {
        if (state != ConfrontationState.Idle) return;

        state = ConfrontationState.Active;
        localScore = -1f;
        rivalScore = -1f;
        localLocked = false;
        rivalLocked = false;
        localPressedSpace = false;
        rivalPressedSpace = false;
        timer = maxDuration;

        participantA = carA;
        participantB = carB;

        // Generate random phase offsets for bar oscillation
        localPhaseOffset = Random.Range(0f, 2f * Mathf.PI);
        rivalPhaseOffset = Random.Range(0f, 2f * Mathf.PI);

        // Determine local vs rival
        if (carA.photonView != null && carA.photonView.IsMine)
        {
            localCar = carA;
            rivalCar = carB;
        }
        else if (carB.photonView != null && carB.photonView.IsMine)
        {
            localCar = carB;
            rivalCar = carA;
        }
        else
        {
            localCar = null;
            rivalCar = null;
        }

        // Set state on both cars to disable standard movement/rotation
        carA.IsInConfrontation = true;
        carB.IsInConfrontation = true;

        // Calculate positions
        Vector3 posA = carA.transform.position;
        Vector3 posB = carB.transform.position;
        Vector3 midpoint = (posA + posB) / 2f;
        Vector3 dir = (posB - posA).normalized;

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = carA.transform.right;
        }

        // Reposition and orient face-to-face
        carA.transform.position = midpoint - dir * separationDistance;
        carB.transform.position = midpoint + dir * separationDistance;
        carA.transform.right = dir;
        carB.transform.right = -dir;

        // Camera follow target
        dummyCamTarget = new GameObject("ConfrontationCamTarget");
        dummyCamTarget.transform.position = midpoint;
        CameraFollow camFollow = FindObjectOfType<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.SetTarget(dummyCamTarget.transform);
        }

        // Create UI if participant
        if (localCar != null)
        {
            string localName = localCar.photonView != null && localCar.photonView.Owner != null
                ? localCar.photonView.Owner.NickName
                : "Tú";
            string rivalName = rivalCar.photonView != null && rivalCar.photonView.Owner != null
                ? rivalCar.photonView.Owner.NickName
                : "Rival";

            CreateConfrontationUI(localName, rivalName);
        }
    }

    private void CreateConfrontationUI(string localName, string rivalName)
    {
        if (uiCanvasGo != null)
        {
            Destroy(uiCanvasGo);
        }

        uiCanvasGo = new GameObject("ConfrontationUICanvas");
        Canvas canvas = uiCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        CanvasScaler scaler = uiCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        uiCanvasGo.AddComponent<GraphicRaycaster>();

        // Dark Overlay
        GameObject overlayGo = new GameObject("DarkOverlay");
        overlayGo.transform.SetParent(uiCanvasGo.transform, false);
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.5f);
        RectTransform overlayRt = overlayGo.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        // Central Panel
        GameObject panelGo = new GameObject("CenterPanel");
        panelGo.transform.SetParent(uiCanvasGo.transform, false);
        Image panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(800, 360);
        panelRt.anchoredPosition = Vector2.zero;

        // Top Border Highlight
        GameObject topBar = new GameObject("TopBorder");
        topBar.transform.SetParent(panelGo.transform, false);
        Image topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = new Color(1f, 0.3f, 0f, 1f);
        RectTransform topBarRt = topBar.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0f, 1f);
        topBarRt.anchorMax = new Vector2(1f, 1f);
        topBarRt.pivot = new Vector2(0.5f, 1f);
        topBarRt.anchoredPosition = Vector2.zero;
        topBarRt.sizeDelta = new Vector2(0, 6);

        // Title Text
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(panelGo.transform, false);
        titleText = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "¡CONFRONTACIÓN!";
        titleText.fontSize = 44;
        titleText.alignment = TMPro.TextAlignmentOptions.Center;
        titleText.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
        titleText.color = new Color(1f, 0.35f, 0f);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 110);
        titleRt.sizeDelta = new Vector2(700, 60);

        // Subtitle Hint
        GameObject hintGo = new GameObject("HintText");
        hintGo.transform.SetParent(panelGo.transform, false);
        TMPro.TextMeshProUGUI hintText = hintGo.AddComponent<TMPro.TextMeshProUGUI>();
        hintText.text = "¡PRESIONA ESPACIO PARA DETENER TU BARRA EN EL MÁXIMO!";
        hintText.fontSize = 18;
        hintText.alignment = TMPro.TextAlignmentOptions.Center;
        hintText.color = Color.white;
        RectTransform hintRt = hintGo.GetComponent<RectTransform>();
        hintRt.anchoredPosition = new Vector2(0, 65);
        hintRt.sizeDelta = new Vector2(700, 30);

        // Local Player UI
        GameObject localNameGo = new GameObject("LocalNameText");
        localNameGo.transform.SetParent(panelGo.transform, false);
        TMPro.TextMeshProUGUI localNameTxt = localNameGo.AddComponent<TMPro.TextMeshProUGUI>();
        localNameTxt.text = string.IsNullOrEmpty(localName) ? "TÚ" : localName;
        localNameTxt.fontSize = 22;
        localNameTxt.alignment = TMPro.TextAlignmentOptions.Left;
        localNameTxt.color = new Color(0f, 0.8f, 1f);
        RectTransform localNameRt = localNameGo.GetComponent<RectTransform>();
        localNameRt.anchoredPosition = new Vector2(-220, 10);
        localNameRt.sizeDelta = new Vector2(250, 30);

        GameObject localBarBg = new GameObject("LocalBarBg");
        localBarBg.transform.SetParent(panelGo.transform, false);
        Image localBarBgImg = localBarBg.AddComponent<Image>();
        localBarBgImg.color = new Color(0.15f, 0.15f, 0.18f, 1f);
        RectTransform localBarBgRt = localBarBg.GetComponent<RectTransform>();
        localBarBgRt.anchoredPosition = new Vector2(-220, -35);
        localBarBgRt.sizeDelta = new Vector2(250, 40);

        GameObject localBarFillGo = new GameObject("LocalBarFill");
        localBarFillGo.transform.SetParent(localBarBg.transform, false);
        localBarFill = localBarFillGo.AddComponent<Image>();
        localBarFill.color = new Color(0f, 0.75f, 1f);
        localBarFill.type = Image.Type.Filled;
        localBarFill.fillMethod = Image.FillMethod.Horizontal;
        localBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        RectTransform localBarFillRt = localBarFillGo.GetComponent<RectTransform>();
        localBarFillRt.anchorMin = Vector2.zero;
        localBarFillRt.anchorMax = Vector2.one;
        localBarFillRt.sizeDelta = Vector2.zero;

        GameObject localStatusGo = new GameObject("LocalStatusText");
        localStatusGo.transform.SetParent(panelGo.transform, false);
        localStatusText = localStatusGo.AddComponent<TMPro.TextMeshProUGUI>();
        localStatusText.text = "¡PRESIONA ESPACIO!";
        localStatusText.fontSize = 15;
        localStatusText.alignment = TMPro.TextAlignmentOptions.Left;
        localStatusText.color = Color.gray;
        RectTransform localStatusRt = localStatusGo.GetComponent<RectTransform>();
        localStatusRt.anchoredPosition = new Vector2(-220, -70);
        localStatusRt.sizeDelta = new Vector2(250, 25);

        // Rival Player UI
        GameObject rivalNameGo = new GameObject("RivalNameText");
        rivalNameGo.transform.SetParent(panelGo.transform, false);
        TMPro.TextMeshProUGUI rivalNameTxt = rivalNameGo.AddComponent<TMPro.TextMeshProUGUI>();
        rivalNameTxt.text = string.IsNullOrEmpty(rivalName) ? "RIVAL" : rivalName;
        rivalNameTxt.fontSize = 22;
        rivalNameTxt.alignment = TMPro.TextAlignmentOptions.Right;
        rivalNameTxt.color = new Color(1f, 0.25f, 0.1f);
        RectTransform rivalNameRt = rivalNameGo.GetComponent<RectTransform>();
        rivalNameRt.anchoredPosition = new Vector2(220, 10);
        rivalNameRt.sizeDelta = new Vector2(250, 30);

        GameObject rivalBarBg = new GameObject("RivalBarBg");
        rivalBarBg.transform.SetParent(panelGo.transform, false);
        Image rivalBarBgImg = rivalBarBg.AddComponent<Image>();
        rivalBarBgImg.color = new Color(0.15f, 0.15f, 0.18f, 1f);
        RectTransform rivalBarBgRt = rivalBarBg.GetComponent<RectTransform>();
        rivalBarBgRt.anchoredPosition = new Vector2(220, -35);
        rivalBarBgRt.sizeDelta = new Vector2(250, 40);

        GameObject rivalBarFillGo = new GameObject("RivalBarFill");
        rivalBarFillGo.transform.SetParent(rivalBarBg.transform, false);
        rivalBarFill = rivalBarFillGo.AddComponent<Image>();
        rivalBarFill.color = new Color(1f, 0.25f, 0.1f);
        rivalBarFill.type = Image.Type.Filled;
        rivalBarFill.fillMethod = Image.FillMethod.Horizontal;
        rivalBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        RectTransform rivalBarFillRt = rivalBarFillGo.GetComponent<RectTransform>();
        rivalBarFillRt.anchorMin = Vector2.zero;
        rivalBarFillRt.anchorMax = Vector2.one;
        rivalBarFillRt.sizeDelta = Vector2.zero;

        GameObject rivalStatusGo = new GameObject("RivalStatusText");
        rivalStatusGo.transform.SetParent(panelGo.transform, false);
        rivalStatusText = rivalStatusGo.AddComponent<TMPro.TextMeshProUGUI>();
        rivalStatusText.text = "JUGANDO...";
        rivalStatusText.fontSize = 15;
        rivalStatusText.alignment = TMPro.TextAlignmentOptions.Right;
        rivalStatusText.color = Color.gray;
        RectTransform rivalStatusRt = rivalStatusGo.GetComponent<RectTransform>();
        rivalStatusRt.anchoredPosition = new Vector2(220, -70);
        rivalStatusRt.sizeDelta = new Vector2(250, 25);

        // VS Badge
        GameObject vsGo = new GameObject("VSBadge");
        vsGo.transform.SetParent(panelGo.transform, false);
        TMPro.TextMeshProUGUI vsText = vsGo.AddComponent<TMPro.TextMeshProUGUI>();
        vsText.text = "VS";
        vsText.fontSize = 32;
        vsText.alignment = TMPro.TextAlignmentOptions.Center;
        vsText.fontStyle = TMPro.FontStyles.Bold;
        vsText.color = new Color(1f, 0.85f, 0f);
        RectTransform vsRt = vsGo.GetComponent<RectTransform>();
        vsRt.anchoredPosition = new Vector2(0, -35);
        vsRt.sizeDelta = new Vector2(80, 50);

        // Timer
        GameObject timerGo = new GameObject("TimerText");
        timerGo.transform.SetParent(panelGo.transform, false);
        timerText = timerGo.AddComponent<TMPro.TextMeshProUGUI>();
        timerText.text = "2.0s";
        timerText.fontSize = 28;
        timerText.alignment = TMPro.TextAlignmentOptions.Center;
        timerText.color = Color.white;
        RectTransform timerRt = timerGo.GetComponent<RectTransform>();
        timerRt.anchoredPosition = new Vector2(0, -125);
        timerRt.sizeDelta = new Vector2(200, 40);
    }

    private void Update()
    {
        if (state == ConfrontationState.Idle) return;

        // Auto abort if participant cars are destroyed
        if (participantA == null || participantB == null)
        {
            AbortConfrontation();
            return;
        }

        if (state == ConfrontationState.Active)
        {
            timer -= Time.deltaTime;
            if (timer < 0f) timer = 0f;

            if (timerText != null)
            {
                timerText.text = timer.ToString("F1") + "s";
                if (timer < 0.5f)
                {
                    timerText.color = Color.red;
                }
            }

            // Animate title pulse
            if (titleText != null)
            {
                float pulse = 1f + Mathf.PingPong(Time.time * 2f, 0.08f);
                titleText.transform.localScale = new Vector3(pulse, pulse, 1f);
            }

            // If we are participant
            if (localCar != null)
            {
                // Local Bar Oscillation
                if (!localLocked)
                {
                    float localFill = Mathf.PingPong(Time.time * oscillationSpeed + localPhaseOffset, 1.0f);
                    if (localBarFill != null)
                    {
                        localBarFill.fillAmount = localFill;
                    }
                    if (localStatusText != null)
                    {
                        localStatusText.text = "¡ESPACIO! (" + (localFill * 100f).ToString("F0") + "%)";
                    }

                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        localScore = localFill;
                        localLocked = true;
                        localPressedSpace = true;
                        
                        if (localBarFill != null)
                        {
                            localBarFill.color = new Color(1f, 0.85f, 0f);
                        }
                        if (localStatusText != null)
                        {
                            localStatusText.text = "LOCKED (" + (localScore * 100f).ToString("F0") + "%)";
                            localStatusText.color = Color.white;
                        }

                        localCar.SendConfrontationScore(localScore, true);
                    }
                }

                // Rival Bar Oscillation (simulated locally until actual score received)
                if (!rivalLocked)
                {
                    float rivalFill = Mathf.PingPong(Time.time * (oscillationSpeed * 0.95f) + rivalPhaseOffset, 1.0f);
                    if (rivalBarFill != null)
                    {
                        rivalBarFill.fillAmount = rivalFill;
                    }
                }

                // Check transition to resolution
                if ((localLocked && rivalLocked) || timer <= 0f)
                {
                    if (!localLocked)
                    {
                        localScore = localBarFill != null ? localBarFill.fillAmount : 0f;
                        localPressedSpace = false;
                        localLocked = true;
                        localCar.SendConfrontationScore(localScore, false);
                        if (localBarFill != null) localBarFill.color = Color.gray;
                        if (localStatusText != null)
                        {
                            localStatusText.text = "TIMEOUT (" + (localScore * 100f).ToString("F0") + "%)";
                            localStatusText.color = Color.red;
                        }
                    }

                    StartCoroutine(ResolveConfrontationRoutine());
                }
            }
            else
            {
                // Spectator
                if (timer <= 0f)
                {
                    StartCoroutine(ResolveConfrontationRoutine());
                }
            }
        }
    }

    public void OnScoreSubmitted(int viewId, float score, bool pressedSpace)
    {
        if (state != ConfrontationState.Active) return;

        if (localCar != null && localCar.photonView != null && localCar.photonView.ViewID == viewId)
        {
            localScore = score;
            localPressedSpace = pressedSpace;
            localLocked = true;
            if (localBarFill != null)
            {
                localBarFill.fillAmount = score;
                localBarFill.color = pressedSpace ? new Color(1f, 0.85f, 0f) : Color.gray;
            }
            if (localStatusText != null)
            {
                if (pressedSpace)
                {
                    localStatusText.text = "LOCKED (" + (score * 100f).ToString("F0") + "%)";
                    localStatusText.color = Color.white;
                }
                else
                {
                    localStatusText.text = "TIMEOUT (" + (score * 100f).ToString("F0") + "%)";
                    localStatusText.color = Color.red;
                }
            }
        }
        else if (rivalCar != null && rivalCar.photonView != null && rivalCar.photonView.ViewID == viewId)
        {
            rivalScore = score;
            rivalPressedSpace = pressedSpace;
            rivalLocked = true;
            if (rivalBarFill != null)
            {
                rivalBarFill.fillAmount = score;
                rivalBarFill.color = pressedSpace ? new Color(0.7f, 0.7f, 0.7f) : Color.gray;
            }
            if (rivalStatusText != null)
            {
                if (pressedSpace)
                {
                    rivalStatusText.text = "¡LISTO! (" + (score * 100f).ToString("F0") + "%)";
                    rivalStatusText.color = Color.white;
                }
                else
                {
                    rivalStatusText.text = "TIMEOUT (" + (score * 100f).ToString("F0") + "%)";
                    rivalStatusText.color = Color.red;
                }
            }
        }
    }

    private IEnumerator ResolveConfrontationRoutine()
    {
        state = ConfrontationState.Resolving;

        // Grace period for network delay
        if (localCar != null && !rivalLocked)
        {
            float waitTimer = 0.5f;
            while (waitTimer > 0f && !rivalLocked)
            {
                waitTimer -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!rivalLocked)
            {
                rivalScore = 0f;
                rivalPressedSpace = false;
                rivalLocked = true;
                if (rivalBarFill != null)
                {
                    rivalBarFill.fillAmount = 0f;
                    rivalBarFill.color = Color.gray;
                }
                if (rivalStatusText != null)
                {
                    rivalStatusText.text = "TIMEOUT (0%)";
                    rivalStatusText.color = Color.red;
                }
            }
        }

        if (localCar != null)
        {
            bool localWon = false;
            bool bothLost = false;

            if (!localPressedSpace && !rivalPressedSpace)
            {
                bothLost = true;
            }
            else if (localPressedSpace && !rivalPressedSpace)
            {
                localWon = true;
            }
            else if (!localPressedSpace && rivalPressedSpace)
            {
                localWon = false;
            }
            else
            {
                if (localScore == rivalScore)
                {
                    localScore += 0.001f; // tie breaker
                }
                localWon = localScore > rivalScore;
            }

            if (bothLost)
            {
                if (titleText != null)
                {
                    titleText.text = "¡AMBOS PIERDEN!";
                    titleText.color = Color.red;
                }
                if (localStatusText != null)
                {
                    localStatusText.text = "TIMEOUT - PERDEDOR";
                    localStatusText.color = Color.red;
                }
                if (rivalStatusText != null)
                {
                    rivalStatusText.text = "TIMEOUT - PERDEDOR";
                    rivalStatusText.color = Color.red;
                }

                ApplyDoubleLoss(localCar, rivalCar);
            }
            else if (localWon)
            {
                if (titleText != null)
                {
                    titleText.text = "¡VICTORIA!";
                    titleText.color = Color.green;
                }
                if (localStatusText != null)
                {
                    localStatusText.text = "¡GANADOR! (" + (localScore * 100f).ToString("F0") + "%)";
                    localStatusText.color = Color.green;
                }
                if (rivalStatusText != null)
                {
                    string pct = rivalPressedSpace ? " (" + (rivalScore * 100f).ToString("F0") + "%)" : "";
                    rivalStatusText.text = "PERDEDOR" + pct;
                    rivalStatusText.color = Color.red;
                }

                ApplyRewardsAndPunishments(localCar, rivalCar);
            }
            else
            {
                if (titleText != null)
                {
                    titleText.text = "¡DERROTA!";
                    titleText.color = Color.red;
                }
                if (localStatusText != null)
                {
                    string pct = localPressedSpace ? " (" + (localScore * 100f).ToString("F0") + "%)" : "";
                    localStatusText.text = "PERDEDOR" + pct;
                    localStatusText.color = Color.red;
                }
                if (rivalStatusText != null)
                {
                    rivalStatusText.text = "¡GANADOR! (" + (rivalScore * 100f).ToString("F0") + "%)";
                    rivalStatusText.color = Color.green;
                }

                ApplyRewardsAndPunishments(rivalCar, localCar);
            }
        }

        yield return new WaitForSeconds(1.5f);

        EndConfrontation();
    }

    private void ApplyRewardsAndPunishments(CarController winner, CarController loser)
    {
        if (winner != null && winner.photonView != null)
        {
            NitroSystem winNitro = winner.GetComponent<NitroSystem>();
            if (winNitro != null)
            {
                if (PhotonNetwork.InRoom)
                    winNitro.photonView.RPC("FullNitroRecharge", RpcTarget.All);
                else
                    winNitro.FullNitroRecharge();
            }

            PlayerHealth winHealth = winner.GetComponent<PlayerHealth>();
            if (winHealth != null)
            {
                if (PhotonNetwork.InRoom)
                    winHealth.photonView.RPC("SetPowerUpInvulnerability", RpcTarget.All, 2.0f);
                else
                    winHealth.SetPowerUpInvulnerability(2.0f);
            }
        }

        if (loser != null)
        {
            bool isLoserMine = (loser.photonView == null) || loser.photonView.IsMine;
            if (isLoserMine)
            {
                loser.TransformToPedestrian(pedestrianPrefab);
            }
        }
    }

    private void ApplyDoubleLoss(CarController carA, CarController carB)
    {
        if (carA != null)
        {
            bool isCarAMine = (carA.photonView == null) || carA.photonView.IsMine;
            if (isCarAMine)
            {
                carA.TransformToPedestrian(pedestrianPrefab);
            }
        }
        if (carB != null)
        {
            bool isCarBMine = (carB.photonView == null) || carB.photonView.IsMine;
            if (isCarBMine)
            {
                carB.TransformToPedestrian(pedestrianPrefab);
            }
        }
    }

    private void EndConfrontation()
    {
        if (state == ConfrontationState.Idle) return;

        if (participantA != null)
        {
            participantA.IsInConfrontation = false;
        }
        if (participantB != null)
        {
            participantB.IsInConfrontation = false;
        }

        CameraFollow camFollow = FindObjectOfType<CameraFollow>();
        if (camFollow != null)
        {
            if (dummyCamTarget != null && camFollow.Target == dummyCamTarget.transform)
            {
                camFollow.SetTarget(null);
            }
        }

        if (dummyCamTarget != null)
        {
            Destroy(dummyCamTarget);
        }

        if (uiCanvasGo != null)
        {
            Destroy(uiCanvasGo);
        }

        localCar = null;
        rivalCar = null;
        participantA = null;
        participantB = null;
        state = ConfrontationState.Idle;
    }

    private void AbortConfrontation()
    {
        EndConfrontation();
    }
}
