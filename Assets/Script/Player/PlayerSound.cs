using UnityEngine;

/// <summary>
/// Drives player SFX off PlayerController's public state/events.
/// Attach to the same GameObject as PlayerController (or assign it manually).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSound : MonoBehaviour
{
    // =========================================================================
    // REFERENCES
    // =========================================================================
    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private AudioSource sfxSource;

    // =========================================================================
    // CLIPS
    // =========================================================================
    [Header("Jump / Land / Ledge")]
    [SerializeField] private AudioClip[] jumpClips;
    [SerializeField] private AudioClip[] landClips;
    [SerializeField] private AudioClip[] ledgeJumpClips; // <-- Clips for ledge climbing

    [Header("Death")]
    [SerializeField] private AudioClip[] deathClips;

    [Header("Dash")]
    [SerializeField] private AudioClip[] dashClips;

    [Header("Climb")]
    [SerializeField] private AudioClip[] climbClips;
    [SerializeField] private float climbStepInterval = 0.28f;
    [SerializeField] private float minClimbSpeed = 0.05f;

    [Header("Pitch Variation")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    // =========================================================================
    // STATE
    // =========================================================================
    private bool wasGrounded;
    private bool wasOnWall;
    private float climbTimer;

    private void Reset()
    {
        controller = GetComponent<PlayerController>();
        sfxSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        controller.OnDeath += HandleDeath;
        controller.OnDash += HandleDash;
        controller.OnRevive += HandleRevive;
        controller.OnJump += HandleJump;
        controller.OnLedgeJump += HandleLedgeJump; // <-- Subscribed to LedgeJump event
    }

    private void OnDisable()
    {
        controller.OnDeath -= HandleDeath;
        controller.OnDash -= HandleDash;
        controller.OnRevive -= HandleRevive;
        controller.OnJump -= HandleJump;
        controller.OnLedgeJump -= HandleLedgeJump; // <-- Unsubscribed
    }

    private void Update()
    {
        if (controller.IsDead) return;

        HandleLand();
        HandleClimb();

        wasGrounded = controller.IsGrounded;
        wasOnWall = controller.IsOnWall;
    }

    // =========================================================================
    // LANDING & JUMPING
    // =========================================================================
    private void HandleLand()
    {
        bool grounded = controller.IsGrounded;

        if (grounded && !wasGrounded)
        {
            PlayRandom(landClips);
        }
    }

    private void HandleJump()
    {
        PlayRandom(jumpClips);
    }

    private void HandleLedgeJump()
    {
        PlayRandom(ledgeJumpClips); // <-- Plays when ledge climb happens
    }

    // =========================================================================
    // CLIMB
    // =========================================================================
    private void HandleClimb()
    {
        bool climbing = controller.IsOnWall && Mathf.Abs(controller.Velocity.y) > minClimbSpeed;

        if (!climbing)
        {
            climbTimer = 0f;
            return;
        }

        climbTimer -= Time.deltaTime;
        if (climbTimer <= 0f)
        {
            PlayRandom(climbClips);
            climbTimer = climbStepInterval;
        }
    }

    // =========================================================================
    // EVENT HANDLERS
    // =========================================================================
    private void HandleDeath()
    {
        PlayRandom(deathClips);
    }

    private void HandleDash()
    {
        PlayRandom(dashClips);
    }

    private void HandleRevive()
    {
        wasGrounded = controller.IsGrounded;
        wasOnWall = controller.IsOnWall;
        climbTimer = 0f;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================
    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        sfxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}