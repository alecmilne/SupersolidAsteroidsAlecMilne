using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderShift : MonoBehaviour {

    // Load of private variables to hold information about the screen
    private float minScreenX;
    private float maxScreenX;

    private float minScreenY;
    private float maxScreenY;

    private float screenHeight;
    private float screenWidth;
    
    private Vector3 offsetTop;
    private Vector3 offsetRight;
    private Vector3 offsetBottom;
    private Vector3 offsetLeft;

    private Bounds screenBounds;

    private bool initialised = false; // used to make sure the Start() code only gets called once
    // Added this as initial wave asteroids reference this script before it naturally gets initialised

    // Force initialisation, but only once
    public void ForceInit()
    {
        if (!initialised)
        {
            Start();
        }
    }

    // Get the camera and populate the min, max, etc of the game screen
    void Start()
    {
        if (!initialised)
        {
            Camera camera = Camera.main;

            float screenAspect = 1.0f * Screen.width / Screen.height;
            float cameraHeight = camera.orthographicSize * 2;
            screenBounds = new Bounds(camera.transform.position, new Vector3(cameraHeight * screenAspect, cameraHeight, 0));

            screenHeight = screenBounds.size.y;
            screenWidth = screenBounds.size.x;

            minScreenX = screenBounds.min.x;
            maxScreenX = screenBounds.max.x;

            minScreenY = screenBounds.min.y;
            maxScreenY = screenBounds.max.y;

            offsetTop = new Vector3(0, screenHeight, 0);
            offsetBottom = new Vector3(0, -screenHeight, 0);
            offsetRight = new Vector3(screenWidth, 0, 0);
            offsetLeft = new Vector3(-screenWidth, 0, 0);

            initialised = true;
        }
    }

    // For moving things from one side of the screen to the other as does original Asteroids
    void Update()
    {
        // Don't check for the initial wave asteroids until they have passed through the screen
        if (IsMovingAway() && !screenBounds.Contains(transform.position))
        {
            // Check positions and add move accordingly for min/max x/y
            if (transform.position.x > maxScreenX)
            {
                transform.position = new Vector3(minScreenX, transform.position.y, 0);
            }
            else if (transform.position.x < minScreenX)
            {
                transform.position = new Vector3(maxScreenX, transform.position.y, 0);
            }

            if (transform.position.y > maxScreenY)
            {
                transform.position = new Vector3(transform.position.x, minScreenY, 0);
            }
            else if (transform.position.y < minScreenY)
            {
                transform.position = new Vector3(transform.position.x, maxScreenY, 0);
            }
        }
    }

    // Used to get the distance required by asteroids to definitely appear off screen
    // This returns the corner distance, so asteroids add a little to this.
    public float GetDiagonalDistance()
    {
        return Mathf.Sqrt(Mathf.Pow(maxScreenX, 2) + Mathf.Pow(maxScreenY, 2));
    }

    // Just some functions to access private variables
    public float GetScreenMinX()
    {
        return minScreenX;
    }

    public float GetScreenMaxX()
    {
        return maxScreenX;
    }

    public float GetScreenMinY()
    {
        return minScreenY;
    }

    public float GetScreenMaxY()
    {
        return maxScreenY;
    }

    public Vector3 GetOffsetTop()
    {
        return offsetTop;
    }

    public Vector3 GetOffsetBottom()
    {
        return offsetBottom;
    }

    public Vector3 GetOffsetLeft()
    {
        return offsetLeft;
    }

    public Vector3 GetOffsetRight()
    {
        return offsetRight;
    }

    // Checks whether an object has been inside and is to be border shifted
    // This is only really relevant for asteroids, so it checks if it is attached to one of them.
    private bool IsMovingAway()
    {
        AsteroidController ac = GetComponent<AsteroidController>();

        if (ac != null)
        {
            if (ac.GetHasBeenInside())
            {
                // Destroy the mirrors so they get re-instanciated correctly
                ac.DestroyMirrors();

                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }
}
