using UnityEngine;
using Unity.Netcode;

public class EnemyAI : NetworkBehaviour
{
    public float speed = 2f;
    public int damageAmount = 10;
    private Transform target;
    private Rigidbody2D rb; // Fizik motoruna erişim

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>(); // Motoru çalıştır
    }

    void Update()
    {
        if (!IsServer) return; // Sadece sunucu yönetir

        if (target == null)
        {
            FindClosestTarget();
        }
        else
        {
            MoveToTarget();
        }
    }

    void FindClosestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (GameObject player in players)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }
        target = closestPlayer;
    }

    void MoveToTarget()
    {
        if (target == null) return;

        // Yönü bul (Sadece Sağa/Sola)
        Vector2 direction = (target.position - transform.position).normalized;

        // --- DÜZELTME BURADA ---
        // Işınlanmak (transform.position) yerine motor gücü (velocity) kullanıyoruz.
        // direction.x * speed -> Sağa sola git
        // rb.velocity.y -> Yerçekimine dokunma, düşüyorsa düşsün, duruyorsa dursun.
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

        // Yüzünü dönme
        // Bu kod mevcut boyutu (1, 0.5, 10 fark etmez) korur, sadece yönü çevirir.
    if (direction.x > 0) 
        transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    else 
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    // Çarpışma kodu aynen kalıyor
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamageServerRpc(damageAmount);
            }
        }
    }
}