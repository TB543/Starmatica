using UnityEngine;

[CreateAssetMenu(fileName = "ChunkSettings", menuName = "Scriptable Objects/ChunkSettings")]
public class ChunkSettings : ScriptableObject
{
    public float LODThreshold;
    public float maxChunkSize;
    public int chunkResolution;
    public GameObject chunkPrefab;
}
