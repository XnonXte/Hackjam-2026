using System.Collections;
using UnityEngine;

/// <summary>
/// Handles all visual aspects of the player: Animations, Sprite Flipping, and Dash Trails.
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Dash Trail (Celeste Silhouette)")]
    [SerializeField] private Color dashTrailColor = new Color(0f, 1f, 1f, 0.7f);
    [SerializeField] private float dashTrailDuration = 0.3f;
    [SerializeField] private float dashTrailInterval = 0.03f;

    // References
    private PlayerController controller;
    private Animator anim;
    private SpriteRenderer sr;

    // ── Animation Hashes ──────────────────────────────────────────────────────
    private static readonly int AnimIdle = Animator.StringToHash("player_idle");
    private static readonly int AnimRun = Animator.StringToHash("player_run");
    private static readonly int AnimJump = Animator.StringToHash("player_jump");
    private static readonly int AnimFall = Animator.StringToHash("player_fall");
    private static readonly int AnimDeath = Animator.StringToHash("player_death");

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // Subscribe to controller events
        controller.OnDeath += PlayDeathAnimation;
        controller.OnRevive += PlayReviveAnimation;
        controller.OnDash += StartDashTrail;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        controller.OnDeath -= PlayDeathAnimation;
        controller.OnRevive -= PlayReviveAnimation;
        controller.OnDash -= StartDashTrail;
    }

    private void Update()
    {
        if (controller.IsDead) return;

        UpdateSpriteFlipping();
        UpdateAnimations();
    }

    private void UpdateSpriteFlipping()
    {
        // Since the source sprite is drawn facing left:
        // When FacingDir < 0 (moving left), flipX = false.
        sr.flipX = controller.FacingDir < 0f;
    }

    private void UpdateAnimations()
    {
        if (!controller.IsGrounded)
        {
            if (controller.Velocity.y > 0f)
                anim.Play(AnimJump);
            else
                anim.Play(AnimFall);
        }
        else if (Mathf.Abs(controller.Velocity.x) > 0.1f)
        {
            anim.Play(AnimRun);
        }
        else
        {
            anim.Play(AnimIdle);
        }
    }

    // =========================================================================
    // EVENT HANDLERS
    // =========================================================================

    private void PlayDeathAnimation()
    {
        anim.Play(AnimDeath);
    }

    private void PlayReviveAnimation()
    {
        anim.Play(AnimIdle);
    }

    private void StartDashTrail()
    {
        StartCoroutine(DashTrailRoutine());
    }

    // =========================================================================
    // DASH TRAILS
    // =========================================================================

    private IEnumerator DashTrailRoutine()
    {
        // Polling controller.IsDashing allows the trail to cleanly follow the physics logic
        while (controller.IsDashing)
        {
            CreateDashGhost();
            yield return new WaitForSeconds(dashTrailInterval);
        }
    }

    private void CreateDashGhost()
    {
        if (sr == null) return;

        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sprite = sr.sprite;
        ghostSr.flipX = sr.flipX;
        ghostSr.color = dashTrailColor;
        
        ghostSr.sortingLayerID = sr.sortingLayerID;
        ghostSr.sortingOrder = sr.sortingOrder - 1; 

        StartCoroutine(FadeGhost(ghostSr));
    }

    private IEnumerator FadeGhost(SpriteRenderer ghostSr)
    {
        float elapsed = 0f;
        Color startColor = ghostSr.color;

        while (elapsed < dashTrailDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / dashTrailDuration);
            ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        Destroy(ghostSr.gameObject);
    }
}