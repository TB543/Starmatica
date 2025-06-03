using UnityEngine;

public class Planet : MonoBehaviour
{
    // fields to store size of planet set in the Unity editor
    [SerializeField] int minRadius;
    [SerializeField] int maxRadius;
    [SerializeField] int minMoons;
    [SerializeField] int maxMoons;
    [SerializeField] GameObject moonPrefab;
    [SerializeField] GameObject chunkPrefab;

    // field to store the planet's moons
    public GameObject[] moons { get; private set; }

    // fields to store the planet's radius and mesh data
    public int seed { get; private set; }
    public int radius { get; private set; }

    /*
     * initializes the planet when the script instance is being loaded.
     */
    private void Awake()
    {
        // initializes the planet 
        seed = Random.Range(int.MinValue, int.MaxValue);
        radius = Random.Range(minRadius, maxRadius);
        transform.position = new Vector3(radius + 50, 0, 0);
        int numMoons = Random.Range(minMoons, maxMoons + 1);
        moons = new GameObject[numMoons];

        // generates the planets orbiting the sun
        for (int i = 0; i < numMoons; i++) {
            GameObject planet = Instantiate(moonPrefab, transform);
            moons[i] = planet;
        }
    }

    /*
     * called before the first frame update, generates the planet mesh.
     */
    private void Start()
    {
        foreach (Vector3 face in new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back }) {
            Vector3 xAxis = new Vector3(face.y, face.z, face.x);
            Vector3 yAxis = Vector3.Cross(face, xAxis);
            GameObject chunkObj = Instantiate(chunkPrefab, transform);
            chunkObj.GetComponent<Chunk>().init(face - xAxis - yAxis, xAxis * 2, yAxis * 2, this);
        }
    }
}
