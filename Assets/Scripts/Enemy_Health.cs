using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    public int expReward = 3;
    public delegate void MonsterDefeated(int exp);
    public static event MonsterDefeated OnMonsterDefeated;
    public int currentHealth;
    public int maxHealth;
    
    [SerializeField] private GameObject itemDropPrefab; // Reference to your drop prefab
    [SerializeField] private float dropChance = 1.0f;   // Chance to drop (0-1)
    
    private void Start()
    {
        currentHealth = maxHealth;
    }
    
    public void ChangeHealth(int amount)
    {
        currentHealth += amount;
        if(currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if(currentHealth <= 0)
        {
            // Before destroying the enemy, drop the item
            if (itemDropPrefab != null && Random.value <= dropChance)
            {
                Instantiate(itemDropPrefab, transform.position, Quaternion.identity);
            }
            
            OnMonsterDefeated(expReward);
            Destroy(gameObject);
        }
    }
}