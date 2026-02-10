using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private GameObject lobbyTemplate;

    private const string JoinCodeKey = "j"; // Şifrenin saklanacağı anahtar

    private async void Start()
    {
        try {
            // --- KİMLİK ÇAKIŞMASINI ÖNLEME (RANDOM PROFILE) ---
            var options = new InitializationOptions();
            string randomProfileName = "Player_" + UnityEngine.Random.Range(0, 10000);
            options.SetProfile(randomProfileName);
            
            await UnityServices.InitializeAsync(options);
            // --------------------------------------------------

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            Debug.Log($"Unity Servisleri Hazır! Profil: {randomProfileName}");
        } catch (Exception e) { Debug.LogError($"Başlangıç Hatası: {e}"); }
    }

    // --- HOST (KURUCU) İŞLEMLERİ ---
    public async void CreateLobby()
    {
        if (string.IsNullOrEmpty(lobbyNameInput.text)) return;

        try
        {
            Debug.Log("Relay hattı oluşturuluyor...");
            
            // 1. Relay Oluştur (4 Kişilik)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            // 2. NetworkManager'a Relay Bilgilerini Ver (WSS - WebGL Uyumlu)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new Unity.Networking.Transport.Relay.RelayServerData(allocation, "wss"));

            // 3. Lobiyi Oluştur ve İçine Relay Kodunu Gizle
            var options = new CreateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyNameInput.text, 4, options);
            Debug.Log($"Lobi Kuruldu! Kod: {joinCode}. Oyun Başlatılıyor...");

            // 4. Host Olarak Başlat
            NetworkManager.Singleton.StartHost();
            
            // 5. Herkesi Sahneye Taşı
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
        catch (Exception e) { Debug.LogError($"Host Hatası: {e}"); }
    }

    // --- LİSTELEME İŞLEMLERİ ---
    public async void RefreshLobbyList()
    {
        try
        {
            // Template (Örnek Buton) dışındakileri temizle
            foreach (Transform child in lobbyListContainer)
            {
                if (child.gameObject != lobbyTemplate) Destroy(child.gameObject);
            }

            var queryOptions = new QueryLobbiesOptions { Count = 20 };
            var queryResult = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            foreach (var lobby in queryResult.Results)
            {
                GameObject btn = Instantiate(lobbyTemplate, lobbyListContainer);
                btn.SetActive(true);
                btn.GetComponentInChildren<TMP_Text>().text = lobby.Name;
                string lobbyId = lobby.Id;
                btn.GetComponent<Button>().onClick.AddListener(() => JoinLobby(lobbyId));
            }
        }
        catch (Exception e) { Debug.LogError($"Listeleme Hatası: {e}"); }
    }

    // --- CLIENT (KATILIMCI) İŞLEMLERİ ---
    public async void JoinLobby(string lobbyId)
    {
        try
        {
            Debug.Log("Lobiye giriliyor...");
            
            // 1. Lobiye Katıl
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            
            // 2. Lobiden Gizli Relay Kodunu Çek
            string joinCode = lobby.Data[JoinCodeKey].Value;
            Debug.Log($"Relay Kodu Bulundu: {joinCode}. Bağlanılıyor...");

            // 3. Relay'e Bağlan
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 4. NetworkManager'a Bilgileri Ver (WSS - WebGL Uyumlu)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new Unity.Networking.Transport.Relay.RelayServerData(joinAllocation, "wss"));

            // 5. Client Olarak Başlat (Sahne otomatik değişecek)
            NetworkManager.Singleton.StartClient();

        }
        catch (Exception e) { Debug.LogError($"Katılma Hatası: {e}"); }
    }
}