using UnityEngine;

/*
 * a class to handle dynamic chunk spliting with quadtree LOD (Level of Detail) for a terrain system.
 */
public class Chunk
{
    // struct for storing data to pass to the compute shader
    public struct ChunkData {
        public Vector3 origin;  // on the unit cube
        public Vector3 xAxis;  // magnitude is the size of the chunk on the unit cube
        public Vector3 yAxis;
    }

    // fields for the chunk
    public ChunkData data { get; private set; }
    private ChunkManager chunkManager;
    private Vector3 center;  // the center of the chunk projected onto the planet
    private float size;  // adjusted size of the chunk based on the planet's radius
    private GameObject chunkGameObject;
    private Chunk[] subChunks = new Chunk[4];  // will be set if the chunk is split
    private float ratio => size / Vector3.Distance(Camera.main.transform.position, chunkManager.transform.position + center);

    /*
     * Constructor for Chunk.
     * Initializes the chunk with the given parameters.
     *
     * @param origin The origin point of the chunk.
     * @param xAxis The x-axis direction of the chunk.
     * @param yAxis The y-axis direction of the chunk.
     * @param chunkManager The manager responsible for handling chunks.
     */
public Chunk(Vector3 origin, Vector3 xAxis, Vector3 yAxis, ChunkManager chunkManager)
    {
        data = new ChunkData {
            origin = origin,
            xAxis = xAxis,
            yAxis = yAxis
        };
        this.chunkManager = chunkManager;
        center = project(origin + (.5f * xAxis) + (.5f * yAxis)) * chunkManager.body.radius;
        size = xAxis.magnitude * chunkManager.body.radius;
        chunkGameObject = chunkManager.requestChunk(data);
    }

    /*
     * updates the chunks LOD (Level of Detail) based on the distance from the camera.
     * splits the chunk if it is too large and needs to be split, or merges it with its parent if it is small enough.
     */
    public void updateLOD()
    {
        // handle merging of chunks  todo plug LODThreshold into falloff formula so levels of detail are clumped tpgether a bit more        additionally modify compute shader to take 1 call per frame to update every chunk at once
        if (ratio < chunkManager.settings.LODThreshold) {
            tryMerge();
            return;
        }

        // tries to split and recursively updates the LOD of the sub-chunks
        trySplit();
        if (chunkGameObject == null) {
            foreach (Chunk chunk in subChunks) {
                chunk.updateLOD();
            }
        }
    }

    /*
     * Merges the child chunks into a single chunk if they are small enough.
     * If the chunk has already been merged, it does nothing.
     * Otherwise, it generates the chunk mesh and returns the sub-chunks to the chunk manager.
     * 
     * @param rootCall Indicates if this method is called from the root chunk.
     *      determines whether to generate the chunk mesh or not.
     */
    private void tryMerge(bool rootCall = true)
    {
        // handles when chunk has already been merged
        if (chunkGameObject != null)
            return;

        // removes the sub-chunks
        foreach (Chunk chunk in subChunks) {
            chunk.tryMerge(false);
            chunkManager.returnChunk(chunk.chunkGameObject);
        }

        // generates the merged chunk mesh
        if (rootCall) {
            chunkGameObject = chunkManager.requestChunk(data);
            subChunks = new Chunk[4];
        }
    }

    /*
     * Splits the chunk into smaller chunks if it is too large.
     * Creates four sub-chunks and initializes them with the appropriate parameters.
     * to be called by leaf chunks that need to be split.
     */
    private void trySplit()
    {
        // handles when chunk does not need to be split
        if (chunkGameObject == null || size <= chunkManager.settings.maxChunkSize)
            return;

        // creates the sub-chunks if they do not already exist
        Vector3 splitXAxis = data.xAxis / 2f;
        Vector3 splitYAxis = data.yAxis / 2f;
        Vector3[] origins = new Vector3[] { Vector3.zero, splitXAxis, splitYAxis, splitXAxis + splitYAxis };

        // creates the sub-chunks and initializes them
        for (int i = 0; i < 4; i++) {
            subChunks[i] = new Chunk(data.origin + origins[i], splitXAxis, splitYAxis, chunkManager);
        }

        // removes the current chunk's game object and sets it inactive
        chunkManager.returnChunk(chunkGameObject);
        chunkGameObject = null;
    }

    /*
     * Projects a point on the unit cube to a point on the sphere using the
     * cube-to-sphere projection algorithm. This ensures that the vertices are
     * evenly distributed across the surface of the sphere.
     *
     * @param point The point on the unit cube to be projected.
     * @return The projected point on the sphere.
     */
    private Vector3 project(Vector3 point)
    {
        float x2 = point.x * point.x;
        float y2 = point.y * point.y;
        float z2 = point.z * point.z;
        float x = point.x * Mathf.Sqrt(1f - (y2 / 2f) - (z2 / 2f) + ((y2 * z2) / 3f));
        float y = point.y * Mathf.Sqrt(1f - (z2 / 2f) - (x2 / 2f) + ((z2 * x2) / 3f));
        float z = point.z * Mathf.Sqrt(1f - (x2 / 2f) - (y2 / 2f) + ((x2 * y2) / 3f));
        return new Vector3(x, y, z).normalized;
    }
}
