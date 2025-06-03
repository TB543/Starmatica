using UnityEngine;

/*
 * a class to hold global variables and useful functions that can be accessed from other scripts
 */
public class Globals : MonoBehaviour
{
    // fields accessible from other scripts
    public static Globals instance { get; private set; }
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

    // terrain generation settings
    public const int octaves = 5;  // how many layers of noise to use - more layers = more detail
    public const float frequency = 3f;  // the zoom level of the noise - lower values = more zoomed out hills
    public const float lacunarity = 2.0f;  // how much to increase the frequency of each layer - higher values = closer hills
    public const float gain = 0.45f;  // how much to decrease the amplitude of each layer - higher values = sharper hills
    public const float amplitude = 500f;  // how much to scale the height of the terrain - higher values = taller hills
    public const float weight = 0.5f;  // how much to decrease the amplitude of each layer - lower values = smoother transition ie smoother plains, exaggerated mountains
}
