using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class InventorySystem : MonoBehaviour
{

    public List<GameObject> equipmentSlots = new List<GameObject>();
    public Sprite defaultItemSprite; // Fallback sprite if custom sprites are missing

    [Header("Item Pickup")]
    public LayerMask itemLayerMask;
    public float pickupRange = 2.0f;
    public Transform playerTransform; // Reference to player position
    private Camera gameCamera;


    [Header("Inventory Configuration")]
    public Vector2 tileSize = new Vector2(32, 32);
    public Vector2 inventoryDimensions = new Vector2(8, 8);
    public int selectedItemZIndex = 1000;
    
    [Header("UI References")]
    public RectTransform inventoryPanel;
    public Image inventoryGrids;
    public BoxCollider2D inventoryCollider;
    
    [Header("Visual Settings")]
    public Color invalidColor = new Color(1f, 0.36f, 0.36f, 1f);
    public Color validColor = new Color(1f, 1f, 1f, 1f);
    public Color shieldColor = new Color(0.3f, 0.3f, 0.8f, 1f); // Fallback color
    public Color swordColor = new Color(0.8f, 0.3f, 0.3f, 1f); // Fallback color
    
    [Header("Item Sprites")]
    public Sprite shieldSprite; // Assign in inspector
    public Sprite swordSprite;  // Assign in inspector
    
    private bool isInventoryVisible = false; // Start with inventory hidden
    
    // Dictionary to store original colors of items
    private Dictionary<RectTransform, Color> itemOriginalColors = new Dictionary<RectTransform, Color>();
    
    // Dynamic variables
    public Dictionary<Vector2, RectTransform> inventoryItemSlots = new Dictionary<Vector2, RectTransform>();
    public Dictionary<RectTransform, Vector2> inventoryItems = new Dictionary<RectTransform, Vector2>();
    private bool isItemSelected = false;
    private RectTransform selectedItem;
    private bool isDraggingItem = false;
    private Vector2 cursorItemDragOffset = Vector2.zero;
    
    private List<RectTransform> overlappingWithItems = new List<RectTransform>();
    private Vector2 itemPrevPosition;
    private bool isSelectedItemInsideInventory = true; // Default to true for simplicity

    void Start()
    {

        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogError("Main Camera not found! Make sure your Main Cam is tagged as 'MainCamera'");
            // Try to find it by name as fallback
            gameCamera = GameObject.Find("Main Cam")?.GetComponent<Camera>();
            if (gameCamera == null)
            {
                Debug.LogError("Couldn't find camera even by name 'Main Cam'");
            }
        }

        // Setup inventory panel size - make sure this is consistent with your inspector settings
        inventoryPanel.sizeDelta = new Vector2(tileSize.x * inventoryDimensions.x, tileSize.y * inventoryDimensions.y);
        
        // Ensure proper anchoring for the panel
        inventoryPanel.anchorMin = new Vector2(0, 0);
        inventoryPanel.anchorMax = new Vector2(0, 0);
        inventoryPanel.pivot = new Vector2(0, 0);


        
        // Setup inventory grid visualization - ensure it matches the panel exactly
        if (inventoryGrids != null)
        {
            RectTransform gridRect = inventoryGrids.rectTransform;
            gridRect.sizeDelta = inventoryPanel.sizeDelta;
            
            // Make sure the grid image stretches to fill the panel
            gridRect.anchorMin = new Vector2(0, 0);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;
            
            // Debug grid size to ensure it's correct
            Debug.Log("Grid Size: " + gridRect.sizeDelta);
        }
        
        // Setup inventory collider - ensure it matches panel exactly
        if (inventoryCollider != null)
        {
            inventoryCollider.size = inventoryPanel.sizeDelta;
            inventoryCollider.offset = new Vector2(inventoryCollider.size.x / 2, inventoryCollider.size.y / 2);
            inventoryCollider.isTrigger = true;
            
            // Debug collider size
            Debug.Log("Collider Size: " + inventoryCollider.size);
            Debug.Log("Collider Offset: " + inventoryCollider.offset);
        }
        
        // Setup existing items
        foreach (var item in GameObject.FindGameObjectsWithTag("Item"))
        {
            AddSignalConnections(item.GetComponent<RectTransform>());
        }
    
        AddDefaultItems();
        
        InitiateSortInventory();
        SetInventoryVisibility(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isInventoryVisible)
        {
            // Check if we clicked on UI element
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                TryPickupItemAtMousePosition();
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (isInventoryVisible && isDraggingItem && selectedItem != null)
        {
            // Get the mouse position in screen space
            Vector2 mousePos = Input.mousePosition;
            
            // Convert to local position in the panel
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                inventoryPanel, 
                mousePos,
                null, // Use null for screen space overlay canvas
                out Vector2 localPoint))
            {
                // Calculate position with pivot offset
                Vector2 pivotOffset = new Vector2(
                    selectedItem.pivot.x * selectedItem.sizeDelta.x,
                    selectedItem.pivot.y * selectedItem.sizeDelta.y
                );
                
                // Check if the mouse is within the inventory bounds
                isSelectedItemInsideInventory = IsPointInsideInventory(localPoint);
                
                // Snap to grid (accounting for pivot)
                Vector2 snappedPosition = new Vector2(
                    Mathf.Floor(localPoint.x / tileSize.x) * tileSize.x,
                    Mathf.Floor(localPoint.y / tileSize.y) * tileSize.y
                );
                
                // Apply pivot offset for display
                snappedPosition += pivotOffset;
                
                // Check for overlap with other items at this position
                Vector2 slotID = new Vector2(
                    Mathf.Floor((snappedPosition.x - pivotOffset.x) / tileSize.x),
                    Mathf.Floor((snappedPosition.y - pivotOffset.y) / tileSize.y)
                );
                
                Vector2 itemSlotSize = new Vector2(
                    Mathf.Ceil(selectedItem.sizeDelta.x / tileSize.x),
                    Mathf.Ceil(selectedItem.sizeDelta.y / tileSize.y)
                );
                
                bool overlapsWithOtherItems = false;
                for (int y = 0; y < itemSlotSize.y; y++)
                {
                    for (int x = 0; x < itemSlotSize.x; x++)
                    {
                        Vector2 checkSlot = new Vector2(slotID.x + x, slotID.y + y);
                        if (inventoryItemSlots.ContainsKey(checkSlot) && 
                            inventoryItemSlots[checkSlot] != selectedItem)
                        {
                            overlapsWithOtherItems = true;
                            break;
                        }
                    }
                    if (overlapsWithOtherItems) break;
                }
                
                // Apply position directly
                selectedItem.anchoredPosition = snappedPosition;
                
                // Change color based on whether it's inside inventory and not overlapping
                Image itemImage = selectedItem.GetComponentInChildren<Image>();
                if (itemImage != null)
                {
                    if (!isSelectedItemInsideInventory || overlapsWithOtherItems)
                    {
                        itemImage.color = invalidColor;
                    }
                    else if (itemOriginalColors.ContainsKey(selectedItem))
                    {
                        itemImage.color = itemOriginalColors[selectedItem];
                    }
                }
            }
        }
    }

