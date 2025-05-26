
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class EnemyAI : MonoBehaviour, IDamagable,IPlayerRange
{
    public AudioSource audioSource;
    public AudioClip BulletFiresound;
    // Get the NavMeshAgent component
    private NavMeshAgent agent;
    [Header("-------------------------Animation---------------------")]
    //Animation Handler
    private Animator m_Animator;
    // Animation hashvaluse
    private int EnemySpeed;
    private int isMoing;
    private int PlayerRange;
    private int isDead;
    private int Onhit;
    bool DeadTriggerCalled;
    [SerializeField] private int GunLayerSet;

    [Header("--------------------Raycast Variable----------------")]
    [SerializeField] private Transform RaycastLocation;
    // Spawn State Variable
    [Header("--------------------Spawn State-----------")]
    [Tooltip("Must Declare this or Error popup")]
    [SerializeField] private Transform[] SpawnMultipos;
    [SerializeField] private LayerMask WallLayer;
    
    [SerializeField] private Light TorchLight;
    private int CurrentSpawnerIndex;
    private Vector3 Spawntargetpos;
    private bool PlayerLastPosReached = false;
    private bool PlayerTooFar = false;

    [Header("------------Enemy Health & State-------------")]
    // Health of the enemy
    public GameObject HitEffect;
    public Slider HealthSlider;
    [SerializeField] private int Health = 100;
    [SerializeField] private EnemyState currentstate;
    private bool StartCalled = false;
    public enum EnemyState
    {
        Chase,
        Attack,
        Dead,
        Spawn
    }
    // instanciate bullet prefab
    [Header("------------Bullet-------------")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public GameObject MussleflashPrefab;
    public float BulletSpeed = 150;
    public float FireRatethreshold = .5f;
    private float nextFire = 0;

    // player reference
    [Header("---------------Player Reference Watch-----------------")]
    private Transform Player;
    private Vector3 playerLastpos;

    [SerializeField]private MaximuPlayerChase _maximuPlayerChase;
    // ------------------------------Built in Funtion-------------------------
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // animation controller
        m_Animator = GetComponent<Animator>();
        isMoing = Animator.StringToHash("isMoving");
        PlayerRange = Animator.StringToHash("PlayerRange");
        EnemySpeed = Animator.StringToHash("EnemySpeed");
        isDead = Animator.StringToHash("isDead");
        Onhit = Animator.StringToHash("OnHit");
        DeadTriggerCalled = false;
        m_Animator.SetLayerWeight(GunLayerSet, 1);
        // agent position
        agent = GetComponent<NavMeshAgent>();
        // Spawn State Declare
        currentstate = EnemyState.Spawn;
        agent.speed = 2f;
        CurrentSpawnerIndex = 0;
        Spawntargetpos = SpawnMultipos[CurrentSpawnerIndex].position;
        agent.SetDestination(Spawntargetpos);
        StartCalled = true;
        HealthSlider.value = Health;

    }

    private void Update()
    {
        HandleMoveAnimation();
        if (DeadTriggerCalled) return;
        switch (currentstate)
        {
            case EnemyState.Spawn:
                {
                    SpawnState();
                }
                break;
            case EnemyState.Chase:
                {
                    Chase_state();
                }
                break;
            case EnemyState.Attack:
                {
                    Attack_state();
                }
                break;
            case EnemyState.Dead:
                {
                    Dead_state();
                }
                break;
        }

    }
    private void OnEnable()
    {
        if (StartCalled)
        {
            // animation controller
            DeadTriggerCalled = false;
            m_Animator.SetLayerWeight(GunLayerSet, 1);
            // agent position
            currentstate = EnemyState.Spawn;
            agent.speed = 2f;
            CurrentSpawnerIndex = 0;
            Spawntargetpos = SpawnMultipos[CurrentSpawnerIndex].position;
            agent.SetDestination(Spawntargetpos);
            HealthSlider.value = Health;
        }

        EnemyAI_Maneger.OnplayerTriggered += EnemyAI_Maneger_OnplayerTriggered;
    }
    private void OnDisable()
    {
        EnemyAI_Maneger.OnplayerTriggered -= EnemyAI_Maneger_OnplayerTriggered;
    }
    private void OnDestroy()
    {
        EnemyAI_Maneger.OnplayerTriggered -= EnemyAI_Maneger_OnplayerTriggered;
    }
    // ------------------------------Event Trigger Funtion
    private void EnemyAI_Maneger_OnplayerTriggered(Transform obj)
    {
        Player = obj;
        playerLastpos = Player.position;
        currentstate = EnemyState.Chase;
    }
    // -------------------------------- Animation --------------------------
    private void HandleMoveAnimation()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            m_Animator.SetBool(isMoing, true);
        }
        else
        {
            m_Animator.SetBool(isMoing, false);
        }
    }
    // --------------------------------State machine functions
    private void SpawnState()
    {


        m_Animator.SetFloat(EnemySpeed, 0f);
        agent.speed = 1f;
        agent.isStopped = false;

        if (Vector3.Distance(transform.position, SpawnMultipos[CurrentSpawnerIndex].position) < 0.5f)
        {
            StartCoroutine(EnemyWaitFornextPostion());
        }
        else if (PlayerLastPosReached || PlayerTooFar)
        {
            agent.SetDestination(SpawnMultipos[CurrentSpawnerIndex].position);
            PlayerLastPosReached = false;
            PlayerTooFar = false;
        }

    }
    private IEnumerator EnemyWaitFornextPostion()
    {
        CurrentSpawnerIndex = (CurrentSpawnerIndex + 1) % SpawnMultipos.Length;
        Spawntargetpos = SpawnMultipos[CurrentSpawnerIndex].position;
        yield return new WaitForSeconds(2);
        agent.SetDestination(Spawntargetpos);
    }
    private void Chase_state()
    {
        agent.isStopped = false;
        m_Animator.SetFloat(EnemySpeed, 1f);
        m_Animator.SetBool(PlayerRange, false);
        agent.speed = 3f;
        if (Player != null)
        {
            if (_maximuPlayerChase.PlayerInRange)
            {
                if (TorchLight != null) TorchLight.color = Color.red;

                currentstate = EnemyState.Attack;

            }
            else
            {
                Player = null;
                PlayerTooFar = true;
                currentstate = EnemyState.Spawn;
            }
        }
        else
        {


            agent.SetDestination(playerLastpos);
            if (Vector3.Distance(transform.position, playerLastpos) < .5f)
            {
                PlayerLastPosReached = true;
                if (TorchLight != null) TorchLight.color = Color.white;
                currentstate = EnemyState.Spawn;
            }
        }
    }
    private void Attack_state()
    {

        if (Player != null)
        {
            Vector3 Directiontoplayer = (Player.position - transform.position).normalized;
            if (Physics.Raycast(RaycastLocation.position, Directiontoplayer, Vector3.Distance(transform.position, Player.position), WallLayer))
            {
                Player = null;
                currentstate = EnemyState.Chase;

            }
            else
            {

                m_Animator.SetBool(PlayerRange, true);
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                RotateTowards(Player.position);
                if (Time.time > nextFire)
                {
                    nextFire = Time.time + FireRatethreshold;
                    Spawnbullet(Player.transform.position);
                }
            }
        }
        else
        {
            _maximuPlayerChase.PlayerInRange = false;
            currentstate = EnemyState.Chase;
        }
    }
    private void Dead_state()
    {
        if (!DeadTriggerCalled)
        {
            agent.isStopped = true;
            gameObject.tag = "Untagged";
            gameObject.layer = LayerMask.NameToLayer("PlayerNonCollider");
            m_Animator.SetLayerWeight(GunLayerSet, 0);
            m_Animator.SetTrigger(isDead);
            DeadTriggerCalled = true;
        }

    }
    // ----------------------------------------Firing funtion --------------------------
    private void RotateTowards(Vector3 PlayerPos)
    {
        Vector3 direction = (PlayerPos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Quaternion currentRotation = transform.rotation;
        transform.rotation = Quaternion.Slerp(currentRotation, lookRotation, Time.deltaTime * 5f);
    }
    private void Spawnbullet(Vector3 playertranform)
    {
        audioSource.PlayOneShot(BulletFiresound);
        //bullet spawn
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Rigidbody BulletRB = bullet.GetComponent<Rigidbody>();
        Vector3 Direction = (playertranform - transform.position).normalized;
        BulletRB.AddForce(Direction * BulletSpeed, ForceMode.Impulse);

        GameObject mussleFlash = Instantiate(MussleflashPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Destroy(mussleFlash, 0.2f);

    }
    // Trigger functions called by other scripts
    public void PlayerOutofRange()
    {
        Player = null;
    }
    // Animation Funtion
    public void EnemyAIDeadAnim()
    {
        Destroy(this.gameObject);
    }

    public void TakeDamage(int Damageamount, Vector3 HitEffectPos)
    {
        GameObject temp = Instantiate(HitEffect, HitEffectPos, Quaternion.identity);
        Destroy(temp, 0.2f);
        Player = PlayerController.instance?.gameObject.transform;
        currentstate = EnemyState.Chase;
        Health -= Damageamount;
        HealthSlider.value = Health;
        EnemyAI_Maneger.PlayertriggeredEvent(Player);
        m_Animator.SetTrigger(Onhit);

        if (Health <= 0)
        {
            currentstate = EnemyState.Dead;
        }

    }

}
