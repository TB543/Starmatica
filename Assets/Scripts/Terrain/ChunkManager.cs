using System.Collections.Generic;
using UnityEngine;

/*
 * a class to manage chunks in the game.
 * splits chunks into smaller chunks if they are too large.
 * merges smaller chunks into larger ones if they are small enough.
 * holds game objects for chunks and their meshes so they can be reused.
 * generates terrain for all dirty chunks
 */
public class ChunkManager : MonoBehaviour
{
    // fields set in the Unity editor
    [SerializeField] GameObject chunkPrefab;
    [SerializeField] ComputeShader terrainShader;
    [SerializeField] ComputeShader LODShader;
    [SerializeField] Body body;

    // fields for the chunk manager
    private static Queue<GameObject> gameObjPool = new Queue<GameObject>();
    private List<Chunk.Data> chunksData = new List<Chunk.Data>();
    private List<Chunk> chunks = new List<Chunk>();
    private Queue<Chunk> mergeBuffer = new Queue<Chunk>();
    private List<Chunk.Data> dirtyChunks = new List<Chunk.Data>();
    private Queue<Mesh> dirtyMeshs = new Queue<Mesh>();
    private const string SHADER_RADIUS_PROPERTY = "_Radius";

    // gets the number of chunks the class manages
    public int chunkCount => chunksData.Count;

    /*
     * called before the first frame update, generates 6 chunks for each face of the body.
     */
    private void Start()
    {
        // sets fields
        Material material = chunkPrefab.GetComponent<MeshRenderer>().sharedMaterial;
        material.SetFloat(SHADER_RADIUS_PROPERTY, body.radius);

        // generates faces
        Vector3[] faces = new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        for (int i = 0; i < 6; i++) {
            Vector3 xAxis = new Vector3(faces[i].y, faces[i].z, faces[i].x);
            Vector3 yAxis = Vector3.Cross(faces[i], xAxis);
            addChunk(faces[i] - xAxis - yAxis, xAxis * 2, yAxis * 2);
        }
    }

    /*
     * called once per frame, updates the LOD of each chunk based on the distance from the camera.
     * splits chunks that are too large and merges smaller chunks with their parent if they are small enough.
     * 
     * Additionally, it processes the dirty chunks and generates their meshes using the compute shader.
     */
    private void Update()
    {
        // merges chunks that can merge
        foreach (int i in ChunkShaderAPI.getLODUpdates(LODShader, chunksData, transform.position, body.radius, ChunkShaderAPI.LODMode.merge))
            chunks[i].merge();

        // clears the merge buffer
        while (mergeBuffer.Count > 0) {
            Chunk chunk = mergeBuffer.Dequeue();

            // updates the data struct
            Chunk.Data lastData = chunksData[chunksData.Count - 1];
            Chunk lastChunk = chunks[chunks.Count - 1];
            lastData.index = chunk.data.index;
            lastChunk.data = lastData;

            // updates the struct stored in the manager chunk lists
            chunksData[chunk.data.index] = lastData;
            chunks[chunk.data.index] = lastChunk;
            chunksData.RemoveAt(chunksData.Count - 1);
            chunks.RemoveAt(chunks.Count - 1);
        }

        // splits chunks that can split
        foreach (int i in ChunkShaderAPI.getLODUpdates(LODShader, chunksData, transform.position, body.radius, ChunkShaderAPI.LODMode.split))
            chunks[i].split();

        // updates all of the meshes that need to be updated
        if (dirtyChunks.Count == 0) return;
        foreach (var (vertices, triangles) in ChunkShaderAPI.getMeshUpdates(terrainShader, dirtyChunks, body.radius, body.seed)) {
            Mesh mesh = dirtyMeshs.Dequeue();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }

    /*
     * adds a chunk to the chunk manager
     * 
     * @param origin the orign of the chuunk
     * @param xAxis the x axis of the chunk
     * @param yAxis the y axis of the chunk
     * 
     * @return the chunk that was added
     */
    public Chunk addChunk(Vector3 origin, Vector3 xAxis, Vector3 yAxis)
    {
        Chunk chunk = new Chunk(origin, xAxis, yAxis, this);
        chunksData.Add(chunk.data);
        chunks.Add(chunk);
        return chunk;
    }

    /*
     * updates a chunks data
     * 
     * @param chunk the chunk whose data to update
     * @param data the new data for the chunk 
     */
    public void modifyData(Chunk chunk, Chunk.Data data)
    {
        chunk.data = data;
        chunksData[chunk.data.index] = data;
    }

    /*
     * queues a chunk to be merged to its parent
     * 
     * @param chunk the chunk to be merged
     */
    public void queueMerge(Chunk chunk)
    {
        mergeBuffer.Enqueue(chunk);
    }

    /*
     * Gets a game object from the pool or creates a new one if the pool is empty.
     * 
     * @param data The chunk data requesting a chunk.
     *      this chunk will be queued for terrain generation.
     * 
     * @return The game object representing the chunk.
     */
    public GameObject requestObj(Chunk.Data data)
    {
        // if the pool is empty, create a new chunk
        GameObject obj;
        if (gameObjPool.Count == 0) {
            obj = Instantiate(chunkPrefab, transform);
            obj.GetComponent<MeshFilter>().mesh.Clear();
        }

        // remove the chunk from the pool and activate it
        else {
            obj = gameObjPool.Dequeue();
            obj.transform.SetParent(transform, false);
            obj.SetActive(true);
        }

        // queue the chunk for terrain generation
        dirtyChunks.Add(data);
        dirtyMeshs.Enqueue(obj.GetComponent<MeshFilter>().mesh);
        return obj;
    }

    /*
     * Frees a game object by deactivating it and returning it to the pool.
     * This allows for reusing game objects instead of creating new ones.
     *
     * @param obj The game object to be freed.
     */
    public void returnObj(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        obj.GetComponent<MeshFilter>().mesh.Clear();
        gameObjPool.Enqueue(obj);
    }
}
