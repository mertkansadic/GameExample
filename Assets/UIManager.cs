using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI healthText; // İşte o boş kutucuk bu!

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateHealthUI(int currentHealth)
    {
        if(healthText != null)
            healthText.text = "Health: " + currentHealth;
    }
}