private void SetInventoryVisibility(bool visible)
{
    // Enable/disable the inventory panel
    inventoryPanel.gameObject.SetActive(visible);
    
    // Also toggle all equipment slots
    foreach (GameObject slot in equipmentSlots)
    {
        if (slot != null)
            slot.SetActive(visible);
    }
    
    // If closing inventory, drop any selected item
    if (!visible && selectedItem != null)
    {
        // Reset the selected item
        Image itemImage = selectedItem.GetComponentInChildren<Image>();
        if (itemImage != null && itemOriginalColors.ContainsKey(selectedItem))
        {
            itemImage.color = itemOriginalColors[selectedItem];
        }
        
        isItemSelected = false;
        isDraggingItem = false;
        selectedItem = null;
    }
}
    
    public void ToggleInventory()
    {
        isInventoryVisible = !isInventoryVisible;
        SetInventoryVisibility(isInventoryVisible);
    }

    // Check if a point is inside the inventory bounds
    private bool IsPointInsideInventory(Vector2 point)
    {
        // Calculate inventory bounds
        float inventoryWidth = tileSize.x * inventoryDimensions.x;
        float inventoryHeight = tileSize.y * inventoryDimensions.y;
        
        // Simple rectangle check
        return (point.x >= 0 && point.x <= inventoryWidth && 
                point.y >= 0 && point.y <= inventoryHeight);
    }
    
    // Check if a rect (item) is inside the inventory bounds
    private bool IsRectInsideInventory(RectTransform rect)
    {
        // Get the corners of the rect
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        
        // Convert corners to local space of inventory panel
        for (int i = 0; i < 4; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                inventoryPanel,
                RectTransformUtility.WorldToScreenPoint(null, corners[i]),
                null,
                out Vector2 localCorner
            );
            
            // If any corner is outside, return false
            if (!IsPointInsideInventory(localCorner))
            {
                return false;
            }
        }
        
        return true;
    }

