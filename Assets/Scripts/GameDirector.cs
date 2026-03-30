// Coordinates the full run loop: timing, score/combo state, feedback popups, audio, and scene transitions.
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Makes Unity run GameDirector earlier than most scripts so shared run state is ready before others query it.
[DefaultExecutionOrder(-100)]
public class GameDirector : MonoBehaviour
{
    public const string MainSceneName = "MainScene";
    public const string ResultsSceneName = "GameOver";

    private const float DefaultRunDuration = 75f;
    private const int MissPenalty = 2;
    private const int MaxMultiplier = 4;

    // The director is a scene singleton so other scripts can query the current run without wiring references everywhere.
    public static GameDirector Instance { get; private set; }

    private Canvas canvas;
    private RectTransform popupLayer;
    private TMP_Text popupTemplate;
    private float runDuration = DefaultRunDuration;
    private bool runFinished;
    private float comboGraceRemaining;

    public float TimeRemaining { get; private set; }
    public int CurrentScore { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public int CaughtCount { get; private set; }
    public int MissCount { get; private set; }

    public bool IsRunActive => !runFinished && TimeRemaining > 0f;
    public float Progress => runDuration <= 0f ? 1f : 1f - (TimeRemaining / runDuration);
    public float RemainingGrace => comboGraceRemaining;
    public float Accuracy
    {
        get
        {
            int total = CaughtCount + MissCount;
            return total == 0 ? 100f : (CaughtCount / (float)total) * 100f;
        }
    }

    public int CurrentMultiplier
    {
        get
        {
            if (Combo <= 0)
            {
                return 1;
            }

            int multiplier = 1 + ((Combo - 1) / 4);
            return Mathf.Clamp(multiplier, 1, MaxMultiplier);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // A fresh run always starts from a clean time scale and an empty stats snapshot.
        Time.timeScale = 1f;
        RunStats.BeginRun();
        runDuration = ResolveRunDuration();
        TimeRemaining = runDuration;
        CacheUi();
        BuildBackdrop();
    }

    void Update()
    {
        if (runFinished)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartRun();
            return;
        }

        // The director owns the master countdown and the temporary combo-shield timer.
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        comboGraceRemaining = Mathf.Max(0f, comboGraceRemaining - Time.deltaTime);

        if (TimeRemaining <= 0f)
        {
            FinishRun();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }

    public float GetSpawnInterval(float baseInterval, float minimumInterval, float spawnBias)
    {
        // Spawn pacing accelerates over time and can be nudged per shape to make valuable targets rarer or more frequent.
        float timeRamp = Mathf.Lerp(1f, 0.5f, Progress);
        float comboRamp = Mathf.Lerp(1f, 0.85f, Mathf.InverseLerp(4f, 18f, Combo) * spawnBias);
        float targetInterval = baseInterval * timeRamp * comboRamp;
        return Mathf.Max(targetInterval, minimumInterval);
    }

    public void RegisterCatch(FallingShapeBase shape)
    {
        if (runFinished || shape == null)
        {
            return;
        }

        Combo += 1;
        CaughtCount += 1;
        MaxCombo = Mathf.Max(MaxCombo, Combo);

        if (shape.ComboGraceSeconds > 0f)
        {
            comboGraceRemaining = Mathf.Max(comboGraceRemaining, shape.ComboGraceSeconds);
        }

        // Rewards are calculated here so score, multiplier, audio, and popup feedback all stay in sync.
        int reward = shape.BasePoints * CurrentMultiplier;
        CurrentScore += reward;

        PlayClip(shape.CatchClip, shape.WorldPosition, Mathf.Lerp(0.7f, 1.05f, Mathf.InverseLerp(1f, 24f, shape.BasePoints)), 1f + Mathf.Min(Combo, 12) * 0.015f);
        SpawnPopup(shape.WorldPosition, $"+{reward}", shape.PopupColor, 1.15f + ((CurrentMultiplier - 1) * 0.08f));
        SpawnSpriteEcho(shape.Visual, shape.WorldPosition, shape.Visual.color);

        if (shape.ComboGraceSeconds > 0f)
        {
            SpawnPopup(shape.WorldPosition + new Vector3(0f, -0.45f, 0f), "Shield", new Color(1f, 0.92f, 0.55f), 0.9f);
        }
    }

    public void RegisterMiss(FallingShapeBase shape)
    {
        if (runFinished || shape == null)
        {
            return;
        }

        MissCount += 1;
        PlayClip(shape.MissClip, shape.WorldPosition, 0.7f, 0.92f);
        SpawnSpriteEcho(shape.Visual, shape.WorldPosition, new Color(1f, 0.48f, 0.48f, 0.9f));

        if (comboGraceRemaining > 0f)
        {
            // A stored shield absorbs one miss before the combo actually breaks.
            comboGraceRemaining = 0f;
            SpawnPopup(shape.WorldPosition, "Shield Used", new Color(1f, 0.9f, 0.55f), 0.95f);
            return;
        }

        if (Combo > 0)
        {
            SpawnPopup(shape.WorldPosition + new Vector3(0f, 0.2f, 0f), "Combo Lost", new Color(1f, 0.72f, 0.52f), 0.95f);
        }

        Combo = 0;

        int penalty = Mathf.Min(CurrentScore, MissPenalty);
        if (penalty > 0)
        {
            CurrentScore -= penalty;
            SpawnPopup(shape.WorldPosition + new Vector3(0f, -0.3f, 0f), $"-{penalty}", new Color(1f, 0.45f, 0.45f), 1f);
        }
    }

    private float ResolveRunDuration()
    {
        // GameDirector reads the run length from the Timer component, so changing the Timer value in the scene changes how long a run lasts.
        Timer timer = FindObjectOfType<Timer>();
        if (timer == null)
        {
            return DefaultRunDuration;
        }

        return Mathf.Max(30f, timer.ConfiguredDuration);
    }

    private void CacheUi()
    {
        canvas = FindObjectOfType<Canvas>();
        popupTemplate = FindObjectOfType<TextMeshProUGUI>();

        if (canvas == null)
        {
            return;
        }

        UiFactory.ConfigureCanvas(canvas);

        // Popup text is spawned under its own full-screen layer so feedback can be positioned anywhere on the canvas.
        GameObject popupLayerObject = new GameObject("PopupLayer", typeof(RectTransform));
        popupLayer = popupLayerObject.GetComponent<RectTransform>();
        popupLayer.SetParent(canvas.transform, false);
        popupLayer.anchorMin = Vector2.zero;
        popupLayer.anchorMax = Vector2.one;
        popupLayer.offsetMin = Vector2.zero;
        popupLayer.offsetMax = Vector2.zero;
    }

    private void BuildBackdrop()
    {
        // This hook is where scene-wide presentation and physics cleanup are centralized.
        NormalizeScenePhysics();
    }

    private void NormalizeScenePhysics()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            // Falling shapes are meant to pass through each other.
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        }

        EdgeCollider2D[] edgeColliders = FindObjectsOfType<EdgeCollider2D>();

        foreach (EdgeCollider2D edgeCollider in edgeColliders)
        {
            edgeCollider.sharedMaterial = null;
            edgeCollider.edgeRadius = 0f;
        }
    }

