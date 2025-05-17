using UnityEngine;
using UnityEngine.EventSystems;

public class ItemUnequipHandler : MonoBehaviour, IPointerClickHandler
{
    public EquipmentSlot equipmentSlot;
    
    private float lastClickTime;
    private float doubleClickTimeThreshold = 0.3f; // seconds between clicks to count as double-click
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < doubleClickTimeThreshold)
        {
            // Double click detected
            if (equipmentSlot != null)
            {
                equipmentSlot.UnequipCurrentItem();
            }
        }
        
        lastClickTime = Time.time;
    }
}