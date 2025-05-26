
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    // create Singleton & player Component
    public static PlayerController instance;
    public static event Action<float, float> CameraShake;
    public static event Action<int> UpdateHealth;

    //----------------------------------------------Audio 
    private AudioSource m_audioSource;
    [SerializeField] private AudioClip m_OnHitAudio;
    [SerializeField] private AudioClip fireBulletClip;
    // -----------------------------------------------Player speed and rotation speed
    [Header("----------------Player Settings----------------")]
    [HideInInspector] public Rigidbody Player_rb;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private int playerSpeed = 5;
    [SerializeField] private int rotationSpeed = 10;
    public int PlayerHealth = 100;

    private bool PlayerDeadCalled = false;


    // -------------------------------------------------bullet prefab
    [Header("-------------------Bullet Settings-----------------")]
    public GameObject bulletPrefab;
    public GameObject musleflash;
    public Transform bulletSpawnPoint;
    public float BulletSpeed = 150;
    public Transform RaycastLocation;
    public LayerMask WallLayer;
    [Header("Flamethrower")]
    public GameObject flamethrowerPrefab;

    //Enemy Related 
    private Vector3 Enemyposition;

    // -----------------------------------------------Animator reference---------------------------------
    [Header("-----------------Animator--------------------")]
    Animator animator;
    // Animator Hashes  
    private int isRunning;
    private int isFiring;
    private int EnemyOnRange;
    private int isDead;
    private int Onhit;
    // Animation layers
    [SerializeField] private int GunLayer = 1;

    // ---------------------------------------------------player Input-----------------------------------
    public PlayerInput playerInput;
    private Vector2 currentMoveInput;
    private bool isMovePressed;
    public bool IsEnemyTriggered = false;

    //-------------------------------------------------Default Funtion------------------------------------

    private void Start()
    {

        PlayerDeadCalled = false;
        UpdateHealth?.Invoke(PlayerHealth);

        if (cameraTransform == null)
        {
            cameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;
        }
        else
        {
            print("Camera Found");
        }
    }
    private void Awake()
    {
        // audio get component
        m_audioSource = GetComponent<AudioSource>();
        // Set the instance to this
        if (instance != null && instance != this)
        {
            Destroy(instance.gameObject);
        }
        instance = this;

        // Get the Rigid Body component
        Player_rb = GetComponent<Rigidbody>();

        // Get the player input component
        playerInput = new PlayerInput();

        // Subscribe to the input events
        playerInput.CharacterController.Move.started += InputOnMove;
        playerInput.CharacterController.Move.canceled += InputOnMove;
        playerInput.CharacterController.Move.performed += InputOnMove;

        // Get the animator component
        animator = GetComponent<Animator>();

        // Get the animator hashes
        isRunning = Animator.StringToHash("isRunning");
        Onhit = Animator.StringToHash("OnHit");
        isFiring = Animator.StringToHash("isFiring");
        isDead = Animator.StringToHash("isDead");

        EnemyOnRange = Animator.StringToHash("EnemyOnRange");
        animator.SetLayerWeight(GunLayer, 1);

    }
    private void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
    }
    private void Update()
    {
        HandleAnimation();
    }
    private void OnEnable()
    {
        // Enable the input
        playerInput.CharacterController.Enable();
    }

    private void OnDisable()
    {
        // Disable the input
        playerInput?.CharacterController.Disable();
    }
    private void OnDestroy()
    {
        playerInput?.CharacterController.Disable();
    }

    // --------------------------------------------Input event subscribe function-------------------------------------
    private void InputOnMove(InputAction.CallbackContext context)
    {
        currentMoveInput = context.ReadValue<Vector2>();
        isMovePressed = currentMoveInput.x != 0 || currentMoveInput.y != 0;
    }

    // ------------------------------Player Funtions-----------------------------
    private void HandleMovement()
    {
        // Get the camera's forward direction
        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        // Get the right direction relative to the camera
        Vector3 right = cameraTransform.right;
        right.y = 0;
        right.Normalize();

        // Get the movement direction relative to the camera
        Vector3 desiredMoveDirection = (forward * currentMoveInput.y + right * currentMoveInput.x).normalized;

        // Move the player in the desired direction
        Vector3 move = desiredMoveDirection * playerSpeed;
        Player_rb.velocity = new Vector3(move.x, Player_rb.velocity.y, move.z);

    }
    private void HandleRotation()
    {
        if (isMovePressed)
        {


            Vector3 forward = cameraTransform.forward;
            forward.y = 0; // Ignore the vertical component
            forward.Normalize();

            // Get the right direction relative to the camera
            Vector3 right = cameraTransform.right;
            right.y = 0; // Ignore the vertical component
            right.Normalize();

            // Get the movement direction relative to the camera
            Vector3 desiredMoveDirection = (forward * currentMoveInput.y + right * currentMoveInput.x).normalized;

            // Rotate the player towards the desired move direction
            if (desiredMoveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
                Quaternion currentRotation = transform.rotation;
                transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else if(!isMovePressed && !IsEnemyTriggered)
        {
            Vector3 lookDirection = cameraTransform.forward;
            lookDirection.y = 0f;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, (rotationSpeed * 0.5f) * Time.deltaTime);
            }
        }
    }
    private void HandleAnimation()
    {
        animator.SetBool(isRunning, isMovePressed);
    }
    public void PlayerDied()
    {
        animator.SetLayerWeight(GunLayer, 0f);
        animator.SetTrigger(isDead);
        PlayerDeadCalled = true;
    }


    #region Animation Events
    public void DestroyplayerFuntionAnim()
    {
        Destroy(gameObject);
    }
    public void FiresoundAnim()
    {
        m_audioSource.PlayOneShot(fireBulletClip);
    }
    //Animation Event
    public void FlameThrowerAnimevent()
    {
        Vector3 tempdir = Enemyposition - flamethrowerPrefab.transform.position;
        Vector3 Direction = new Vector3(tempdir.x, tempdir.y + .8f, tempdir.z).normalized;
        flamethrowerPrefab.transform.forward = Direction;
        flamethrowerPrefab.SetActive(true);
    }
    public void FlameThrowerAnimeventEnd()
    {
        flamethrowerPrefab.SetActive(false);
    }
    public void SpawnBulletAnim()
    {
        Vector3 tempdir = Enemyposition - bulletSpawnPoint.position;
        Vector3 Direction = new Vector3(tempdir.x, tempdir.y + .9f, tempdir.z).normalized;
        bulletSpawnPoint.forward = Direction;
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet.GetComponent<Rigidbody>().AddForce(bullet.transform.forward * BulletSpeed, ForceMode.Impulse);
        GameObject temp = Instantiate(musleflash, bulletSpawnPoint);
        Destroy(temp, 0.2f);

    }
    #endregion

    #region CalledBy other object
    public void SpawnBullet(Transform enemyposition)
    {

        RotateTowardsEnemy(enemyposition.position);
        if(Physics.Raycast(RaycastLocation.position, (enemyposition.position - RaycastLocation.position).normalized, 50f, WallLayer))
        {
            animator.SetBool(EnemyOnRange, true);
            animator.SetBool(isFiring, false);
        }
        else
        {
            animator.SetBool(EnemyOnRange, true);
            Enemyposition = enemyposition.position;
            animator.SetBool(isFiring, true);
            Debug.DrawRay(RaycastLocation.position, (enemyposition.position - RaycastLocation.position).normalized * 50f, Color.red);
        }
       
       
    }
    public void EnemyOutofRange()
    {
        animator.SetBool(isFiring, false);
        animator.SetBool(EnemyOnRange, false);
    }
    public void RotateTowardsEnemy(Vector3 Enemyposition)
    {
        Vector3 positiontoLook = (Enemyposition - transform.position).normalized;
        positiontoLook.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(positiontoLook);
        Quaternion currentRotation = transform.rotation;
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);

    }
    #endregion


    #region InstanceFuntion
    // -------------------------------------public Funtion for other object-----------------------
    public void HealPlayer(int Healamount, out bool Healable)
    {
        if (PlayerHealth >= 90)
        {
            Healable = false;
        }
        else
        {
            Healable = true;
            PlayerHealth = Healamount;
            UpdateHealth?.Invoke(PlayerHealth);
            print("Healed " + PlayerHealth);
        }

    }
    public void TakeDamage(int HitDamage)
    {
        if (PlayerDeadCalled) return;
        PlayerHealth -= HitDamage;
        m_audioSource.PlayOneShot(m_OnHitAudio);
        animator.SetTrigger(Onhit);
        UpdateHealth?.Invoke(PlayerHealth);
        CameraShake?.Invoke(2f, .1f);
        if (PlayerHealth <= 0)
        {
            PlayerDied();

        }
    }

    #endregion

}
