using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    [SerializeField] ChunkSettings settings;
    [SerializeField] ComputeShader terrainShader;
    [SerializeField] ComputeShader LODShader;

    // fields for the chunk manager used by the chunk objects
    public Body body { get; private set; }

    // various structures used to manage chunks
    private static Queue<GameObject> gameObjectPool = new Queue<GameObject>();
    public List<Chunk.Data> allChunkData { get; private set; } = new List<Chunk.Data>();
    public List<Chunk> allChunks { get; private set; } = new List<Chunk>();
    public Queue<Chunk> mergeBuffer { get; private set; } = new Queue<Chunk>();
    private List<Chunk.Data> dirtyChunks = new List<Chunk.Data>();
    private Queue<Mesh> dirtyMeshs = new Queue<Mesh>();

    /*
     * called before the first frame update, generates 6 chunks for each face of the body.
     */
    private void Start()
    {
        body = GetComponent<Body>();
        Vector3[] faces = new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        for (int i = 0; i < 6; i++) {
            Vector3 xAxis = new Vector3(faces[i].y, faces[i].z, faces[i].x);
            Vector3 yAxis = Vector3.Cross(faces[i], xAxis);
            new Chunk(faces[i] - xAxis - yAxis, xAxis * 2, yAxis * 2, this);
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
        float t = Time.realtimeSinceStartup;
        // merges chunks that can merge
        foreach (int i in getLODUpdates(0))
            allChunks[i].merge();
        clearMergeBuffer();

        // splits chunks that can split
        foreach (int i in getLODUpdates(1))
            allChunks[i].split();

        updateMesh();

        print((Time.realtimeSinceStartup - t) * 1000);
    }

    /*
     * removes all chunks that were merged from their respective lists
     */
    private void clearMergeBuffer()
    {
        // clears the merge buffer
        while (mergeBuffer.Count > 0) {
            Chunk chunk = mergeBuffer.Dequeue();

            // updates the data struct
            Chunk.Data lastData = allChunkData[allChunkData.Count - 1];
            Chunk lastChunk = allChunks[allChunks.Count - 1];
            lastData.index = chunk.data.index;
            lastChunk.data = lastData;

            // updates the struct stored in the manager chunk lists
            allChunkData[chunk.data.index] = lastData;
            allChunks[chunk.data.index] = lastChunk;
            allChunkData.RemoveAt(allChunkData.Count - 1);
            allChunks.RemoveAt(allChunks.Count - 1);
        }
    }

    /*
     * puts all the chunks into a compute shader to filter the only the ones that need LOD updates.
     * 
     * @param mode The mode of the LOD update: 0 is for merge checks, 1 is for split checks.
     * 
     * @return an array of chunk indexes that need to be updated
     */
    private int[] getLODUpdates(int mode)
    {
        // prepares the data for the compute shader
        int[] result = new int[allChunkData.Count - 1];
        int[] count = { 0 };
        ComputeBuffer chunksDataBuffer = new ComputeBuffer(allChunkData.Count, Marshal.SizeOf(typeof(Chunk.Data)));
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer resultBuffer = new ComputeBuffer(allChunkData.Count, sizeof(int));
        chunksDataBuffer.SetData(allChunkData);
        countBuffer.SetData(count);
        resultBuffer.SetData(result);

        // loads the compute shader and sets the parameters
        LODShader.SetBuffer(0, "chunks", chunksDataBuffer);
        LODShader.SetBuffer(0, "count", countBuffer);
        LODShader.SetBuffer(0, "result", resultBuffer);
        LODShader.SetInt("mode", mode);
        LODShader.SetInt("numChunks",  allChunkData.Count);
        LODShader.SetVector("cameraPosition", Camera.main.transform.position);
        LODShader.SetVector("chunkGlobalPosition", transform.position);
        LODShader.SetFloat("radius", body.radius);

        // runs the shader and collects the results
        int groups = Mathf.CeilToInt(allChunkData.Count / 64f);
        LODShader.Dispatch(0, groups, 1, 1);
        countBuffer.GetData(count);
        result = new int[count[0]];
        resultBuffer.GetData(result, 0, 0, count[0]);
        chunksDataBuffer.Release();
        countBuffer.Release();
        resultBuffer.Release();
        return result;
    }

    /*
     * updates the meshes of the dirty chunks by using a compute shader to generate the meshes.
     * retrieves the data from the compute buffers and sets it to the meshes.
     * clears the dirty chunks and dirty meshes after updating.
     */
    private void updateMesh()
    {
        // prepares the compute shader for generating meshes
        if (dirtyChunks.Count == 0) return;
        int chunkVertexCount = settings.chunkResolution * settings.chunkResolution;
        int chunkTriangleCount = (settings.chunkResolution - 1) * (settings.chunkResolution - 1) * 6;
        Vector3[] vertices = new Vector3[chunkVertexCount * dirtyChunks.Count];
        int[] triangles = new int[chunkTriangleCount * dirtyChunks.Count];
        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        ComputeBuffer chunkDataBuffer = new ComputeBuffer(dirtyChunks.Count, Marshal.SizeOf(typeof(Chunk.Data)));

        // sets the data for the compute shader
        vertexBuffer.SetData(vertices);
        triangleBuffer.SetData(triangles);
        chunkDataBuffer.SetData(dirtyChunks);
        terrainShader.SetBuffer(0, "points", vertexBuffer);
        terrainShader.SetBuffer(0, "triangles", triangleBuffer);
        terrainShader.SetBuffer(0, "chunks", chunkDataBuffer);
        terrainShader.SetInt("detail", settings.chunkResolution);
        terrainShader.SetInt("numChunks", dirtyChunks.Count);
        terrainShader.SetFloat("radius", body.radius);
        terrainShader.SetInt("seed", body.seed);

        // runs the compute shader to generate the meshes
        int groups = Mathf.CeilToInt(settings.chunkResolution / 8f);
        terrainShader.Dispatch(0, groups, groups, dirtyChunks.Count);
        int vertexIndex = 0;
        int triangleIndex = 0;

        // copies the generated data into the meshes
        vertices = new Vector3[chunkVertexCount];
        triangles = new int[chunkTriangleCount];
        while (dirtyMeshs.Count > 0) {

            // retrieves the data from the compute buffers
            vertexBuffer.GetData(vertices, 0, vertexIndex, chunkVertexCount);
            triangleBuffer.GetData(triangles, 0, triangleIndex, chunkTriangleCount);
            vertexIndex += chunkVertexCount;
            triangleIndex += chunkTriangleCount;

            // retrieves the mesh from the queue and sets its data
            Mesh mesh = dirtyMeshs.Dequeue();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        // releases the compute buffers
        vertexBuffer.Release();
        triangleBuffer.Release();
        chunkDataBuffer.Release();
        dirtyChunks.Clear();
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
        if (gameObjectPool.Count == 0) {
            obj = Instantiate(settings.chunkPrefab, body.transform);
            obj.GetComponent<MeshFilter>().mesh.Clear();
        }

        // remove the chunk from the pool and activate it
        else {
            obj = gameObjectPool.Dequeue();
            obj.transform.SetParent(body.transform, false);
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
        gameObjectPool.Enqueue(obj);
    }
}
