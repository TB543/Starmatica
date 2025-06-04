using UnityEngine;

/*
 * a class to represent a planet in the universe.
 */
public class Planet : Sun
{
    protected override void Awake()
    {
        base.Awake();
        transform.position = new Vector3(0, 0, radius + 50);
    }
}
