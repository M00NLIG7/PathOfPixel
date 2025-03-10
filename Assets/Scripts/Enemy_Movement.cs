using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy_Movement : MonoBehaviour
{
    public float speed;
    private bool isChasing;
    private Rigidbody2D rb;
    private int facingDirection = -1;
    private Transform player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
       if(isChasing) {
        if(player.position.x > transform.position.x && facingDirection == -1||
                player.position.x < transform.position.x && facingDirection == 1
        ) {
            Flip();
        }

        Vector2 direction = (player.position - transform.position).normalized; 
        rb.linearVelocity = direction * speed;
       }
    }

    void Flip() {
        facingDirection *= 1;
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player") {
            if(player == null) {
                player = collision.transform;
            }
            isChasing = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player") {

            rb.linearVelocity = Vector2.zero;
            isChasing = false;
        }

    }
}
