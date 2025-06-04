using UnityEngine;

/*
 * a class to hold global variables and useful functions that can be accessed from other scripts
 */
public class Variables : MonoBehaviour
{
    // fields accessible from other scripts
    public static Variables instance { get; private set; }
    public static int universeSeed;
    public static Vector2Int loadedQuadrantCoords;

    /*
     * Awake is called when the script instance is being loaded.
     * It initializes the global variables.
     */
    private void Awake()
    {
        // ensures that only one instance of Globals exists in the scene
        if (instance != null) {
            Destroy(gameObject);
            return;
        }

        // sets the instance to this object
        instance = this;
        universeSeed = Random.Range(int.MinValue, int.MaxValue);
        DontDestroyOnLoad(gameObject);
    }

    /*
     * generates a random number based on the given values for the
     * universe seed and loaded quadrent coordinates.
     * using the FNV-1a hash algorithm.
     */
    public static int hashQuadrent()
    {
        // ensures integer overflow is not checked
        unchecked {
            const int fnvPrime = 16777619;
            int result = (int)2166136261;

            // apply the universe seed and quadrant coordinates to the hash
            result = (result ^ universeSeed) * fnvPrime;
            result = (result ^ loadedQuadrantCoords.x) * fnvPrime;
            return (result ^ loadedQuadrantCoords.y) * fnvPrime;
        }
    }
}
