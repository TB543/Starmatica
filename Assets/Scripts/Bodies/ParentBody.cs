using UnityEngine;

/*
 * a class to represent a body with other bodies orbiting it in the universe.
 */
public class ParentBody : Body
{
    // fields to stored in the Unity editor
    [SerializeField] int minOrbitingBodies;
    [SerializeField] int maxOrbitingBodies;
    [SerializeField] GameObject orbitingBodyPrefab;

    // field to store the orbiting
    public GameObject[] orbitingBodies { get; private set; }

    /*
     * called when the script instance is being loaded.
     * creates the bodies orbiting this body.   
     */
    protected override void Awake()
    {
        // initalizes the parent class
        base.Awake();
        int orbitingBodyCount = Random.Range(minOrbitingBodies, maxOrbitingBodies + 1);
        orbitingBodies = new GameObject[orbitingBodyCount];

        // generates the orbiting planets
        for (int i = 0; i < orbitingBodyCount; i++) {
            GameObject orbitingBody = Instantiate(orbitingBodyPrefab, transform);
            orbitingBodies[i] = orbitingBody;
        }
    }
}