public void AddSignalConnections(RectTransform item)
{
    // Store the original color
    Image itemImage = item.GetComponentInChildren<Image>();
    if (itemImage != null && !itemOriginalColors.ContainsKey(item))
    {
        itemOriginalColors[item] = itemImage.color;
        Debug.Log($"Stored original color for {item.name}: {itemOriginalColors[item]}");
    }
    
    // Remove any existing triggers to avoid duplicates
    EventTrigger existingTrigger = item.gameObject.GetComponent<EventTrigger>();
    if (existingTrigger != null)
    {
        Destroy(existingTrigger);
    }
    
    // Add drag handlers
    EventTrigger trigger = item.gameObject.AddComponent<EventTrigger>();
    
    // PointerDown
    EventTrigger.Entry pointerDown = new EventTrigger.Entry();
    pointerDown.eventID = EventTriggerType.PointerDown;
    pointerDown.callback.AddListener((data) => {
        isItemSelected = true;
        selectedItem = item;
        
        // Ensure it appears on top while dragging - UPDATED CODE
        Image itemImg = selectedItem.GetComponentInChildren<Image>();
        if (itemImg != null && itemImg.canvas != null)
        {
            itemImg.canvas.sortingOrder = selectedItemZIndex;
        }
        else
        {
            // If we can't find the canvas through the image, try to find it in parents
            Canvas parentCanvas = selectedItem.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.sortingOrder = selectedItemZIndex;
            }
            else
            {
                Debug.LogWarning("No canvas found for item: " + item.name);
            }
        }
        
        itemPrevPosition = selectedItem.anchoredPosition;
        Debug.Log("Selected item: " + item.name + " at position: " + itemPrevPosition);
    });
    trigger.triggers.Add(pointerDown);
    
    // Drag
    EventTrigger.Entry drag = new EventTrigger.Entry();
    drag.eventID = EventTriggerType.Drag;
    drag.callback.AddListener((data) => {
        if (isItemSelected)
        {
            isDraggingItem = true;
        }
    });
    trigger.triggers.Add(drag);
    
    // PointerUp
    EventTrigger.Entry pointerUp = new EventTrigger.Entry();
    pointerUp.eventID = EventTriggerType.PointerUp;
    pointerUp.callback.AddListener((data) => {
        if (selectedItem != null)
        {
            // Reset Z-index
            Image itemImg = selectedItem.GetComponentInChildren<Image>();
            if (itemImg != null && itemImg.canvas != null)
            {
                itemImg.canvas.sortingOrder = 0; // Reset to default
            }
            else
            {
                Canvas parentCanvas = selectedItem.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    parentCanvas.sortingOrder = 0;
                }
            }
            
            // Calculate position with pivot offset
            Vector2 pivotOffset = new Vector2(
                selectedItem.pivot.x * selectedItem.sizeDelta.x,
                selectedItem.pivot.y * selectedItem.sizeDelta.y
            );
            
            Vector2 slotID = new Vector2(
                Mathf.Floor((selectedItem.anchoredPosition.x - pivotOffset.x) / tileSize.x),
                Mathf.Floor((selectedItem.anchoredPosition.y - pivotOffset.y) / tileSize.y)
            );
            
            Vector2 itemSlotSize = new Vector2(
                Mathf.Ceil(selectedItem.sizeDelta.x / tileSize.x),
                Mathf.Ceil(selectedItem.sizeDelta.y / tileSize.y)
            );
            
            // Check for overlaps with other items
            bool overlapsWithOtherItems = false;
            for (int y = 0; y < itemSlotSize.y; y++)
            {
                for (int x = 0; x < itemSlotSize.x; x++)
                {
                    Vector2 checkSlot = new Vector2(slotID.x + x, slotID.y + y);
                    if (inventoryItemSlots.ContainsKey(checkSlot) && 
                        inventoryItemSlots[checkSlot] != selectedItem)
                    {
                        overlapsWithOtherItems = true;
                        break;
                    }
                }
                if (overlapsWithOtherItems) break;
            }
            
            // Process item placement
            if (overlapsWithOtherItems || !isSelectedItemInsideInventory)
            {
                // Return to previous position if overlapping or outside
                selectedItem.anchoredPosition = itemPrevPosition;
                Debug.Log("Item returned due to invalid placement: " + selectedItem.name);
            }
            else
            {
                // Try to add to inventory
                if (!AddItemToInventory(selectedItem))
                {
                    // Return to previous position if can't be added
                    selectedItem.anchoredPosition = itemPrevPosition;
                    Debug.Log("Item returned due to invalid grid position: " + selectedItem.name);
                }
                else
                {
                    Debug.Log("Item placed in inventory: " + selectedItem.name);
                }
            }
            
            // Reset color to original color
            Image itemImage = selectedItem.GetComponentInChildren<Image>();
            if (itemImage != null && itemOriginalColors.ContainsKey(selectedItem))
            {
                itemImage.color = itemOriginalColors[selectedItem];
                Debug.Log($"Reset color for {selectedItem.name} to original: {itemOriginalColors[selectedItem]}");
            }
            
            isItemSelected = false;
            isDraggingItem = false;
            selectedItem = null;
        }
    });
    trigger.triggers.Add(pointerUp);
}

    public void OverlappingWithOtherItem(RectTransform item)
    {
        if (selectedItem == item)
            return;
            
        overlappingWithItems.Add(item);
        
        if (selectedItem != null)
        {
            Image itemImage = selectedItem.GetComponentInChildren<Image>();
            if (itemImage != null)
            {
                itemImage.color = invalidColor;
            }
        }
    }

    public void NotOverlappingWithOtherItem(RectTransform item)
    {
        if (selectedItem == item)
            return;
            
        overlappingWithItems.Remove(item);
        
        if (overlappingWithItems.Count == 0 && isItemSelected && isSelectedItemInsideInventory)
        {
            Image itemImage = selectedItem.GetComponentInChildren<Image>();
            if (itemImage != null && itemOriginalColors.ContainsKey(selectedItem))
            {
                itemImage.color = itemOriginalColors[selectedItem];
            }
        }
    }

    public bool AddItemToInventory(RectTransform item)
    {
        // Calculate position with pivot offset
        Vector2 pivotOffset = new Vector2(
            item.pivot.x * item.sizeDelta.x,
            item.pivot.y * item.sizeDelta.y
        );
        
        Vector2 adjustedPosition = item.anchoredPosition - pivotOffset;
        
        Vector2 slotID = new Vector2(
            Mathf.Floor(adjustedPosition.x / tileSize.x),
            Mathf.Floor(adjustedPosition.y / tileSize.y)
        );
        
        Vector2 itemSlotSize = new Vector2(
            Mathf.Ceil(item.sizeDelta.x / tileSize.x),
            Mathf.Ceil(item.sizeDelta.y / tileSize.y)
        );
        
        Vector2 itemMaxSlotID = slotID + itemSlotSize - new Vector2(1, 1);
        Vector2 inventorySlotBounds = inventoryDimensions - new Vector2(1, 1);
        
        // Debug placement attempt
        Debug.Log($"Trying to place item: {item.name}, Slot: {slotID}, Size: {itemSlotSize}, MaxSlot: {itemMaxSlotID}");
        
        if (itemMaxSlotID.x > inventorySlotBounds.x || itemMaxSlotID.y > inventorySlotBounds.y || 
            slotID.x < 0 || slotID.y < 0)
        {
            Debug.Log("Failed: Item would extend beyond inventory bounds");
            return false;
        }
        
        // Check if this is the same position the item already occupies
        if (inventoryItems.ContainsKey(item) && inventoryItems[item] == slotID)
        {
            // Item is already in this position, just make sure it's properly snapped
            item.anchoredPosition = new Vector2(
                slotID.x * tileSize.x,
                slotID.y * tileSize.y
            ) + pivotOffset;
            
            return true;
        }
        
        // Remove from old position if it was already in inventory
        if (inventoryItems.ContainsKey(item))
        {
            RemoveItemInInventorySlot(item, inventoryItems[item]);
        }
        
        // Check if any slots are already occupied by OTHER items
        for (int y = 0; y < itemSlotSize.y; y++)
        {
            for (int x = 0; x < itemSlotSize.x; x++)
            {
                Vector2 slotPosition = new Vector2(slotID.x + x, slotID.y + y);
                if (inventoryItemSlots.ContainsKey(slotPosition))
                {
                    RectTransform existingItem = inventoryItemSlots[slotPosition];
                    if (existingItem != item)
                    {
                        Debug.Log($"Failed: Slot {slotPosition} is already occupied by {existingItem.name}");
                        return false;
                    }
                }
            }
        }
        
        // All slots are available, so add the item
        for (int y = 0; y < itemSlotSize.y; y++)
        {
            for (int x = 0; x < itemSlotSize.x; x++)
            {
                Vector2 slotPosition = new Vector2(slotID.x + x, slotID.y + y);
                inventoryItemSlots[slotPosition] = item;
            }
        }
        
        inventoryItems[item] = slotID;
        
        // Snap the item to the exact grid position, with pivot offset
        item.anchoredPosition = new Vector2(
            slotID.x * tileSize.x,
            slotID.y * tileSize.y
        ) + pivotOffset;
        
        Debug.Log($"Successfully placed {item.name} at slot {slotID}");
        return true;
    }

    public void RemoveItemInInventorySlot(RectTransform item, Vector2 existingSlotID)
    {
        Vector2 itemSlotSize = new Vector2(
            Mathf.Ceil(item.sizeDelta.x / tileSize.x),
            Mathf.Ceil(item.sizeDelta.y / tileSize.y)
        );
        
        for (int y = 0; y < itemSlotSize.y; y++)
        {
            for (int x = 0; x < itemSlotSize.x; x++)
            {
                Vector2 slotPosition = new Vector2(existingSlotID.x + x, existingSlotID.y + y);
                if (inventoryItemSlots.ContainsKey(slotPosition))
                {
                    inventoryItemSlots.Remove(slotPosition);
                }
            }
        }
    }

    public void InitiateSortInventory()
    {
        if (inventoryItems.Count == 0)
        {
            Debug.Log("No items to sort");
            return;
        }
        
        Debug.Log("Starting inventory sort");
        
        // Remove event connections
        foreach (var item in inventoryItems.Keys)
        {
            RemoveSignalConnections(item);
        }
        
        // Convert dictionary to list for sorting
        List<KeyValuePair<RectTransform, Vector2>> itemsToSort = new List<KeyValuePair<RectTransform, Vector2>>();
        foreach (var item in inventoryItems)
        {
            Vector2 itemSlotSize = new Vector2(
                Mathf.Ceil(item.Key.sizeDelta.x / tileSize.x),
                Mathf.Ceil(item.Key.sizeDelta.y / tileSize.y)
            );
            
            itemsToSort.Add(new KeyValuePair<RectTransform, Vector2>(item.Key, itemSlotSize));
        }
        
        // Sort by height priority
        itemsToSort = itemsToSort.OrderByDescending(i => i.Value.y)
                                .ThenByDescending(i => i.Value.x)
                                .ToList();
        
        Debug.Log($"Sorting {itemsToSort.Count} items");
                                
        if (!SortInventory(itemsToSort))
        {
            Debug.Log("Height priority sort failed, trying width priority");
            // Try width priority if height priority didn't work well
            itemsToSort = itemsToSort.OrderByDescending(i => i.Value.x)
                                    .ThenByDescending(i => i.Value.y)
                                    .ToList();
            SortInventory(itemsToSort);
        }
        
        // Reconnect events
        foreach (var item in inventoryItems.Keys)
        {
            AddSignalConnections(item);
        }
        
        Debug.Log("Inventory sort completed");
    }

    public bool SortInventory(List<KeyValuePair<RectTransform, Vector2>> itemsToSort)
    {
        // Clear existing slots
        inventoryItemSlots.Clear();
        
        // Create list of all possible slot positions
        List<Vector2> inventoryBlankSlots = new List<Vector2>();
        for (int x = 0; x < inventoryDimensions.x; x++)
        {
            for (int y = 0; y < inventoryDimensions.y; y++)
            {
                inventoryBlankSlots.Add(new Vector2(x, y));
            }
        }
        
        // Try to place each item in the inventory
        foreach (var itemPair in itemsToSort)
        {
            RectTransform item = itemPair.Key;
            Vector2 itemSlotSize = itemPair.Value;
            
            bool isSlotAvailable = false;
            List<Vector2> assignedSlots = new List<Vector2>();
            Vector2 upperLeftSlotID = Vector2.zero;
            
            // Try each blank slot as a possible position
            foreach (var blankSlot in inventoryBlankSlots)
            {
                isSlotAvailable = true;
                assignedSlots.Clear();
                
                // Check if the item dimensions fit in the inventory
                for (int x = 0; x < itemSlotSize.x; x++)
                {
                    for (int y = 0; y < itemSlotSize.y; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            upperLeftSlotID = blankSlot;
                        }
                        
                        Vector2 slotID = blankSlot + new Vector2(x, y);
                        
                        // Check if slot is within inventory bounds
                        if (slotID.x >= inventoryDimensions.x || slotID.y >= inventoryDimensions.y)
                        {
                            isSlotAvailable = false;
                            assignedSlots.Clear();
                            break;
                        }
                        
                        // Check if slot is available
                        if (inventoryBlankSlots.Contains(slotID))
                        {
                            assignedSlots.Add(slotID);
                        }
                        else
                        {
                            isSlotAvailable = false;
                            assignedSlots.Clear();
                            break;
                        }
                    }
                    
                    if (!isSlotAvailable)
                    {
                        break;
                    }
                }
                
                // If a suitable slot is found, place the item
                if (isSlotAvailable)
                {
                    // Remove slots from available slots
                    foreach (var assignedSlotID in assignedSlots)
                    {
                        inventoryBlankSlots.Remove(assignedSlotID);
                        inventoryItemSlots[assignedSlotID] = item;
                    }
                    
                    // Calculate pivot offset
                    Vector2 pivotOffset = new Vector2(
                        item.pivot.x * item.sizeDelta.x,
                        item.pivot.y * item.sizeDelta.y
                    );
                    
                    // Position the item with pivot offset
                    item.anchoredPosition = new Vector2(
                        upperLeftSlotID.x * tileSize.x,
                        upperLeftSlotID.y * tileSize.y
                    ) + pivotOffset;
                    
                    // Update inventory tracking
                    inventoryItems[item] = upperLeftSlotID;
                    
                    Debug.Log($"Placed {item.name} at {upperLeftSlotID}");
                    break;
                }
            }
            
            // If any item couldn't be placed, return false
            if (!isSlotAvailable)
            {
                Debug.LogWarning($"Could not place {item.name} during sort");
                return false;
            }
        }
        
        return true;
    }

