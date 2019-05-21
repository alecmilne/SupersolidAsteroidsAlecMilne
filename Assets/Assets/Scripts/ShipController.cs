using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    // publically accessible prefabs, holders and variables
    public GameObject bullet; // bullet prefab
    public Animator anim; // animator for the ship engines and blinking when restarted
    // please note the engine is nicely animated, I spent a bit of time working on those frames
    public GameObject[] brokenPieces; // prefabs for destroyed ship pieces
    public bool isRapidFire = false; // not used in game, but built in capability in case I got around to it
    // rapid fire is a little buggy as it is overpowered and I haven't tested it much

    // private ship controlling variables
    private bool alive = true; // this is used when the player would spawn inside an asteroid. Keep not alive until they move out of the way
    // when not alive the player can't move
    private bool isInvincible = false; // user is invincible for a short time after spawning in case about to be hit by an asteroid
    private bool isFiring = false;
    private GameController gameController;
    private Rigidbody2D rb;

    // sounds for ship destroyed and some for firing. I recorded all sounds myself, sorry if they get annoying.
    public AudioClip soundShipDestroyed;
    public List<AudioClip> soundShotFired;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject gameControllerObject = GameObject.FindWithTag("gameController");
        gameController = gameControllerObject.GetComponent<GameController>();
    }
    
    // Control loop for this ship. Fire when needed, move when needed.
    void FixedUpdate()
    {
        if (alive)
        {
            // can only fire when alive.
            CheckFiring();

            // get the right inputs for using the engine and turning
            UpdateMovement();

            // slightly odd way of doing it, but initially I wanted to have multiple ways of firing, so kept this in.
            if (isFiring)
            {
                Fire();
            }
        }
        else
        {
            // check when the ship can be alive again
            UpdateNotAlive();
        }

        // rapid fire isn't a game feature but you can enable it in the inspector and have an easy game.
        // note, game is slightly buggy with it on
        if (!isRapidFire)
        {
            isFiring = false;
        }
    }

    // Check if the ship has collided with an asteroid
    void OnTriggerEnter2D(Collider2D _c)
    {
        if (alive && !isInvincible)
        {
            if (_c.gameObject.tag != "bullet")
            {
                // ship has been hit by something other than a bullet, which in this game means it is an asteroid
                RemoveShip();

                gameController.DecrementLives();
            }
        }
    }

    // some public functions for accessing private variables
    public bool IsAlive()
    {
        return alive;
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }

    public bool IsKillable()
    {
        return alive && !isInvincible;
    }
    
    // Check the keyboard for if the player is firing
    private void CheckFiring()
    {
        if (isFiring)
        {
            if (Input.GetKeyUp("space"))
            {
                isFiring = false;
            }
        }
        else
        {
            if (Input.GetKeyDown("space"))
            {
                isFiring = true;
            }
        }
    }

    // check the keyboard for movement commands
    // also trigger the animation for engines
    private void UpdateMovement()
    {
        transform.Rotate(0, 0, -Input.GetAxis("Horizontal") * 500.0f * Time.deltaTime);

        float powerValue = Input.GetAxis("Vertical");

        if (powerValue > 0)
        {
            rb.AddForce(transform.up * 3.0f * Input.GetAxis("Vertical"));
            anim.SetBool("isEngineOn", true);
        }
        else
        {
            anim.SetBool("isEngineOn", false);
        }
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, 7.0f); // stop the player going too fast
    }

    // get the number of objects overlapping the recently reset player ship
    private int GetCountCollidingObjects()
    {
        PolygonCollider2D myCollider = gameObject.transform.GetComponent<PolygonCollider2D>();
        int numColliders = 10;
        PolygonCollider2D[] colliders = new PolygonCollider2D[numColliders];
        ContactFilter2D contactFilter = new ContactFilter2D
        {
            useTriggers = true
        };

        return myCollider.OverlapCollider(contactFilter, colliders);
    }

    // check how many objects overlapping, when none then allow the user to control the ship again
    private void UpdateNotAlive()
    {
        int colliderCount = GetCountCollidingObjects();

        if (colliderCount == 0)
        {
            alive = true;

            Invoke("InvincibleTimer", 2); // start a 2s timer to take off invincibility
        }
    }
    
    // the player has been hit by an asteroid, restart the player ship to the centre of the screen
    // also instantiate the broken ship pieces with the original position, rotation, velocity + add random movement, rotation, etc.
    public void RemoveShip()
    {
        for (int i = 0; i < brokenPieces.Length; ++i)
        {
            GameObject brokenPiece = Instantiate(brokenPieces[i], transform.position, transform.rotation);

            Rigidbody2D bpRB = brokenPiece.GetComponent<Rigidbody2D>();
            bpRB.velocity = rb.velocity;
            bpRB.AddForce(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), 0));

            Destroy(brokenPiece, 3.0f);
        }

        AudioSource.PlayClipAtPoint(soundShipDestroyed, Camera.main.transform.position);

        CancelInvoke(); // cancel the timer for invincibility
        alive = false;
        ResetShip();
    }

    // make the ship immune to collisions with asteroids and make it blink to indicate this.
    // used when starting a new life in case there are unavoidable asteroids coming towards the player
    private void SetInvincible()
    {
        anim.SetBool("isBlinking", true);
        isInvincible = true;
    }

    // move the ship to the origin and reset rotation and velocty
    private void ResetShip()
    {
        transform.position = new Vector3(0, 0, 0);
        transform.rotation = Quaternion.identity;
        GetComponent<Rigidbody2D>().velocity = new Vector3(0, 0, 0);

        SetInvincible();
    }
    
    // stop the invincibility setting
    private void InvincibleTimer()
    {
        anim.SetBool("isBlinking", false);
        isInvincible = false;
        return;
    }

    // fire a bullet
    // instantiates a bullet ahead of the player with the correct rotation and adds it to the bullet holder
    private void Fire()
    {
        int fireSoundNum = Random.Range(0, soundShotFired.Count);
        AudioSource.PlayClipAtPoint(soundShotFired[fireSoundNum], Camera.main.transform.position);

        Vector3 bulletPosition = new Vector3(transform.position.x, transform.position.y, 0) + transform.up * 0.53f;

        GameObject newBullet = Instantiate(bullet, bulletPosition, transform.rotation);
        newBullet.transform.parent = gameController.bulletHolder.transform;

        gameController.FiredShot(); // decrease the score for every shot fired
    }
}
