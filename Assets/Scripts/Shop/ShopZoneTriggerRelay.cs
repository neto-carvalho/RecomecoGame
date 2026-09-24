using UnityEngine;

[DisallowMultipleComponent]
public class ShopZoneTriggerRelay : MonoBehaviour
{
    public ShopZone shop;

    void OnTriggerEnter(Collider other) => shop?.RegisterTrigger(other, true);
    void OnTriggerExit(Collider other) => shop?.RegisterTrigger(other, false);
}
