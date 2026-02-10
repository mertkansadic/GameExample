using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Takip edilecek hedef (Bizim karakter)
    public Vector3 offset = new Vector3(0, 0, -10); // Kamera mesafesi
    public float smoothSpeed = 0.125f; // Kameranın yumuşak gelme hızı

    void LateUpdate()
    {
        // Hedef yoksa (karakter henüz doğmadıysa) hiçbir şey yapma
        if (target == null) return;

        // Hedefin gitmek istediği yer
        Vector3 desiredPosition = target.position + offset;
        
        // Oraya yumuşakça kay (Smooth Follow)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Pozisyonu güncelle
        transform.position = smoothedPosition;
    }
}