using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private GameObject enemyPrefab; // Yeşil Slime Prefabı buraya
    [SerializeField] private Transform spawnPointsParent; // Haritadaki noktaların ana objesi
    [SerializeField] private int baseEnemyHealth = 20; // Slime'ın normal canı
    [SerializeField] private float timeBetweenWaves = 3f; // Dalga arası bekleme süresi

    private List<Transform> spawnPoints = new List<Transform>();
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool isWaveActive = false;

    public override void OnNetworkSpawn()
    {
        // Sadece sunucu (Host) oyunu yönetir
        if (!IsServer) 
        {
            this.enabled = false; // Clientlar bu kodu çalıştırmasın
            return; 
        }

        // Spawn noktalarını listeye al
        foreach (Transform child in spawnPointsParent)
        {
            spawnPoints.Add(child);
        }

        // Düşman ölüm olayına abone ol
        EnemyHealth.OnEnemyDied += EnemyDiedHandler;

        // Oyunu başlat!
        StartCoroutine(StartNextWave());
    }

    public override void OnNetworkDespawn()
    {
        // Abonelikten çık (Hata almamak için önemli)
        EnemyHealth.OnEnemyDied -= EnemyDiedHandler;
    }

    private void EnemyDiedHandler()
    {
        enemiesAlive--;
        Debug.Log("Düşman öldü. Kalan: " + enemiesAlive);

        if (enemiesAlive <= 0 && isWaveActive)
        {
            isWaveActive = false;
            StartCoroutine(StartNextWave());
        }
    }

    private IEnumerator StartNextWave()
    {
        Debug.Log($"Dalga {currentWave} bitti. Sonraki dalga {timeBetweenWaves} saniye sonra...");
        yield return new WaitForSeconds(timeBetweenWaves);

        currentWave++;
        
        // --- SENİN İSTEDİĞİN ÖZEL MANTIK ---
        int count = 1;
        int healthMultiplier = 1;

        if (currentWave == 1) count = 1;      // 1. Dalga: 1 Slime
        else if (currentWave == 2) count = 2; // 2. Dalga: 2 Slime
        else if (currentWave == 3) count = 4; // 3. Dalga: 4 Slime
        else 
        {
            // 4. ve sonraki dalgalar: 4 Slime ama Canlar İKİ KAT!
            count = 4; 
            healthMultiplier = 2; 
            Debug.Log("DİKKAT! Slime'lar güçlendi!");
        }
        // -----------------------------------

        Debug.Log($"DALGA {currentWave} BAŞLIYOR! Gelen Düşman: {count}");
        
        enemiesAlive = count;
        isWaveActive = true;

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(healthMultiplier);
            yield return new WaitForSeconds(0.5f); // Yarım saniye arayla doğsunlar
        }
    }

    private void SpawnEnemy(int healthMultiplier)
    {
        if (spawnPoints.Count == 0) return;

        // Rastgele bir nokta seç
        Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        // Düşmanı yarat
        GameObject newEnemy = Instantiate(enemyPrefab, randomPoint.position, Quaternion.identity);
        newEnemy.GetComponent<NetworkObject>().Spawn();

        // Canını ayarla
        if (newEnemy.TryGetComponent<EnemyHealth>(out EnemyHealth healthScript))
        {
            healthScript.SetMaxHealth(baseEnemyHealth * healthMultiplier);
        }
    }
}