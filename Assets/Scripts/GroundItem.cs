using UnityEngine;

public class GroundItem : MonoBehaviour
{
    public string itemName;
    public Sprite itemSprite;
    public int inventoryWidth = 1;  // How many tiles wide in inventory
    public int inventoryHeight = 1; // How many tiles tall in inventory
    
    private bool isInRange = false;
    private SpriteRenderer outlineRenderer;
    
    private void Start()
    {
        // Create an outline or highlight effect
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(transform);
        outline.transform.localPosition = Vector3.zero;
        
        outlineRenderer = outline.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = GetComponent<SpriteRenderer>().sprite;
        outlineRenderer.color = new Color(1f, 1f, 0.5f, 0.5f); // Yellow outline
        outlineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        outlineRenderer.enabled = false; // Start with outline disabled
        
        // Make sure outline is behind the main sprite
        outlineRenderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;
        outline.transform.localScale = new Vector3(1.1f, 1.1f, 1); // Slightly larger for outline effect
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = true;
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = true; // Show outline when in range
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = false;
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = false; // Hide outline when out of range
            }
        }
    }
    
    // Optional: Handle mouse hover for tooltips
    private void OnMouseEnter()
    {
        // You can implement tooltip display here
        // Example: UIManager.instance.ShowTooltip(itemName, transform.position);
    }
    
    private void OnMouseExit()
    {
        // Hide tooltip
        // Example: UIManager.instance.HideTooltip();
    }
    
    // Public method to check if item is in pickup range
    public bool IsInRange()
    {
        return isInRange;
    }
}