private void AddDefaultItems()
{
    // Create a shield item
    GameObject shieldItem = new GameObject("DefaultShieldGroundItem");
    GroundItem shieldGroundItem = shieldItem.AddComponent<GroundItem>();
    shieldGroundItem.itemName = "DefaultShield";
    shieldGroundItem.itemSprite = shieldSprite != null ? shieldSprite : defaultItemSprite;
    shieldGroundItem.inventoryWidth = 2;
    shieldGroundItem.inventoryHeight = 2;
    
    // Add the shield to inventory
    AddItemFromGround(shieldGroundItem);
    
    // Destroy the temporary ground item GameObject
    Destroy(shieldItem);
    
    // Create a sword item
    GameObject swordItem = new GameObject("DefaultSwordGroundItem");
    GroundItem swordGroundItem = swordItem.AddComponent<GroundItem>();
    swordGroundItem.itemName = "DefaultSword";
    swordGroundItem.itemSprite = swordSprite != null ? swordSprite : defaultItemSprite;
    swordGroundItem.inventoryWidth = 1;
    swordGroundItem.inventoryHeight = 3;
    
    // Add the sword to inventory
    AddItemFromGround(swordGroundItem);
    
    // Destroy the temporary ground item GameObject
    Destroy(swordItem);
    
    Debug.Log("Default items added to inventory");
}
    
    public void RemoveSignalConnections(RectTransform item)
    {
        // Remove event trigger
        EventTrigger trigger = item.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            Destroy(trigger);
        }
    }

