// Provides the shared movement, collision, and feedback logic for every falling shape in the game.
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class FallingShapeBase : MonoBehaviour
{
    private const string ScreenBottomTag = "ScreenBottom";

    // Each shape subclass only supplies data; the base class turns that data into runtime behavior.
    protected struct ShapeTuning
    {
        public string Label;
        public int Points;
        public float BaseFallSpeed;
        public float EndSpeedMultiplier;
        public float SwayAmplitude;
        public float SwayFrequency;
        public float SpinSpeed;
        public float TelegraphDuration;
        public float TelegraphSpeedScale;
        public float SpawnBias;
        public float ComboGraceSeconds;
        public float HitStopDuration;
        public Color PopupColor;
    }

    private ShapeTuning tuning;
    private Rigidbody2D rigidBody;
    private Collider2D hitBox;
    private SpriteRenderer spriteRenderer;
    private AudioClip catchClip;
    private AudioClip missClip;
    private Vector3 baseScale;
    private Color baseColor;
    private float spawnTime;
    private float elapsedFixedTime;
    private float telegraphTimeRemaining;
    private float swayPhase;
    private float lastSwayOffsetX;
    private bool resolved;

    public int BasePoints => tuning.Points;
    public float SpawnBias => tuning.SpawnBias;
    public float ComboGraceSeconds => tuning.ComboGraceSeconds;
    public float HitStopDuration => tuning.HitStopDuration;
    public Color PopupColor => tuning.PopupColor;
    public AudioClip CatchClip => catchClip;
    public AudioClip MissClip => missClip;
    public SpriteRenderer Visual => spriteRenderer;
    public Vector3 WorldPosition => transform.position;
    public string ShapeLabel => tuning.Label;

    protected abstract ShapeTuning BuildTuning();

    protected virtual void Awake()
    {
        // Tune values are captured once up front so each prefab can behave differently without duplicating logic.
        tuning = BuildTuning();
    }

    protected virtual void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        hitBox = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseColor = spriteRenderer.color;
        spawnTime = Time.time;
        elapsedFixedTime = 0f;
        telegraphTimeRemaining = tuning.TelegraphDuration;
        swayPhase = Random.Range(0f, Mathf.PI * 2f);
        lastSwayOffsetX = 0f;

        // Shapes are moved manually with a kinematic body so their fall pattern stays consistent and easy to tune.
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.gravityScale = 0f;
        rigidBody.drag = 0f;
        rigidBody.angularDrag = 0f;
        rigidBody.useFullKinematicContacts = true;
        rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidBody.velocity = Vector2.zero;

        if (hitBox != null)
        {
            hitBox.sharedMaterial = null;
            hitBox.isTrigger = true;
        }

        // Audio sources are optional per prefab and are interpreted as catch first, miss second.
        AudioSource[] audioSources = GetComponents<AudioSource>();
        if (audioSources.Length > 0)
        {
            catchClip = audioSources[0].clip;
        }

        if (audioSources.Length > 1)
        {
            missClip = audioSources[1].clip;
        }
    }

    protected virtual void Update()
    {
        if (resolved)
        {
            return;
        }

        UpdateVisuals();
    }

    protected virtual void FixedUpdate()
    {
        if (resolved)
        {
            return;
        }

        GameDirector director = GameDirector.Instance;
        if (director == null || !director.IsRunActive)
        {
            return;
        }

        elapsedFixedTime += Time.fixedDeltaTime;
        // Fall speed ramps upward as the run progresses, which increases pressure on the player.
        float speedMultiplier = Mathf.Lerp(1f, tuning.EndSpeedMultiplier, director.Progress);
        float verticalSpeed = tuning.BaseFallSpeed * speedMultiplier;

        if (telegraphTimeRemaining > 0f)
        {
            // Telegraph time briefly slows the shape before it fully commits to the drop.
            telegraphTimeRemaining = Mathf.Max(0f, telegraphTimeRemaining - Time.fixedDeltaTime);
            verticalSpeed *= tuning.TelegraphSpeedScale;
        }

        // Horizontal sway is applied as a delta so the object oscillates smoothly rather than drifting away.
        float swayOffsetX = Mathf.Sin((elapsedFixedTime * tuning.SwayFrequency) + swayPhase) * tuning.SwayAmplitude;
        float swayDeltaX = swayOffsetX - lastSwayOffsetX;
        lastSwayOffsetX = swayOffsetX;

        Vector2 nextPosition = rigidBody.position + new Vector2(swayDeltaX, -verticalSpeed * Time.fixedDeltaTime);
        rigidBody.MovePosition(nextPosition);
        rigidBody.MoveRotation(rigidBody.rotation + (tuning.SpinSpeed * Time.fixedDeltaTime));
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (resolved)
        {
            return;
        }

        if (collision.GetComponent<Player>() != null)
        {
            resolved = true;
            // A player collision counts as a successful catch and immediately hands scoring to the director.
            GameDirector.Instance?.RegisterCatch(this);
            Cleanup();
            return;
        }

        if (!collision.CompareTag(ScreenBottomTag))
        {
            return;
        }

        resolved = true;
        // Reaching the screen bottom is treated as a miss so the same prefab can report both outcomes.
        GameDirector.Instance?.RegisterMiss(this);
        Cleanup();
    }

    private void UpdateVisuals()
    {
        if (telegraphTimeRemaining > 0f)
        {
            transform.localScale = baseScale;

            // The alpha pulse makes a telegraphed spawn read clearly before full-speed movement begins.
            Color telegraphColor = baseColor;
            telegraphColor.a = 0.58f + (Mathf.PingPong((Time.time - spawnTime) * 2.4f, 0.22f));
            spriteRenderer.color = telegraphColor;
        }
        else
        {
            transform.localScale = baseScale;
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, baseColor, Time.deltaTime * 12f);
        }
    }

    private void Cleanup()
    {
        if (hitBox != null)
        {
            hitBox.enabled = false;
        }

        if (rigidBody != null)
        {
            // Disable physics first so a shape cannot trigger twice while it is being destroyed.
            rigidBody.simulated = false;
            rigidBody.velocity = Vector2.zero;
        }

        Destroy(gameObject);
    }
}
