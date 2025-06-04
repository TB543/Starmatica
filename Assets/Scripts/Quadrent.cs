using UnityEngine;

/*
 * a class to generate and manage a quadrant in the universe.
 * 
 * This script generates a quadrant with a random number of suns, planets, and moons
 * based on the settings defined in the Unity editor.
 */
public class Quadrant : MonoBehaviour
{
    // fields set in the Unity editor
    [SerializeField] int minSuns;
    [SerializeField] int maxSuns;
    [SerializeField] GameObject sunPrefab;

    // fields stored after the quadrant is generated
    public GameObject[] suns { get; private set; }

    /*
     * loads the quadrant data from the given seed.
     */
    private void Start()
    {
        // loads the quadrant data
        Random.InitState(Variables.hashQuadrent());
        int numSuns = Random.Range(minSuns, maxSuns + 1);
        suns = new GameObject[numSuns];

        // generates the suns in the quadrant
        for (int i = 0; i < numSuns; i++) {
            GameObject sun = Instantiate(sunPrefab, transform);
            suns[i] = sun;
        }
    }
}
