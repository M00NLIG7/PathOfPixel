using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;


    [Header("Combat Stats")]
    public float weaponRange;
    public int damage;


    [Header("Movement Stats")]
    public int speed;


    [Header("Health Stats")]
    public int maxHealth;
    public int currentHealth;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }
}
