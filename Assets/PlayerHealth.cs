using UnityEngine;
using Unity.Netcode;
using System.Collections; // Coroutine kullanmak için bu gerekli

public class PlayerHealth : NetworkBehaviour
{
    // Canı 100 yaptık ki hemen ölme
    public NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    private Animator animator; // Animatör referansı
    private bool isDead = false; // Zaten ölü mü kontrolü

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += OnHealthChanged;
        // Karakterin üzerindeki veya altındaki Animator'ı bul
        animator = GetComponentInChildren<Animator>();
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (IsOwner && UIManager.Instance != null)
        {
            // UIManager.Instance.UpdateHealthUI(newValue); // UI kodun varsa açarsın
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (isDead) return; // Zaten ölüyse tekrar hasar almasın

        Health.Value -= damage;
        Debug.Log("Can: " + Health.Value);

        if (Health.Value <= 0)
        {
            // Ölüm sürecini başlat
            isDead = true;
            DieClientRpc(); // Tüm client'larda animasyonu oynat
        }
    }

    // Ölüm animasyonunu herkesin görmesi için ClientRpc kullanıyoruz
    [ClientRpc]
    private void DieClientRpc()
    {
        if (IsOwner)
        {
             StartCoroutine(DeathRoutine());
        }
        else
        {
            // Diğer oyuncular sadece animasyonu görsün, ışınlanma mantığını owner yapacak
            if (animator != null) animator.SetTrigger("Die");
        }
    }

    // Ölüm süreci (Beklemeli)
    private IEnumerator DeathRoutine()
    {
        Debug.Log("Ölüm animasyonu başlıyor...");

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Animasyon bitene kadar bekle
        yield return new WaitForSeconds(2f); 

        // Işınlanma ve Can Yenileme
        Debug.Log("Yeniden doğuluyor...");
        transform.position = new Vector3(0, 0, 0); 
        
        if (IsServer) ResetHealth();
        else ResetHealthServerRpc();

        isDead = false;

        // --- YENİ EKLENEN KISIM ---
        // Animatörü resetle ve ayağa kaldır
        if (animator != null) 
        {
            animator.ResetTrigger("Die"); // Eski öl emrini temizle
            animator.SetTrigger("Respawn"); // YENİ: Ayağa kalk emri ver!
        }
    }

    [ServerRpc]
    private void ResetHealthServerRpc()
    {
        ResetHealth();
    }
    
    private void ResetHealth()
    {
         Health.Value = 100;
         isDead = false;
    }
}