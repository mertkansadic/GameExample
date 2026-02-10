using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System; // Action (Event) için gerekli

public class EnemyHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 20; // Başlangıç canı
    // Slime öldüğünde çalışacak bir "Olay" (Event) tanımlıyoruz
    public static event Action OnEnemyDied; 

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    private Animator animator;
    private Collider2D enemyCollider;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
        
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    // Dışarıdan canını ayarlamak için (Dalga 4'te iki katına çıkarmak için)
    public void SetMaxHealth(int newMaxHealth)
    {
        if (!IsServer) return;
        maxHealth = newMaxHealth;
        currentHealth.Value = maxHealth;
    }

    // Oyuncunun (Ice-man mermisi veya Wolverine pençesi) çağıracağı fonksiyon
    public void TakeDamage(int damageAmount)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= damageAmount;

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        
        // Ölünce artık oyuncuya çarpamasın
        if (enemyCollider != null) enemyCollider.enabled = false;

        // Ölüm animasyonunu tüm clientlarda oynat
        DieClientRpc();

        // Yöneticiye "bir düşman öldü" haberini gönder
        OnEnemyDied?.Invoke();

        // Animasyon bitince yok et (1 saniye bekler)
        StartCoroutine(DespawnRoutine());
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        if (animator != null) animator.SetTrigger("Die");
    }

    private IEnumerator DespawnRoutine()
    {
        yield return new WaitForSeconds(1f); // Animasyon süresi kadar bekle
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}