    private void SpawnPopup(Vector3 worldPosition, string message, Color color, float scaleBoost)
    {
        if (popupLayer == null || popupTemplate == null)
        {
            return;
        }

        TextMeshProUGUI popup = UiFactory.CreateText(
            "Popup",
            popupLayer,
            popupTemplate,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(260f, 70f),
            34f,
            TextAlignmentOptions.Center,
            false);

        popup.text = message;
        popup.color = color;
        popup.raycastTarget = false;

        // World-space catch/miss positions are converted into canvas coordinates so popups appear exactly over the action.
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            popupLayer,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 anchoredPoint);

        popup.rectTransform.anchoredPosition = anchoredPoint;
        popup.gameObject.AddComponent<FloatingHudText>().Initialize(new Vector2(0f, 90f), 0.6f, scaleBoost);
    }

    private void SpawnSpriteEcho(SpriteRenderer sourceRenderer, Vector3 worldPosition, Color color)
    {
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return;
        }

        // sprite echo is a short-lived afterimage that makes catches and misses feel punchier.
        GameObject echoObject = new GameObject("ShapeEcho");
        echoObject.transform.position = worldPosition;
        echoObject.transform.rotation = sourceRenderer.transform.rotation;
        echoObject.transform.localScale = sourceRenderer.transform.lossyScale;

        SpriteRenderer echoRenderer = echoObject.AddComponent<SpriteRenderer>();
        echoRenderer.sprite = sourceRenderer.sprite;
        echoRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        echoRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
        echoRenderer.color = color;

        echoObject.AddComponent<SpriteEcho>().Initialize(echoRenderer);
    }

    private void PlayClip(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        if (clip == null)
        {
            return;
        }

        // A temporary AudioSource keeps per-event sound playback simple and avoids requiring a central mixer object.
        GameObject audioObject = new GameObject($"Sfx_{clip.name}");
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f;
        source.Play();

        Destroy(audioObject, (clip.length / Mathf.Max(0.01f, pitch)) + 0.1f);
    }

    private void FinishRun()
    {
        if (runFinished)
        {
            return;
        }

        runFinished = true;
        TimeRemaining = 0f;
        Time.timeScale = 1f;

        // Snapshot the run before changing scenes so the results screen can render without carrying the whole scene forward.
        RunStats.CompleteRun(CurrentScore, MaxCombo, CaughtCount, MissCount);
        SceneManager.LoadScene(ResultsSceneName);
    }

    private void RestartRun()
    {
        // Restart is a clean scene reload instead of a manual reset.
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainSceneName);
    }
}

