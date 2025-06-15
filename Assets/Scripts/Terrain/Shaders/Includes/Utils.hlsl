// struct for chunk data passed by the api, see Chunk.Data struct for more info as to what each value means
struct ChunkData
{
    float3 origin;
    float3 xAxis;
    float3 yAxis;
    int index;
    int isLeaf;
    int canMergeChildren;
};

/*
* Projects a point on the unit cube to a point on a sphere using the
* cube-to-sphere projection algorithm. This ensures that the vertices are
* evenly distributed across the surface of the sphere.
*
* @param p The point on the unit cube to be projected.
* @param radius the radius of the sphere
* @return The projected point on the sphere.
*/
float3 project(float3 p, float radius)
{
    float x2 = p.x * p.x;
    float y2 = p.y * p.y;
    float z2 = p.z * p.z;
    
    float x = p.x * sqrt(1 - (y2 / 2) - (z2 / 2) + ((y2 * z2) / 3));
    float y = p.y * sqrt(1 - (z2 / 2) - (x2 / 2) + ((z2 * x2) / 3));
    float z = p.z * sqrt(1 - (x2 / 2) - (y2 / 2) + ((x2 * y2) / 3));
            
    return normalize(float3(x, y, z)) * radius;
}