private void TryPickupItemAtMousePosition()
{
    // Check if required reggferences exist
    if (gameCamera == null || playerTransform == null)
    {
        Debug.LogError("Missing required references for item pickup. Check mainCamera and playerTransform.");
        return;
    }
    
    // Cast a ray from the camera through the mouse position
    Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
    RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, itemLayerMask);
    
    // If we hit a ground item and it's within range of the player
    if (hit.collider != null)
    {
        GroundItem groundItem = hit.collider.GetComponent<GroundItem>();
        if (groundItem != null)
        {
            // Check if the item is within pickup range of the player
            float distanceToItem = Vector2.Distance(playerTransform.position, hit.collider.transform.position);
            if (distanceToItem <= pickupRange)
            {
                // Add item to inventory
                AddItemFromGround(groundItem);
            }
            else
            {
                // Optional: Show "too far away" message
                Debug.Log("Item is too far away to pick up");
            }
        }
    }
}

// Method to add the ground item to inventory
public void AddItemFromGround(GroundItem groundItem)
{
    // Create the inventory UI item
    GameObject inventoryItem = new GameObject(groundItem.itemName);
    inventoryItem.transform.SetParent(inventoryPanel.transform, false);
    RectTransform itemRect = inventoryItem.AddComponent<RectTransform>();
    inventoryItem.AddComponent<CanvasRenderer>();
    
    // Center pivot and anchors for better image positioning
    itemRect.anchorMin = new Vector2(0, 0);
    itemRect.anchorMax = new Vector2(0, 0);
    itemRect.pivot = new Vector2(0.5f, 0.5f); // Center pivot point
    
    // Set size based on the item's dimensions
    itemRect.sizeDelta = new Vector2(
        tileSize.x * groundItem.inventoryWidth, 
        tileSize.y * groundItem.inventoryHeight
    );
    
    // Add image component with item sprite
    Image itemImage = inventoryItem.AddComponent<Image>();
    itemImage.sprite = groundItem.itemSprite;
    itemImage.color = Color.white;
    itemImage.preserveAspect = true;
    
    // Store original color for highlighting
    itemOriginalColors[itemRect] = Color.white;
    
    // Position at a default location initially
    float offsetX = itemRect.pivot.x * itemRect.sizeDelta.x;
    float offsetY = itemRect.pivot.y * itemRect.sizeDelta.y;
    
    // Try to find first available position in inventory 
    Vector2 availablePosition = FindFirstAvailablePosition(itemRect);
    itemRect.anchoredPosition = new Vector2(
        availablePosition.x * tileSize.x + offsetX, 
        availablePosition.y * tileSize.y + offsetY
    );
    
    // Tag as Item for detection
    inventoryItem.tag = "Item";
    
    // Add to inventory tracking
    AddSignalConnections(itemRect);
    AddItemToInventory(itemRect);
    
    // Remove the ground item
    Destroy(groundItem.gameObject);
    
    // Show the inventory briefly to let the player see the new item
    StartCoroutine(FlashInventory());
    
    // Log pickup
    Debug.Log("Picked up: " + groundItem.itemName);
}

