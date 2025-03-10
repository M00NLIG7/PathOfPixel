using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public Animator anim;
    public float cooldown = 2;
    public float timer;
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public StatsUI statsUI;
    public AudioClip sfx1;
    public AudioSource src;

    public void Update()
    {
        if(timer > 0)
        {
            timer -= Time.deltaTime;
        }
    }
    public void Attack()
    {
        if(timer <= 0)
        {
            anim.SetBool("isAttacking", true);

            PlaySound();
            timer = cooldown;
        }
    }

    public void DealDamage()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);
        if(enemies.Length > 0)
        {
            enemies[0].GetComponent<Enemy_Health>().ChangeHealth(-StatsManager.Instance.damage);
        }
    }

    private void PlaySound()
    {
        src.clip = sfx1;
        src.Play();
    }

    public void FinishAttacking()
    {
        anim.SetBool("isAttacking", false);
        Debug.Log("Stopping attack");
    }
}
