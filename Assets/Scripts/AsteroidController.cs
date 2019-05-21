using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidController : MonoBehaviour {

    private GameController gameController; // common game controller

    private BorderShift borderShift; // needs to reference the screen sizes

    private bool hasBeenInside = true; // so that asteroids from the wave don't trigger the edge mirroring

    private Rigidbody2D rb;
    private PolygonCollider2D pc;

    // gameobjects for the screen edge mirroring. These objects pass their interactions with bullets onto this gameobject
    private GameObject childTop = null;
    private GameObject childBottom = null;
    private GameObject childLeft = null;
    private GameObject childRight = null;
    private GameObject childDiagonal = null;

    // only populated for mirrored asteroids, if original then this stays numm
    public GameObject parentAsteroid = null;

    // bool to do similar to above but simpler to check within functions
    private bool isMirror = false;

    // tracks closest the asteroid has been to the centre. This is used for tracking when the initial wave
    // asteroids have been inside the screen area and are moving away again
    private float closestFromCentre = -1.0f;

    void Start ()
    {
        // grab the relevant components
        borderShift = GetComponent<BorderShift>();
        rb = GetComponent<Rigidbody2D>();
        pc = GetComponent<PolygonCollider2D>();
        
        GameObject gameControllerObject = GameObject.FindWithTag("gameController");
        gameController = gameControllerObject.GetComponent<GameController>();
    }

    // Only update the original asteroids, not the mirror counterparts
    void Update()
    {
        if (!isMirror)
        {
            if (!hasBeenInside)
            {
                CheckForHasBeenInside(); // check until it definitely has been inside the screen space
            }

            if (hasBeenInside)
            {
                UpdateMirrorsInstances(); // when it has been inside, update the mirror instances
            }
        }
    }

    void OnTriggerEnter2D(Collider2D _c)
    {
        if (_c.gameObject.tag == "bullet" ||
            (_c.gameObject.tag == "player" && gameController.player.IsAlive())) // only bullets and alive ship can interact with asteroids
        {
            if (_c.gameObject.tag == "bullet")
            {
                // remove the bullet and tell the game controller the asteroid has been destroyed
                Destroy(_c.gameObject);

                gameController.DestroyedAsteroid();
            }

            else if (_c.gameObject.tag == "player" && gameController.player.IsKillable())
            {
                // player has been killed, but only if not invincible (checked within iskillable)
                gameController.player.RemoveShip();
            }

            // play a randomised sound for the asteroid being destroyed
            int asteroidSoundNum = Random.Range(0, gameController.soundAsteroid.Count);
            AudioSource.PlayClipAtPoint(gameController.soundAsteroid[asteroidSoundNum], Camera.main.transform.position);

            if (isMirror)
            {
                // mirror was hit, pass the destroy command to the parent
                parentAsteroid.GetComponent<AsteroidController>().BreakDownAsteroid();
                parentAsteroid.GetComponent<AsteroidController>().DestroyWithMirrors();
            }
            else
            {
                // this is an original asteroid. Instantiate smaller ones and destroy the mirrors of this asteroid
                BreakDownAsteroid();
                DestroyWithMirrors();
            }
        }
    }

    // used within border shift to simplify a function
    public bool GetHasBeenInside()
    {
        return hasBeenInside;
    }

    // setting of the private bool. Used by GameController for initialising asteroids outside of play area
    public void SetHasBeenInside(bool _hasBeenInside)
    {
        hasBeenInside = _hasBeenInside;
    }

    // Initialises an asteroid to duplicate anothers position, rotation and angular velocity.
    public void InitMovement(AsteroidController _asteroid)
    {
        rb = GetComponent<Rigidbody2D>();
        Rigidbody2D _rb = _asteroid.GetComponent<Rigidbody2D>();

        rb.velocity = _rb.velocity;
        rb.angularVelocity = _rb.angularVelocity;
        rb.transform.position = _rb.transform.position;

        Vector3 newRotation = new Vector3(_rb.transform.eulerAngles.x, _rb.transform.eulerAngles.y, _rb.transform.eulerAngles.z);
        rb.transform.eulerAngles = newRotation;
    }

    // Similar call, but randomised speed and angular velocity on whichever direction it is pointing
    public void InitMovement()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.AddForce(transform.up * Random.Range(40, 80));

        rb.angularVelocity = Random.Range(-90.0f, 90.0f);
    }

    // Convert the tag string on different asteroids into the relevant gameobject
    private GameObject GetAsteroidTypeFromTag(string _tag)
    {
        GameObject asteroidType = null;

        if (_tag == "asteroidLarge")
        {
            asteroidType = gameController.asteroidLarge;
        }
        else if (_tag == "asteroidMedium")
        {
            asteroidType = gameController.asteroidMedium;
        }
        else
        {
            asteroidType = gameController.asteroidSmall;
        }

        return asteroidType;
    }

    // Create a mirrored asteroid for the screen edge border looping
    private GameObject InstantiateMirrorAsteroid(Vector3 _offset)
    {
        GameObject newAsteroid = Instantiate(GetAsteroidTypeFromTag(this.tag));
        AsteroidController ac = newAsteroid.GetComponent<AsteroidController>();
        ac.hasBeenInside = false;
        ac.isMirror = true;
        ac.InitMovement(this); // so it copies the movement and rotation of the original
        ac.parentAsteroid = this.gameObject;

        // move the mirror to _offset away so it appears as if it is the cut-off part of the original
        newAsteroid.transform.position = new Vector3(this.transform.position.x + _offset.x, this.transform.position.y + _offset.y, 0);

        newAsteroid.transform.parent = gameController.asteroidMirrorHolder.transform;

        return newAsteroid;
    }

    // function for destroying the mirrors of this asteroid as well as itself
    public void DestroyWithMirrors()
    {
        DestroyMirrors();
        Destroy(this.gameObject);
    }

    // destroy all possible mirrors of it. Originally I had checks for null, but Destroy doesn't seem to mind being called on a null
    public void DestroyMirrors()
    {
        Destroy(childTop);
        Destroy(childBottom);
        Destroy(childRight);
        Destroy(childLeft);
        Destroy(childDiagonal);
    }

    // Check to see whether a mirror needs to be created if it doesn't exist, or needs to be destroyed if the _test is false
    private void TestForMirrorCreation(ref GameObject go, bool _test, Vector3 _offset)
    {
        if (_test)
        {
            if (go == null)
            {
                go = InstantiateMirrorAsteroid(_offset);
            }
        }
        else
        {
            Destroy(go);
        }
    }

    // function for checking if an asteroid has started moving away from the origin, meaning it has been within the screen space
    private void CheckForHasBeenInside()
    {
        float dist = Vector3.Distance(this.transform.position, Vector3.zero);

        if (closestFromCentre >= 0.0f)
        {
            if (dist > closestFromCentre)
            {
                hasBeenInside = true;
            }
            else
            {
                closestFromCentre = dist;
            }
        }
        else
        {
            closestFromCentre = dist;
        }
    }

    // Check for whether any of the possible mirror instances need to be added or removed
    private void UpdateMirrorsInstances()
    {
        TestForMirrorCreation(ref childTop, (pc.bounds.min.y < borderShift.GetScreenMinY()), borderShift.GetOffsetTop());
        TestForMirrorCreation(ref childBottom, (pc.bounds.max.y > borderShift.GetScreenMaxY()), borderShift.GetOffsetBottom());
        TestForMirrorCreation(ref childRight, (pc.bounds.min.x < borderShift.GetScreenMinX()), borderShift.GetOffsetRight());
        TestForMirrorCreation(ref childLeft, (pc.bounds.max.x > borderShift.GetScreenMaxX()), borderShift.GetOffsetLeft());
        
        // Slightly convoluted, but this adds the diagonal mirrored instance for when an asteroid straddles the corner
        if (childRight != null && childTop != null)
        {
            if (childDiagonal == null) { childDiagonal = InstantiateMirrorAsteroid(borderShift.GetOffsetRight() + borderShift.GetOffsetTop()); }
        }
        else if (childRight != null && childBottom != null)
        {
            if (childDiagonal == null) { childDiagonal = InstantiateMirrorAsteroid(borderShift.GetOffsetRight() + borderShift.GetOffsetBottom()); }
        }
        else if (childLeft != null && childTop != null)
        {
            if (childDiagonal == null) { childDiagonal = InstantiateMirrorAsteroid(borderShift.GetOffsetLeft() + borderShift.GetOffsetTop()); }
        }
        else if (childLeft != null && childBottom != null)
        {
            if (childDiagonal == null) { childDiagonal = InstantiateMirrorAsteroid(borderShift.GetOffsetLeft() + borderShift.GetOffsetBottom()); }
        }
        else if (childDiagonal != null)
        {
            // no need for a diagonal, destroy if it exists
            Destroy(childDiagonal);
        }
    }

    // This creates a new asteroid when the current one has been broken down
    // Adds random force, rotation, etc.
    private void CreateNewAsteroid(GameObject _asteroidObject)
    {
        GameObject newAsteroid = Instantiate(_asteroidObject);

        AsteroidController ac = newAsteroid.GetComponent<AsteroidController>();
        ac.InitMovement(this);
        ac.hasBeenInside = true;

        Rigidbody2D acRB = newAsteroid.GetComponent<Rigidbody2D>();

        acRB.velocity *= Random.Range(0.8f, 1.6f);

        Vector2 sideForce = Vector3.Cross(rb.velocity, Vector3.forward);
        acRB.AddForce(sideForce * (Random.Range(0,100) >= 50 ? 1 : -1) * Random.Range(40.0f, 60.0f));

        acRB.rotation = Random.Range(0, 360);
        acRB.angularVelocity = Random.Range(-90, 90);

        newAsteroid.transform.parent = this.transform.parent;
    }

    // This declared how many and what type of asteroids are instantiated when this one is shot
    public void BreakDownAsteroid()
    {
        if (tag.Equals("asteroidLarge"))
        {
            CreateNewAsteroid(gameController.asteroidMedium);
            CreateNewAsteroid(gameController.asteroidMedium);
        }
        else if (tag.Equals("asteroidMedium"))
        {
            CreateNewAsteroid(gameController.asteroidSmall);
            CreateNewAsteroid(gameController.asteroidSmall);
            CreateNewAsteroid(gameController.asteroidSmall);
        }

        gameController.UpdateAsteroidCount();
    }

}