// Briefly show inventory when picking up an item
private IEnumerator FlashInventory()
{
    bool wasVisible = isInventoryVisible;
    
    if (!wasVisible)
    {
        SetInventoryVisibility(true);
        isInventoryVisible = true;
    }
    
    yield return new WaitForSeconds(1.0f);
    
    if (!wasVisible)
    {
        SetInventoryVisibility(false);
        isInventoryVisible = false;
    }
}

// Helper to find first available position in inventory
private Vector2 FindFirstAvailablePosition(RectTransform item)
{
    Vector2 itemSlotSize = new Vector2(
        Mathf.Ceil(item.sizeDelta.x / tileSize.x),
        Mathf.Ceil(item.sizeDelta.y / tileSize.y)
    );
    
    // Try each slot in the inventory
    for (int y = 0; y < inventoryDimensions.y; y++)
    {
        for (int x = 0; x < inventoryDimensions.x; x++)
        {
            Vector2 slotPosition = new Vector2(x, y);
            bool canPlace = true;
            
            // Check if all required slots are free
            for (int iy = 0; iy < itemSlotSize.y; iy++)
            {
                for (int ix = 0; ix < itemSlotSize.x; ix++)
                {
                    Vector2 checkSlot = new Vector2(slotPosition.x + ix, slotPosition.y + iy);
                    
                    // Check if slot is within bounds
                    if (checkSlot.x >= inventoryDimensions.x || checkSlot.y >= inventoryDimensions.y)
                    {
                        canPlace = false;
                        break;
                    }
                    
                    // Check if slot is occupied
                    if (inventoryItemSlots.ContainsKey(checkSlot))
                    {
                        canPlace = false;
                        break;
                    }
                }
                
                if (!canPlace) break;
            }
            
            // If we can place the item here, return this position
            if (canPlace)
            {
                return slotPosition;
            }
        }
    }
    
    // If no position found, return a default position outside the inventory
    // The item will be auto-sorted later or can be manually placed
    return new Vector2(inventoryDimensions.x + 1, 0);
}
}