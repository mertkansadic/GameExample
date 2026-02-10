using Unity.Netcode.Components;
using UnityEngine;

// Bu script, standart NetworkTransform'un "Sadece Sunucu Yönetir" kuralını bozar.
// Oyuncuların kendi karakterlerini yönetmesine izin verir.
public class ClientNetworkTransform : NetworkTransform
{
    // "Yetki sunucuda mı?" sorusuna "Hayır" cevabı veriyoruz.
    // Böylece yetki Client'a (oyuncuya) geçiyor.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}