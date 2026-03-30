// Controls the player/paddle collector that slides along the bottom of the screen to catch falling shapes.
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public float EnginePower = 220f;
    public float MaxSpeed = 18f;
    public float IdleDamping = 130f;
    public float TurnBoost = 3.1f;
    public float LaunchBoost = 1.5f;
    public float ScreenPadding = 0.9f;

    private Rigidbody2D rigidBody;
    private Collider2D hitBox;
    private float horizontalInput;
    private float currentSpeedX;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        hitBox = GetComponent<Collider2D>();

        // The player uses a kinematic body because movement is authored directly rather than simulated with forces.
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.gravityScale = 0f;
        rigidBody.useFullKinematicContacts = true;
        rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidBody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rigidBody.velocity = Vector2.zero;

        if (hitBox != null)
        {
            hitBox.sharedMaterial = null;
            hitBox.isTrigger = false;
        }
    }

    void Update()
    {
        // Only horizontal input matters, so the controller reads the axis once and reuses it in FixedUpdate.
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (GameDirector.Instance != null && !GameDirector.Instance.IsRunActive)
        {
            horizontalInput = 0f;
        }
    }

    void FixedUpdate()
    {
        float targetSpeed = horizontalInput * MaxSpeed;
        float acceleration = EnginePower;

        // A launch boost makes the paddle feel responsive from a standstill.
        if (Mathf.Abs(horizontalInput) > 0.01f && Mathf.Abs(currentSpeedX) < MaxSpeed * 0.2f)
        {
            acceleration *= LaunchBoost;
        }

        // Turning against current momentum gets extra acceleration so direction changes don't feel sluggish.
        if (Mathf.Abs(horizontalInput) > 0.01f &&
            Mathf.Abs(currentSpeedX) > 0.01f &&
            Mathf.Sign(targetSpeed) != Mathf.Sign(currentSpeedX))
        {
            acceleration *= TurnBoost;
        }

        currentSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeed, acceleration * Time.fixedDeltaTime);

        if (Mathf.Abs(horizontalInput) < 0.01f)
        {
            currentSpeedX = Mathf.MoveTowards(currentSpeedX, 0f, IdleDamping * Time.fixedDeltaTime);
        }

        float nextX = rigidBody.position.x + (currentSpeedX * Time.fixedDeltaTime);
        float clampedX = ClampToScreen(nextX);

        // Hitting the screen edge zeroes horizontal speed so the player does not keep pushing into the boundary.
        if (!Mathf.Approximately(clampedX, nextX))
        {
            currentSpeedX = 0f;
        }

        rigidBody.MovePosition(new Vector2(clampedX, rigidBody.position.y));
    }

    private float ClampToScreen(float targetX)
    {
        if (Camera.main == null)
        {
            return targetX;
        }

        // Screen bounds are converted to world space so the controller works across resolutions and camera sizes.
        float spriteHalfWidth = hitBox != null ? hitBox.bounds.extents.x : 0.5f;
        float maxWorldX = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0f, 0f)).x;
        float limit = maxWorldX - spriteHalfWidth - ScreenPadding;
        return Mathf.Clamp(targetX, -limit, limit);
    }
}
