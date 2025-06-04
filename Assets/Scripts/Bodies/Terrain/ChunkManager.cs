using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    public ChunkSettings _settings;
    [SerializeField] ComputeShader terrainShader;

    // fields for the chunk manager used by the chunk objects
    public ChunkSettings settings => _settings;
    public Body body { get; private set; }

    // various structures used to manage chunks
    private static Queue<GameObject> pool = new Queue<GameObject>();
    private Chunk[] rootChunks = new Chunk[6];

    // structures to to hold the data to pass to the compute shader
    private List<Chunk.ChunkData> dirtyChunks = new List<Chunk.ChunkData>();
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
            rootChunks[i] = new Chunk(faces[i] - xAxis - yAxis, xAxis * 2, yAxis * 2, this);
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
        // updates the LOD of each chunk
        foreach (Chunk chunk in rootChunks) {
            chunk.updateLOD();
        }

        // prepares the compute shader for generating meshes
        if (dirtyChunks.Count == 0) return;
        int chunkVertexCount = settings.chunkResolution * settings.chunkResolution;
        int chunkTriangleCount = (settings.chunkResolution - 1) * (settings.chunkResolution - 1) * 6;
        Vector3[] vertices = new Vector3[chunkVertexCount * dirtyChunks.Count];
        int[] triangles = new int[chunkTriangleCount * dirtyChunks.Count];
        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        ComputeBuffer chunkDataBuffer = new ComputeBuffer(dirtyChunks.Count, Marshal.SizeOf(typeof(Chunk.ChunkData)));

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

    private void updateMesh()
    {

    }

    /*
     * Gets a game object from the pool or creates a new one if the pool is empty.
     * 
     * @param data The chunk data requesting a chunk.
     *      this chunk will be queued for terrain generation.
     * 
     * @return The game object representing the chunk.
     */
    public GameObject requestChunk(Chunk.ChunkData data)
    {
        // if the pool is empty, create a new chunk
        GameObject obj;
        if (pool.Count == 0) {
            obj = Instantiate(settings.chunkPrefab, body.transform);
            obj.GetComponent<MeshFilter>().mesh.Clear();
        }

        // remove the chunk from the pool and activate it
        else {
            obj = pool.Dequeue();
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
    public void returnChunk(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        obj.GetComponent<MeshFilter>().mesh.Clear();
        pool.Enqueue(obj);
    }
}
