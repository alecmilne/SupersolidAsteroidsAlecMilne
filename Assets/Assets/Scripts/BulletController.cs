using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour {

    // very simple script for making the bullet move for a specific amount of time

    public int speed = 800;
    public float killTime = 1.0f;

	void Start ()
    {
        Destroy(gameObject, killTime); // set up destroy on delay
        
        GetComponent<Rigidbody2D>().AddForce(transform.up * speed); // add force to bullet in direction it is facing
    }
}
