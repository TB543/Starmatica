using System.Collections.Generic;
using UnityEngine;

/*
 * a class to manage chunks in the game.
 * splits chunks into smaller chunks if they are too large.
 * merges smaller chunks into larger ones if they are small enough.
 * holds game objects for chunks and their meshes so they can be reused.
 */
public class ChunkManager : MonoBehaviour
{
    // fields set in the Unity editor
    [SerializeField] private ChunkSettings _settings;
    [SerializeField] ComputeShader _terrainShader;

    // fields for the chunk manager used by the chunk objects
    public ChunkSettings settings => _settings;
    public ComputeShader terrainShader => _terrainShader;
    public Body body { get; private set; }

    // pool of game objects to represent chunks
    private static Queue<GameObject> pool = new Queue<GameObject>();
    private HashSet<Chunk> rootChunks = new HashSet<Chunk>();

    /*
     * called before the first frame update, generates 6 chunks for each face of the body.
     */
    private void Start()
    {
        body = GetComponent<Body>();
        foreach (Vector3 face in new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back }) {
            Vector3 xAxis = new Vector3(face.y, face.z, face.x);
            Vector3 yAxis = Vector3.Cross(face, xAxis);
            rootChunks.Add(new Chunk(face - xAxis - yAxis, xAxis * 2, yAxis * 2, this));
        }
    }

    /*
     * called once per frame, updates the LOD of each chunk based on the distance from the camera.
     * splits chunks that are too large and merges smaller chunks with their parent if they are small enough.
     */
    private void Update()
    {
        float startTime = Time.realtimeSinceStartup;
        foreach (Chunk chunk in rootChunks) {
            chunk.updateLOD();
        }
        float endTime = Time.realtimeSinceStartup;
        UnityEngine.Debug.Log($"Terrain generation took {(endTime - startTime) * 1000f} ms");
    }

    /*
     * Gets a game object from the pool or creates a new one if the pool is empty.
     * 
     * @param chunk The chunk object requesting a chunk.
     * @return The game object representing the chunk.
     */
    public GameObject requestChunk()
    {
        // if the pool is empty, create a new chunk
        if (pool.Count == 0)
            return Instantiate(settings.chunkPrefab, body.transform);

        // remove the chunk from the pool and activate it
        GameObject obj = pool.Dequeue();
        obj.transform.SetParent(body.transform, false);
        obj.SetActive(true);
        return obj;
    }

    /*
     * Frees a game object by deactivating it and returning it to the pool.
     * This allows for reusing game objects instead of creating new ones.
     *
     * @param obj The game object to be freed.
     */
    public void returnChunk(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
