using UnityEngine;

/*
 * a class to represent a sun in the universe.
 */
public class Sun : MonoBehaviour
{
    // fields to stored in the Unity editor
    [SerializeField] int minPlanets;
    [SerializeField] int maxPlanets;
    [SerializeField] GameObject planetPrefab;

    // field to store the planets orbiting the sun
    public GameObject[] planets { get; private set; }

    /*
     * called when the script instance is being loaded.
     * creates the planets orbiting the sun.
     */
    private void Awake()
    {
        // initalizes the sun
        int numPlanets = Random.Range(minPlanets, maxPlanets + 1);
        planets = new GameObject[numPlanets];

        // generates the planets orbiting the sun
        for (int i = 0; i < numPlanets; i++) {
            GameObject planet = Instantiate(planetPrefab, transform);
            planets[i] = planet;
        }
    }
}
