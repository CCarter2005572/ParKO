using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Image = UnityEngine.UI.Image;

public class PlayerController : NetworkBehaviour
{
    [SyncVar] public string playerName;

    public static List<Transform> usedSpawnPoints = new List<Transform>();

    [Header("Movement")]
    public float moveSpeed = 20f;
    public float jumpForce = 50f;
    private Vector3 externalForce = Vector3.zero;

    [Header("Gravity Settings")]
    public float gravityScale = 2f;
    public float jumpGravityMultiplier = 1f;
    public float fallGravityMultiplier = 2f;

    [Header("Stats")]
    public float Stamina, maxStamina;
    [SyncVar] public float Health, maxHealth;

    [Header("Costs")]
    public float attackCost;

    [Header("References")]
    public CharacterController controller;
    public Transform cam;
    public Image StaminaBar;
    public Image HealthBar;
    public GameObject exhaustedText;
    public GameObject deathText;
    public GameObject p1hud;
    public GameObject healthBar;
    public GameObject staminaBar;

    private Vector3 moveDirection;
    private float yVelocity;
    private Animator animator;
    private AudioManager audioManager;

    private bool hasDied = false;
    private bool playedOutOfStamina = false;
    public bool isFrozen = false;
    [HideInInspector] public bool hasFinished = false;
    private TextMeshProUGUI winText;

    [Header("Sprinting")]
    public float sprintSpeedMultiplier = 1.5f;
    public float staminaDrainRate = 10f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;
    private float lastSprintTime = -999f;
    private bool isSprinting = false;

    [Header("Double Jump")]
    public float doubleJumpMultiplier = 0.5f;
    public float doubleJumpStaminaCost = 15f;
    private bool canDoubleJump = false;

    [Header("Exhaustion")]
    public float exhaustionDuration = 3f;
    private bool isExhausted = false;
    private bool waitingForExhaustion = false;
    private float exhaustionTimer = 0f;

    private float originalHeight;
    private Vector3 originalCenter;

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();
        if (audioManager == null)
            Debug.LogWarning("AudioManager not found in scene.");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Transform[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn").Select(go => go.transform).ToArray();
        foreach (Transform spawn in spawnPoints)
        {
            if (!usedSpawnPoints.Contains(spawn))
            {
                usedSpawnPoints.Add(spawn);
                transform.position = spawn.position;
                transform.rotation = spawn.rotation;
                break;
            }
        }
    }

    void Start()
    {
        if (isLocalPlayer)
        {
            CmdRegisterWithRaceManager();
            CmdAssignRandomName();

            if (isServer)
            {
                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
                if (spawnPoints.Length > 0)
                {
                    int index = (int)(netId % spawnPoints.Length);
                    Transform spawn = spawnPoints[index].transform;
                    transform.position = spawn.position;
                    transform.rotation = spawn.rotation;
                }
            }

            isFrozen = true;

            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();

            GameObject hudInstance = Instantiate(Resources.Load("HUD")) as GameObject;

            StaminaBar = GameObject.Find("Stamina")?.GetComponent<Image>();
            HealthBar = GameObject.Find("Health")?.GetComponent<Image>();
            exhaustedText = GameObject.Find("exhaustedText");
            deathText = GameObject.Find("deathText");
            p1hud = GameObject.Find("p1hud");
            healthBar = GameObject.Find("healthBar");
            staminaBar = GameObject.Find("staminaBar");
            winText = GameObject.Find("winText")?.GetComponent<TextMeshProUGUI>();

            if (exhaustedText != null) exhaustedText.SetActive(false);
            if (deathText != null) deathText.SetActive(false);
            if (p1hud != null) p1hud.SetActive(true);
            if (healthBar != null) healthBar.SetActive(true);
            if (staminaBar != null) staminaBar.SetActive(true);

            cam = Camera.main?.transform;
            CameraController camController = Camera.main?.GetComponent<CameraController>();
            if (camController != null)
            {
                camController.target = this.transform;
                if (camController.pivot != null)
                {
                    camController.pivot.position = this.transform.position;
                    camController.pivot.parent = this.transform;
                }
            }

            originalHeight = controller.height;
            originalCenter = controller.center;
        }
    }

    [Command] void CmdRegisterWithRaceManager() => FindObjectOfType<RaceManager>()?.RegisterPlayer(this);
    [Command] private void CmdAssignRandomName() { if (string.IsNullOrEmpty(playerName)) playerName = "Player" + Random.Range(1000, 9999); }

