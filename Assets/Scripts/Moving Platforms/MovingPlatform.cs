using UnityEngine;

public abstract class MovingPlatform : MonoBehaviour
{
    protected Vector3 spawnpoint;
    [SerializeField] protected int speed;
    void Start()
    {
        spawnpoint = transform.position;
    }
}
