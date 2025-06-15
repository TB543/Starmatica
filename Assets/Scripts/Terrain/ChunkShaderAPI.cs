using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/*
 * a class to handle communication to the chunk compute shaders
 */
[System.Serializable]
public class ChunkShaderAPI
{
    // fields for the shader scripts
    [Header("Shader Scripts/Settings")]
    [SerializeField] ComputeShader LODShader;
    [SerializeField] ComputeShader terrainShader;

    // constants used for LOD calculations
    [Header("LOD Settings")]
    [SerializeField] float maxDetailThreshold = 1000f; // chunks under this distance will be rendered at max LOD
    [SerializeField] float maxChunkSize = 200f; // the maximum size of a chunk
    [SerializeField] float LODThreshold = .4f; // relative screen size for chunks for when to split/merge
    [SerializeField] float falloffPower = 3f; // higher values = LOD decreases faster
    [SerializeField] float falloffDistance = 10000f; // how wide the falloff zone is

    // fields for the terrain generation
    [Header("Terrain Settings")]
    [SerializeField] int chunkResolution = 8; // number of points on the chunk in both the x and y direction (value if 8 = 64 total points)
    [SerializeField] float terrainAmplitude = .01f; // how big the mountains/valleys should be as a percentage of the planets radius (1 is 100%)

    /*
     * stores the mode for checking LOD updates: 0 for merge, 1 for split
     */
    public enum LODMode {
        merge = 0,
        split = 1
    }

    /*
     * puts all the chunks into a compute shader to filter the only the ones that need LOD updates.
     * 
     * @param shader the shader for checking LOD updates
     * @param chunkData the list of data for chunks that need to be checked
     * @param position the position of the planet in space
     * @param radius the radius of the planet
     * @param mode the LODMode to use in the shader
     * 
     * @return an array of chunk indexes that need to be updated
     */
    public int[] getLODUpdates(List<Chunk.Data> chunkData, Vector3 position, float radius, LODMode mode)
    {
        // prepares the data for the compute shader
        int[] result = new int[chunkData.Count - 1];
        int[] count = { 0 };
        ComputeBuffer chunksDataBuffer = new ComputeBuffer(chunkData.Count, Marshal.SizeOf(typeof(Chunk.Data)));
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer resultBuffer = new ComputeBuffer(chunkData.Count, sizeof(int));
        chunksDataBuffer.SetData(chunkData);
        countBuffer.SetData(count);
        resultBuffer.SetData(result);

        // loads the compute shader and sets the parameters
        LODShader.SetFloat("maxDetailThreshold", maxDetailThreshold);
        LODShader.SetFloat("maxChunkSize", maxChunkSize);
        LODShader.SetFloat("LODThreshold", LODThreshold);
        LODShader.SetFloat("falloffPower", falloffPower);
        LODShader.SetFloat("falloffDistance", falloffDistance);
        LODShader.SetBuffer(0, "chunks", chunksDataBuffer);
        LODShader.SetBuffer(0, "atomicCount", countBuffer);
        LODShader.SetBuffer(0, "result", resultBuffer);
        LODShader.SetInt("mode", (int)mode);
        LODShader.SetInt("numChunks", chunkData.Count);
        LODShader.SetVector("cameraPosition", Camera.main.transform.position);
        LODShader.SetVector("planetPosition", position);
        LODShader.SetFloat("planetRadius", radius);

        // runs the shader and collects the results
        int groups = Mathf.CeilToInt(chunkData.Count / 64f);
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
     * gets the new mesh data for all chunks that need to be updated
     * 
     * @param shader the shader to get the mesh data
     * @param chunks a list of all the chunks that need to be updated
     * @param radius the radius of the planet
     * @param seed the seed of the planet
     * 
     * @return a tuple containing the vertex and triangle data for the mesh
     */
    public IEnumerable<(Vector3[], int[])> getMeshUpdates(List<Chunk.Data> chunks, float radius, int seed)
    {
        // prepares the compute shader for generating meshes
        int chunkVertexCount = chunkResolution * chunkResolution;
        int chunkTriangleCount = (chunkResolution - 1) * (chunkResolution - 1) * 6;
        Vector3[] vertices = new Vector3[chunkVertexCount * chunks.Count];
        int[] triangles = new int[chunkTriangleCount * chunks.Count];
        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        ComputeBuffer chunkDataBuffer = new ComputeBuffer(chunks.Count, Marshal.SizeOf(typeof(Chunk.Data)));

        // sets the data for the compute shader
        vertexBuffer.SetData(vertices);
        triangleBuffer.SetData(triangles);
        chunkDataBuffer.SetData(chunks);
        terrainShader.SetBuffer(0, "points", vertexBuffer);
        terrainShader.SetBuffer(0, "triangles", triangleBuffer);
        terrainShader.SetBuffer(0, "chunks", chunkDataBuffer);
        terrainShader.SetInt("detail", chunkResolution);
        terrainShader.SetInt("numChunks", chunks.Count);
        terrainShader.SetFloat("planetRadius", radius);
        terrainShader.SetInt("seed", seed);
        terrainShader.SetFloat("terrainAmplitude", terrainAmplitude); 

        // runs the compute shader to generate the meshes
        int groups = Mathf.CeilToInt(chunkResolution / 8f);
        terrainShader.Dispatch(0, groups, groups, chunks.Count);
        int vertexIndex = 0;
        int triangleIndex = 0;

        // gets iterates over every generated mesh
        vertices = new Vector3[chunkVertexCount];
        triangles = new int[chunkTriangleCount];
        while (vertexIndex < chunkVertexCount * chunks.Count) 
        {
            // retrieves the data from the compute buffers
            vertexBuffer.GetData(vertices, 0, vertexIndex, chunkVertexCount);
            triangleBuffer.GetData(triangles, 0, triangleIndex, chunkTriangleCount);
            vertexIndex += chunkVertexCount;
            triangleIndex += chunkTriangleCount;
            yield return (vertices, triangles);
        }

        // releases the compute buffers
        vertexBuffer.Release();
        triangleBuffer.Release();
        chunkDataBuffer.Release();
        chunks.Clear();
    }
}
