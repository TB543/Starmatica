using UnityEngine;

[System.Serializable]
public class ChunkSettings : ScriptableObject
{
    public float LODThreshold;
    public float maxChunkSize;
    public int chunkResolution;
    public GameObject chunkPrefab;
}
