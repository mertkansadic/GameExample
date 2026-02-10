using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Saldırı Ayarları")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.0f; 
    [SerializeField] private LayerMask enemyLayers; 
    [SerializeField] private int attackDamage = 10; 

    [Header("Iceman Ayarları")]
    [SerializeField] private GameObject iceProjectilePrefab; 
    [SerializeField] private Transform firePoint;            

    [Header("Identity Sistemi")]
    [SerializeField] private IdentityShifter identityShifter;

    private Animator animator;
    private Rigidbody2D rb;
    private bool isGrounded = true;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (identityShifter == null) identityShifter = GetComponent<IdentityShifter>();

        CameraFollow camScript = FindFirstObjectByType<CameraFollow>(); 
        if (camScript != null)
        {
            camScript.target = transform;
        }
        else if (Camera.main != null)
        {
               Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
               Camera.main.transform.parent = transform;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        Move();
        Jump();
    
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (identityShifter != null) identityShifter.ShiftIdentity();
        }
    }

    void Move()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);
        
        if (moveX > 0) transform.localScale = new Vector3(-1, 1, 1);
        else if (moveX < 0) transform.localScale = new Vector3(1, 1, 1);
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (animator != null) animator.SetTrigger("Jump");
            isGrounded = false;
        }
    }

    void Attack()
    {
        if (animator != null) animator.SetTrigger("Attack");

        // 1. WOLVERINE SALDIRISI (Yakın Dövüş)
        if (gameObject.name.ToLower().Contains("wolverine"))
        {
            if (attackPoint == null) return;

            // Vuruş algılamasını Client kendi tarafında yapar (Lag hissetmemek için)
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
            
            foreach(Collider2D enemy in hitEnemies)
            {
                // Vurulan objenin NetworkObject bileşenini almamız lazım ki Server'a "Kimi vurduk?" diyebilelim.
                if (enemy.TryGetComponent<NetworkObject>(out NetworkObject targetNetObj))
                {
                    // Server'a emri gönder: "Bu ID'li düşmana hasar ver!"
                    MeleeAttackServerRpc(targetNetObj.NetworkObjectId, attackDamage);
                }
            }
        }
        // 2. ICEMAN SALDIRISI (Mermi)
        else 
        {
             if (!IsOwner) return;

             if (iceProjectilePrefab != null && firePoint != null)
             {
                Quaternion bulletRotation = transform.localScale.x < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                SpawnProjectileServerRpc(firePoint.position, bulletRotation);
             }
        }
    }

    // --- YENİ EKLENEN KISIM ---
    // Bu fonksiyon Client tarafından çağrılır ama SERVER üzerinde çalışır.
    [ServerRpc]
    private void MeleeAttackServerRpc(ulong targetNetworkObjectId, int damage)
    {
        // Server, gelen ID ile sahnedeki o düşman objesini bulur
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObj))
        {
            // O objenin can scriptini bulur ve hasarı uygular
            if (targetObj.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
            {
                enemyHealth.TakeDamage(damage);
            }
        }
    }
    // ---------------------------

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground")) isGrounded = true;
        else isGrounded = true;
    }
    
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(Vector3 spawnPos, Quaternion rotation)
    {
        GameObject projectile = Instantiate(iceProjectilePrefab, spawnPos, rotation);
        projectile.GetComponent<NetworkObject>().Spawn();
    }
}