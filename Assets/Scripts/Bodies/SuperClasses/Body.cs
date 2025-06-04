using UnityEngine;

/*
 * a class to represent a celestial body in the universe.
 */
public class Body : MonoBehaviour
{
    // fields to store size of body set in the Unity editor
    [SerializeField] protected int minRadius;
    [SerializeField] protected int maxRadius;

    // fields to store the bodies data
    public int seed { get; private set; }
    public int radius { get; private set; }

    /*
     * initializes the body when the script instance is being loaded.
     */
    protected virtual void Awake()
    {
        seed = Random.Range(int.MinValue, int.MaxValue);
        radius = Random.Range(minRadius, maxRadius);
    }
}
