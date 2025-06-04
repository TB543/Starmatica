using UnityEngine;

/*
 * a class to represent a sun in the universe.
 */
public class Sun : Body
{
    // fields to stored in the Unity editor
    [SerializeField] int minOrbitingBodies;
    [SerializeField] int maxOrbitingBodies;
    [SerializeField] GameObject orbitingBodyPrefab;

    // field to store the planets orbiting the sun
    public GameObject[] orbitingBodies { get; private set; }

    /*
     * called when the script instance is being loaded.
     * creates the planets orbiting the sun.
     */
    protected override void Awake()
    {
        // initalizes the sun
        base.Awake();
        int orbitingBodyCount = Random.Range(minOrbitingBodies, maxOrbitingBodies + 1);
        orbitingBodies = new GameObject[orbitingBodyCount];

        // generates the planets orbiting the sun
        for (int i = 0; i < orbitingBodyCount; i++) {
            GameObject orbitingBody = Instantiate(orbitingBodyPrefab, transform);
            orbitingBodies[i] = orbitingBody;
        }
    }
}
