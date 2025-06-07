using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * a class to hold global variables and useful functions that can be accessed from other scripts
 */
public class GameManager : MonoBehaviour
{
    // fields accessible from other scripts
    public static GameManager Instance { get; private set; }
    public static int? universeSeed;

    // stores quadrant scene name
    private const string QUADRANT_SCENE_NAME = "Quadrant";

    /*
     * called when the script is loaded, ensures singleton gameManager
     */
    private void Awake()
    {
        // sets random seed if none exists
        if (universeSeed == null)
            universeSeed = Random.Range(int.MinValue, int.MaxValue);

        // ensures singleton gameManager
        if (Instance != null)
            Destroy(gameObject);
        else {
            Instance = this;
            DontDestroyOnLoad(Instance);
            loadQuadrent(Vector2Int.zero);  // todo this is for starting game without menu scene, delete later
        }
    }

    /*
     * generates a random number based on the given values for the
     * universe seed and loaded quadrent coordinates
     * using the FNV-1a hash algorithm and loads the quadrent with the seed
     */
    public static void loadQuadrent(Vector2Int quadrantCoords)
    {
        // ensures integer overflow is not checked
        unchecked {
            const int fnvPrime = 16777619;
            int result = (int)2166136261;

            // apply the universe seed and quadrant coordinates to the hash and load the quadrant
            result = (result ^ universeSeed.Value) * fnvPrime;
            result = (result ^ quadrantCoords.x) * fnvPrime;
            result = (result ^ quadrantCoords.y) * fnvPrime;
            Random.InitState(result);
            SceneManager.LoadScene(QUADRANT_SCENE_NAME);
        }
    }
}
