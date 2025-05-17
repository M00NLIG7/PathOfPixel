using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlot : MonoBehaviour, IDropHandler
{
    [Tooltip("What type of item can be equipped here (Weapon, Shield, etc.)")]
    public string slotType = "Any";
    
    [Tooltip("Color to show when an item can be equipped")]
    public Color validDropColor = new Color(0.8f, 1f, 0.8f, 1f);
    
    [Tooltip("Color to show when an item cannot be equipped")]
    public Color invalidDropColor = new Color(1f, 0.5f, 0.5f, 1f);
    
    private Image slotImage;
    private Color originalColor;
    private RectTransform equippedItem;
    private InventorySystem inventorySystem;
    
    void Start()
    {
        slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            originalColor = slotImage.color;
        }
        
        inventorySystem = FindObjectOfType<InventorySystem>();
        if (inventorySystem == null)
        {
            Debug.LogError("No InventorySystem found in the scene!");
        }
    }
    
    // Override to handle parent panel collider
    // Add this method to your EquipmentSlot.cs script
    public void ForceProcessDrop(PointerEventData eventData)
    {
        // Debug.Log("Force processing drop on " + gameObject.name);
        if (eventData.pointerDrag != null)
        {
            RectTransform droppedItem = eventData.pointerDrag.GetComponent<RectTransform>();
            
            // If we already have an item equipped
            if (equippedItem != null)
            {
                // Return the old item to the inventory
                ReturnItemToInventory(equippedItem);
            }
            
            // Equip the new item
            EquipItem(droppedItem);
        }
    }
    // This method is called when something is dropped on this slot
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            RectTransform droppedItem = eventData.pointerDrag.GetComponent<RectTransform>();
            
            // If we already have an item equipped
            if (equippedItem != null)
            {
                // Return the old item to the inventory
                ReturnItemToInventory(equippedItem);
            }
            
            // Equip the new item
            EquipItem(droppedItem);
        }
    }
    
    // Equip an item to this slot
    private void EquipItem(RectTransform item)
    {
        // If the item is coming from the inventory, remove it from inventory tracking
        if (inventorySystem != null)
        {
            // This is a simplified version - we'd need to add this method to InventorySystem
            if (inventorySystem.inventoryItems.ContainsKey(item))
            {
                Vector2 slotID = inventorySystem.inventoryItems[item];
                inventorySystem.RemoveItemInInventorySlot(item, slotID);
                inventorySystem.inventoryItems.Remove(item);
            }
        }
        
        // Store reference to the equipped item
        equippedItem = item;
        
        // Parent the item to this slot and center it
        item.SetParent(transform);
        
        // Center the item in the slot
        item.anchorMin = new Vector2(0.5f, 0.5f);
        item.anchorMax = new Vector2(0.5f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.anchoredPosition = Vector2.zero;
        
        // Scale to fit if needed
        FitItemToSlot(item);
        
        // Remove the event triggers from the item so it can't be dragged while equipped
        EventTrigger trigger = item.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.enabled = false;
        }
        
        // Add a double-click handler to unequip
        AddUnequipHandler(item.gameObject);
    }
    
    // Add a component to handle double-clicks to unequip
    private void AddUnequipHandler(GameObject itemObject)
    {
        // Check if it already has the component
        ItemUnequipHandler existingHandler = itemObject.GetComponent<ItemUnequipHandler>();
        if (existingHandler == null)
        {
            ItemUnequipHandler handler = itemObject.AddComponent<ItemUnequipHandler>();
            handler.equipmentSlot = this;
        }
    }
    
    // Scale the item to fit the slot
    private void FitItemToSlot(RectTransform item)
    {
        RectTransform slotRect = GetComponent<RectTransform>();
        
        // Get the slot and item sizes
        float slotWidth = slotRect.rect.width;
        float slotHeight = slotRect.rect.height;
        float itemWidth = item.rect.width;
        float itemHeight = item.rect.height;
        
        // Calculate the scale factor
        float widthScale = slotWidth / itemWidth * 0.8f; // 80% of slot size
        float heightScale = slotHeight / itemHeight * 0.8f;
        float scale = Mathf.Min(widthScale, heightScale);
        
        // Apply the new scale
        if (scale < 1.0f)
        {
            item.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            item.localScale = Vector3.one;
        }
    }
    
    // Return an item to the inventory
    public void ReturnItemToInventory(RectTransform item)
    {
        // Only do this if we have a reference to the inventory system
        if (inventorySystem != null)
        {
            // Parent back to inventory panel
            item.SetParent(inventorySystem.inventoryPanel);
            
            // Reset scale
            item.localScale = Vector3.one;
            
            // Set proper anchors for inventory items
            item.anchorMin = new Vector2(0, 0);
            item.anchorMax = new Vector2(0, 0);
            
            // Find available position
            Vector2 slotPosition = Vector2.zero;
            bool foundSlot = false;
            
            // Simple slot finder algorithm (you may need to adapt this to match your inventory system)
            for (int y = 0; y < inventorySystem.inventoryDimensions.y; y++)
            {
                for (int x = 0; x < inventorySystem.inventoryDimensions.x; x++)
                {
                    Vector2 testPos = new Vector2(x, y);
                    if (!inventorySystem.inventoryItemSlots.ContainsKey(testPos))
                    {
                        slotPosition = testPos;
                        foundSlot = true;
                        break;
                    }
                }
                if (foundSlot) break;
            }
            
            // Set position based on slot
            float offsetX = item.pivot.x * item.sizeDelta.x;
            float offsetY = item.pivot.y * item.sizeDelta.y;
            
            item.anchoredPosition = new Vector2(
                slotPosition.x * inventorySystem.tileSize.x + offsetX,
                slotPosition.y * inventorySystem.tileSize.y + offsetY
            );
            
            // Re-enable dragging
            EventTrigger trigger = item.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.enabled = true;
            }
            else
            {
                // If the trigger was removed, re-add it
                inventorySystem.AddSignalConnections(item);
            }
            
            // Add back to inventory tracking
            inventorySystem.AddItemToInventory(item);
            
            // Clear equipped item reference
            if (equippedItem == item)
            {
                equippedItem = null;
            }
        }
    }
    
    // Public method to unequip the current item
    public void UnequipCurrentItem()
    {
        if (equippedItem != null)
        {
            ReturnItemToInventory(equippedItem);
            equippedItem = null;
        }
    }
}