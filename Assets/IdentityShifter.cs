using Unity.Netcode;
using UnityEngine;

public class IdentityShifter : NetworkBehaviour
{
    [SerializeField] private GameObject wolverinePrefab;
    [SerializeField] private GameObject icemanPrefab;

    public void ShiftIdentity()
    {
        if (!IsOwner) return;
        ShiftIdentityServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void ShiftIdentityServerRpc(ulong clientId)
    {
        // 1. Mevcut oyuncu objesini ve pozisyonunu bul
        GameObject currentObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        Vector3 spawnPos = currentObject.transform.position;
        Quaternion spawnRot = currentObject.transform.rotation;

        // 2. ESKİ karakterin canını hafızaya al
        int savedHealth = 100; // Varsayılan değer
        if (currentObject.TryGetComponent<PlayerHealth>(out PlayerHealth oldHealth))
        {
            savedHealth = oldHealth.Health.Value;
        }

        // 3. Hangi karaktere dönüşeceğine karar ver
        // ToLower() ekleyerek küçük-büyük harf hatasını, Contains ile de (Clone) ekini görmezden geliyoruz.
        GameObject prefabToSpawn = currentObject.name.ToLower().Contains("wolverine") ? icemanPrefab : wolverinePrefab;

        // 4. ESKİ karakteri dünyadan sil
        currentObject.GetComponent<NetworkObject>().Despawn();

        // 5. YENİ karakteri yarat ve yetki ver
        GameObject newCharacter = Instantiate(prefabToSpawn, spawnPos, spawnRot);
        newCharacter.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        // 6. HAFIZADAKİ canı yeni karaktere enjekte et
        if (newCharacter.TryGetComponent<PlayerHealth>(out PlayerHealth newHealth))
        {
            newHealth.Health.Value = savedHealth;
        }
    }
}