    void Update()
    {
        if (!isLocalPlayer || hasDied || isFrozen) return;
        HandleExhaustion();
        HandleMovement();
        HandleActions();
        UpdateUI();
        CheckDeath();
        CheckStaminaDrain();
    }

    private void HandleActions()
    {
        if (!isExhausted && Input.GetKeyDown(KeyCode.F) && Stamina >= attackCost)
        {
            Stamina -= attackCost;
            CmdAttack();
        }
    }

   [Command]
    private void CmdAttack()
    {
        RpcPlayAttackAnimation(); // Sync animation to all players

        Collider[] hitPlayers = Physics.OverlapSphere(transform.position + transform.forward * 2f, 1.5f);
        foreach (var hit in hitPlayers)
        {
            PlayerController target = hit.GetComponent<PlayerController>();
            if (target != null && target != this && !target.hasDied)
            {
                target.TakeDamage(20f); // Adjust value as needed
            }
        }
    }


    [ClientRpc]
    private void RpcPlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("punch");
        }
    }

    public void TakeDamage(float amount)
    {
        if (!isServer || hasDied) return;

        Health -= amount;
        if (Health < 0) Health = 0;
    }

    private void HandleMovement()
    {
        if (isFrozen || isExhausted)
        {
            yVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;
            moveDirection = new Vector3(0, yVelocity, 0);
            controller.Move(moveDirection * Time.deltaTime);
            animator?.SetFloat("movement", 0);
            return;
        }

        bool isGrounded = IsGrounded();

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        float speedMultiplier = 1f;
        isSprinting = Input.GetKey(KeyCode.LeftShift) && Stamina > 0 && (horizontal != 0 || vertical != 0);

        if (isSprinting)
        {
            speedMultiplier = sprintSpeedMultiplier;
            Stamina -= staminaDrainRate * Time.deltaTime;
            lastSprintTime = Time.time;
            if (Stamina < 0) Stamina = 0;
        }

        if (!isSprinting && Time.time - lastSprintTime >= staminaRegenDelay && Stamina < maxStamina)
        {
            Stamina += staminaRegenRate * Time.deltaTime;
            if (Stamina > maxStamina) Stamina = maxStamina;
        }

        Vector3 horizontalMove = (forward * vertical + right * horizontal).normalized * moveSpeed * speedMultiplier;

        if (isGrounded && yVelocity < 0) yVelocity = -2f;

        if (isGrounded)
        {
            if (!isExhausted && Input.GetButtonDown("Jump"))
            {
                canDoubleJump = true;
                yVelocity = jumpForce;
                animator.SetBool("jumpage", true);
                audioManager?.PlaySFX(audioManager.jumpGrunt);
            }
            else if (!isExhausted && yVelocity <= 0f)
            {
                animator.SetBool("jumpage", false);
            }

            if (waitingForExhaustion)
            {
                waitingForExhaustion = false;
                isExhausted = true;
                exhaustionTimer = exhaustionDuration;
            }
        }
        else if (!isExhausted && canDoubleJump && Input.GetButtonDown("Jump") && Stamina >= doubleJumpStaminaCost)
        {
            yVelocity = jumpForce * doubleJumpMultiplier;
            Stamina -= doubleJumpStaminaCost;
            canDoubleJump = false;
            animator.SetBool("jumpage", true);
            audioManager?.PlaySFX(audioManager.jumpGrunt);
            lastSprintTime = Time.time;
        }

        if (yVelocity > 0)
            yVelocity += Physics.gravity.y * gravityScale * jumpGravityMultiplier * Time.deltaTime;
        else
            yVelocity += Physics.gravity.y * gravityScale * fallGravityMultiplier * Time.deltaTime;

        moveDirection = new Vector3(horizontalMove.x, yVelocity, horizontalMove.z) + externalForce;
        controller.Move(moveDirection * Time.deltaTime);

        float currentSpeed = new Vector3(horizontalMove.x, 0, horizontalMove.z).magnitude;
        animator?.SetFloat("movement", currentSpeed);
    }

    private void UpdateUI()
    {
        if (StaminaBar != null)
            StaminaBar.fillAmount = Mathf.Lerp(StaminaBar.fillAmount, Stamina / maxStamina, Time.deltaTime * 10f);

        if (HealthBar != null)
            HealthBar.fillAmount = Mathf.Lerp(HealthBar.fillAmount, Health / maxHealth, Time.deltaTime * 10f);
    }

    private void CheckDeath()
    {
        if (!hasDied && Health <= 0)
        {
            hasDied = true;
            audioManager?.PlaySFX(audioManager.death);
            animator.SetTrigger("die");

            moveSpeed = 0;
            jumpForce = 0;
            Stamina = 0;

            if (deathText != null)
                deathText.SetActive(true);

            controller.height = originalHeight * 0.5f;
            controller.center = new Vector3(0, controller.height / 2f, 0);

            if (IsGrounded())
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 5f))
                {
                    transform.position = new Vector3(transform.position.x, hit.point.y + controller.height / 5f, transform.position.z);
                }
            }

            RaceManager raceManager = FindObjectOfType<RaceManager>();
            if (raceManager != null)
            {
                CmdNotifyDeath();
            }
        }
    }

    [Command]
    private void CmdNotifyDeath()
    {
        RaceManager raceManager = FindObjectOfType<RaceManager>();
        if (raceManager != null)
        {
            raceManager.PlayerDied(this);
        }
    }


    private void CheckStaminaDrain()
    {
        if (hasDied) return;

        if (!playedOutOfStamina && Stamina <= 0)
        {
            playedOutOfStamina = true;
            audioManager?.PlaySFX(audioManager.staminaDeplete);

            if (IsGrounded())
            {
                isExhausted = true;
                exhaustionTimer = exhaustionDuration;
                animator?.SetBool("isExhausted", true);
            }
            else
            {
                waitingForExhaustion = true;
                animator?.SetBool("isExhausted", true);
            }
        }
        else if (Stamina > 0 && playedOutOfStamina)
        {
            playedOutOfStamina = false;
        }
    }

    private void HandleExhaustion()
    {
        if (isExhausted)
        {
            exhaustionTimer -= Time.deltaTime;

            if (exhaustedText != null) exhaustedText.SetActive(true);
            animator?.SetBool("isExhausted", true);

            controller.height = originalHeight * 0.5f;
            controller.center = new Vector3(0, controller.height / 2f, 0);

            if (IsGrounded())
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 5f))
                {
                    transform.position = new Vector3(transform.position.x, hit.point.y + controller.height / 2f, transform.position.z);
                }
            }

            if (exhaustionTimer <= 0f)
            {
                isExhausted = false;
                if (exhaustedText != null) exhaustedText.SetActive(false);
                animator?.SetBool("isExhausted", false);
                controller.height = originalHeight;
                controller.center = originalCenter;
            }
        }
        else
        {
            if (exhaustedText != null) exhaustedText.SetActive(false);
            animator?.SetBool("isExhausted", false);
            controller.height = originalHeight;
            controller.center = originalCenter;
        }
    }

    private bool IsGrounded()
    {
        if (controller.isGrounded) return true;

        float raycastDistance = controller.skinWidth + 0.1f;
        return Physics.Raycast(transform.position, Vector3.down, raycastDistance);
    }

    public void SetExternalForce(Vector3 force)
    {
        externalForce = force;
    }

    public void FreezePlayer()
    {
        isFrozen = true;
        if (!isLocalPlayer) return;

        animator?.SetFloat("movement", 0f);
        animator?.SetBool("jumpage", false);
    }

    [TargetRpc] public void TargetFreezeOnFinish(NetworkConnection target) { isFrozen = true; animator?.SetFloat("movement", 0f); animator?.SetBool("jumpage", false); }
    [TargetRpc] public void TargetUnfreeze(NetworkConnection target) { isFrozen = false; }

    [TargetRpc] public void TargetShowWinText(NetworkConnection target, int placement)
    {
        if (winText == null) return;
        string placeText = placement switch
        {
            1 => "Winner!",
            2 => "2nd Place!",
            3 => "3rd Place!",
            _ => "Finished!"
        };
        Color placeColor = placement switch
        {
            1 => new Color(1f, 0.84f, 0f),
            2 => new Color(0.75f, 0.75f, 0.75f),
            3 => new Color(0.8f, 0.5f, 0.2f),
            _ => Color.white
        };
        winText.text = placeText;
        winText.fontMaterial = new Material(winText.fontMaterial);
        winText.color = placeColor;
        winText.gameObject.SetActive(true);
    }
}
