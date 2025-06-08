using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/*
 * a class to handle communication to the chunk compute shaders
 */
public static class ChunkShaderAPI
{
    /*
     * stores the mode for checking LOD updates: 0 for merge, 1 for split
     */
    public enum LODMode {
        merge = 0,
        split = 1
    }

    // settings for chunks resolution
    private static int chunkResolution = 8;

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
    public static int[] getLODUpdates(ComputeShader shader, List<Chunk.Data> chunkData, Vector3 position, float radius, LODMode mode)
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
        shader.SetBuffer(0, "chunks", chunksDataBuffer);
        shader.SetBuffer(0, "count", countBuffer);
        shader.SetBuffer(0, "result", resultBuffer);
        shader.SetInt("mode", (int)mode);
        shader.SetInt("numChunks", chunkData.Count);
        shader.SetVector("cameraPosition", Camera.main.transform.position);
        shader.SetVector("chunkGlobalPosition", position);
        shader.SetFloat("radius", radius);

        // runs the shader and collects the results
        int groups = Mathf.CeilToInt(chunkData.Count / 64f);
        shader.Dispatch(0, groups, 1, 1);
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
    public static IEnumerable<(Vector3[], int[])> getMeshUpdates(ComputeShader shader, List<Chunk.Data> chunks, float radius, int seed)
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
        shader.SetBuffer(0, "points", vertexBuffer);
        shader.SetBuffer(0, "triangles", triangleBuffer);
        shader.SetBuffer(0, "chunks", chunkDataBuffer);
        shader.SetInt("detail", chunkResolution);
        shader.SetInt("numChunks", chunks.Count);
        shader.SetFloat("radius", radius);
        shader.SetInt("seed", seed);

        // runs the compute shader to generate the meshes
        int groups = Mathf.CeilToInt(chunkResolution / 8f);
        shader.Dispatch(0, groups, groups, chunks.Count);
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