// Animates floating score text so gameplay feedback appears near the action instead of only in the HUD.
public class FloatingHudText : MonoBehaviour
{
    private RectTransform rectTransform;
    private TextMeshProUGUI label;
    private Vector2 velocity;
    private float lifetime;
    private float totalLifetime;
    private float startScale;

    public void Initialize(Vector2 travelVelocity, float duration, float scale)
    {
        rectTransform = GetComponent<RectTransform>();
        label = GetComponent<TextMeshProUGUI>();
        velocity = travelVelocity;
        lifetime = duration;
        totalLifetime = duration;
        startScale = scale;
        rectTransform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        // Popups drift upward, shrink slightly, and fade so they read quickly without cluttering the screen.
        float deltaTime = Time.unscaledDeltaTime;
        lifetime -= deltaTime;
        rectTransform.anchoredPosition += velocity * deltaTime;

        float progress = 1f - (lifetime / totalLifetime);
        rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, startScale * 0.8f, progress);

        if (label != null)
        {
            Color nextColor = label.color;
            nextColor.a = Mathf.Lerp(1f, 0f, progress);
            label.color = nextColor;
        }

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}

// Draws a short-lived sprite afterimage behind a shape to accentuate catches and misses.
public class SpriteEcho : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float lifetime = 0.25f;
    private float totalLifetime = 0.25f;
    private Vector3 startScale;

    public void Initialize(SpriteRenderer renderer)
    {
        spriteRenderer = renderer;
        startScale = transform.localScale;
    }

    void Update()
    {
        // The echo expands and fades over a fraction of a second to create a lightweight impact effect.
        float deltaTime = Time.unscaledDeltaTime;
        lifetime -= deltaTime;

        float progress = 1f - (lifetime / totalLifetime);
        transform.localScale = Vector3.Lerp(startScale, startScale * 1.55f, progress);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(0.75f, 0f, progress);
            spriteRenderer.color = color;
        }

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
