using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GameController : MonoBehaviour {

    // Holds the prefabs for the asteroid types
    public GameObject asteroidLarge;
    public GameObject asteroidMedium;
    public GameObject asteroidSmall;

    public ShipController player; // player ship gameobject with relevant script
    public GameObject bullet; // prefab for bullets

    public GameObject asteroidHolder; // holds all the asteroids for the current wave
    public GameObject asteroidMirrorHolder; // this holds all the mirrored asteroids
                                            /*
                                             * please note that they straddle the screen edge
                                             * I spent a good chunk of time doing that feature
                                             * 
                                             */

    public GameObject bulletHolder; // holds all the bullets

    // holders for the display of game info
    public Text scoreText;
    public Text livesText;
    public Text waveText;
    public Text highScoreText;

    public List<AudioClip> soundAsteroid; // list of sound options for destroying asteroids

    // readonly values for starting lives and how much you get for destroying an asteroid
    private readonly int startingLives = 10;
    private readonly int pointsPerAsteroid = 100;
    private readonly int costPerShot = 1; // the score goes down by 1 for every shot

    // live variables for holding game info
    private int numAsteroids = 0;
    private int score = 0;
    private int lives = 0;
    private int wave = 0;
    private int highScore = 0;
    

    void Start ()
    {
        BeginGame();
    }

    // Re-initialise the game variables and check if high score has happened.
    // Update text reflecting these changes
    private void ResetDisplayVariables()
    {
        if (score > highScore)
        {
            highScore = score;
        }

        lives = startingLives;
        score = 0;
        wave = 0;

        UpdateLivesText();
        UpdateScoreText();
        UpdateWaveText();
        UpdateHighScoreText();
    }

    // Call the relevant functions for resetting the game
    private void BeginGame()
    {
        ResetDisplayVariables();
        
        DestroyBullets();

        SpawnAsteroids();
    }

    // Destroy all remaining bullets from last game
    private void DestroyBullets()
    {
        foreach (Transform child in bulletHolder.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Remove all asteroids from the scene
    // Also remove their mirror counterparts from the screen edge straddling feature
    private void DestroyExistingAsteroids()
    {
        foreach (Transform child in asteroidHolder.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in asteroidMirrorHolder.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Asteroids are initialised off-screen evenly distributed
    private Vector2 GetAsteroidStartPosition(int _i, float _distance)
    {
        float directionDegrees = _i * 360.0f / numAsteroids;

        float direction = directionDegrees * Mathf.PI / 180.0f;

        return new Vector2(Mathf.Sin(direction) * _distance, Mathf.Cos(direction) * _distance);
    }

    // They aim for a spot call target, this returns the required rotation to point towards it
    private float GetAsteroidStartRotation(Vector2 _startPosition, Vector2 _target)
    {
        float rotationTemp = Mathf.Atan2(_startPosition.y - _target.y, _startPosition.x - _target.x);
        return 90 + (rotationTemp * 180.0f / Mathf.PI);
    }
    
    // Adds new asteroids to the scene for the wave
    private void SpawnAsteroids()
    {
        DestroyExistingAsteroids(); // remove old asteroids, but there shouldn't be any.

        numAsteroids = 4 + wave; // asteroid number increases with each wave

        for (int i = 0; i < numAsteroids; i++)
        {
            GameObject newAsteroidTemp = Instantiate(asteroidLarge);
            newAsteroidTemp.GetComponent<AsteroidController>().SetHasBeenInside(false);
                // this makes sure the code knows that it is coming from outside the screen area
            
            BorderShift bs = newAsteroidTemp.GetComponent<BorderShift>();
            bs.ForceInit();
                // I don't like doing this, but when the game starts the border shift hasn't derived screen space info yet

            float minDist = bs.GetDiagonalDistance(); // distance from centre of screen to the corner

            Vector2 startPosition = GetAsteroidStartPosition(i, Random.Range(minDist + 2, minDist + 5)); // distributed start position with randomised distance

            newAsteroidTemp.transform.position = new Vector3(startPosition.x, startPosition.y, 0);

            float rotationTempDegrees = GetAsteroidStartRotation(startPosition, new Vector2(Random.Range(-3, 3), Random.Range(-3, 3)));
            newAsteroidTemp.transform.Rotate(0, 0, rotationTempDegrees);
                // rotate the asteroid so it points towards the target

            newAsteroidTemp.GetComponent<AsteroidController>().InitMovement();
                // this randomised velocity and angular momentum of the asteroid

            newAsteroidTemp.transform.SetParent(asteroidHolder.transform); // add to the asteroid holder gameobject
        }
    }

    // When the ship gets hit by an asteroid, do relevant life decrement and updates
    // If reaches 0 then new game
    public void DecrementLives()
    {
        lives--;

        if (lives < 1)
        {
            BeginGame();
        }

        UpdateLivesText();
    }

    // increase score when an asteroid is destroyed
    public void DestroyedAsteroid()
    {
        score += pointsPerAsteroid;
        UpdateScoreText();
    }

    // decrease the score when the player fires
    // this is incentivise being conservative with shots
    public void FiredShot()
    {
        score -= costPerShot;
        UpdateScoreText();
    }

    // call to update the on screen text for score
    public void UpdateScoreText()
    {
        scoreText.text = "Score: " + score.ToString();
    }

    // call to update the on screen text for wave
    public void UpdateWaveText()
    {
        waveText.text = "Wave: " + (wave + 1);
    }

    // call to update the on screen text for lives
    public void UpdateLivesText()
    {
        livesText.text = "Lives: " + lives;
    }

    // call to update the on screen text for high score
    public void UpdateHighScoreText()
    {
        highScoreText.text = "High Score: " + highScore;
    }

    // keep track of how many asteroids are in the scene
    // While this way could slow down with a huge number of asteroids,
    //  it avoids and missed ones with simple reduction of a counter
    public void UpdateAsteroidCount()
    {
        numAsteroids = asteroidHolder.transform.childCount - 1;

        if (numAsteroids == 0)
        {
            // completed the wave, start a new one
            wave++;
            UpdateWaveText();
            SpawnAsteroids();
        }
    }
}
