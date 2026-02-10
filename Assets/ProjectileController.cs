using UnityEngine;
using Unity.Netcode;

public class ProjectileController : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifeTime = 2f;

    // Start yerine OnNetworkSpawn kullanıyoruz (Daha güvenli)
    public override void OnNetworkSpawn()
    {
        // Sadece SUNUCU (Server) merminin ömrünü takip eder.
        if (IsServer)
        {
            // 2 saniye sonra 'DespawnProjectile' fonksiyonunu çalıştır
            Invoke(nameof(DespawnProjectile), lifeTime);
        }
    }

    void Update()
    {
        // Mermiyi herkes hareket ettirebilir (Görsel akıcılık için)
        // Ama pozisyonu Server yönettiği için NetworkTransform varsa burayı silebilirsin.
        // Şimdilik kalsın, zarar gelmez.
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        // Vuruş işlemini SADECE sunucu yapar
        if (!IsServer) return;

        if (hitObject.CompareTag("Enemy"))
        {
            if (hitObject.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
            {
                enemyHealth.TakeDamage(damage);
            }
            DespawnProjectile(); // Vurunca yok et
        }
        else if (hitObject.CompareTag("Ground"))
        {
            DespawnProjectile(); // Duvara çarpınca yok et
        }
    }

    // Mermiyi güvenli bir şekilde yok eden özel fonksiyon
    private void DespawnProjectile()
    {
        // Eğer obje hala sahnede var ise (Daha önce yok olmadıysa)
        if (IsSpawned && NetworkObject != null)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}