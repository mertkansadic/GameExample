using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BootstrapManager : MonoBehaviour
{
    public TMP_InputField nameInput;
    public Button connectButton;

    private void Start()
    {
        if (PlayerPrefs.HasKey("player name"))
        {
            nameInput.text = PlayerPrefs.GetString("player name");
        }
        OnNameChanged(nameInput.text);
    }

    public void OnNameChanged(string newName)
    {
        connectButton.interactable = newName.Length >= 1 && newName.Length <= 12;
    }

    public void SaveAndProceed()
    {
        PlayerPrefs.SetString("player name", nameInput.text);
        Debug.Log("Isim Kaydedildi: " + nameInput.text);
        SceneManager.LoadScene("MenuScene");
    }
}