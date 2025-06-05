using UnityEngine;

/*
 * a class to handle dynamic chunk spliting with quadtree LOD (Level of Detail) for a terrain system.
 */
public class Chunk
{
    // struct for storing data to pass to the compute shaders
    public struct Data {
        public Vector3 origin;  // on the unit cube
        public Vector3 xAxis;  // magnitude is the size of the chunk on the unit cube
        public Vector3 yAxis;
        public int index;  // index of the chunk in the chunk manager
        public int isLeaf; // 0 for false 1 for true
        public int canMergeChildren; // 0 for false 1 for true
    }

    // fields for the chunk
    public Data data;
    private ChunkManager chunkManager;
    private GameObject chunkGameObject;
    private Chunk[] subChunks = new Chunk[4];
    private Chunk parent; // will be set by parent chunk

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
        data = new Data {
            origin = origin,
            xAxis = xAxis,
            yAxis = yAxis,
            index = chunkManager.allChunkData.Count,
            isLeaf = 1,
            canMergeChildren = 1
        };
        this.chunkManager = chunkManager;
        chunkGameObject = chunkManager.requestObj(data);
        chunkManager.allChunkData.Add(data);
        chunkManager.allChunks.Add(this);
    }

    /*
     * determines if the chunk can merge or not. merge conditions are as follows:
     * all child chunks are leaf nodes
     * 
     * @return 1 if it can 0 if it cannot
     */
    private int canMergeChildren()
    {
        foreach (Chunk chunk in subChunks)
            if (chunk.data.isLeaf == 0) return 0;
        return 1;
    }

    /*
     * Merges the child chunks into this chunk
     */
    public void merge()
    {
        // removes the sub-chunks
        foreach (Chunk chunk in subChunks) {
            chunkManager.returnObj(chunk.chunkGameObject);
            chunkManager.mergeBuffer.Enqueue(chunk);
        }

        // generates the merged chunk mesh
        chunkGameObject = chunkManager.requestObj(data);
        subChunks = new Chunk[4];

        // updates the isLeaf data in the struct
        Data newData = data;
        newData.isLeaf = 1;
        data = newData;
        chunkManager.allChunkData[data.index] = newData;

        // checks if parent can merge
        if (parent != null) {
            newData = parent.data;
            newData.canMergeChildren = parent.canMergeChildren();
            parent.data = newData;
            chunkManager.allChunkData[parent.data.index] = newData;
        }
    }

    /*
     * Splits the chunk into smaller chunks
     * Creates four sub-chunks and initializes them with the appropriate parameters.
     * to be called by leaf chunks that need to be split.
     */
    public void split()
    {
        // splits the chunk
        Vector3 splitXAxis = data.xAxis / 2f;
        Vector3 splitYAxis = data.yAxis / 2f;
        Vector3[] origins = new Vector3[] { Vector3.zero, splitXAxis, splitYAxis, splitXAxis + splitYAxis };

        // creates the sub-chunks and initializes them
        for (int i = 0; i < 4; i++) {
            subChunks[i] = new Chunk(data.origin + origins[i], splitXAxis, splitYAxis, chunkManager);
            subChunks[i].parent = this;
        }

        // removes the current chunk's game object and sets it inactive
        chunkManager.returnObj(chunkGameObject);
        chunkGameObject = null;

        // updates the isLeaf data in the struct
        Data newData = data;
        newData.isLeaf = 0;
        data = newData;
        chunkManager.allChunkData[data.index] = newData;

        // ensures parent cannot merge without this chunk merging first5
        if (parent != null) {
            newData = parent.data;
            newData.canMergeChildren = 0;
            parent.data = newData;
            chunkManager.allChunkData[parent.data.index] = newData;
        }
    }
}
