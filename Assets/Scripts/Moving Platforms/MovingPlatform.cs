using UnityEngine;

public abstract class MovingPlatform : MonoBehaviour
{
    //Las plataformas se mueven relativas a su spawnpoint
    protected Vector3 spawnpoint;
    //Velocidad a la que se mueven las plataformas
    [SerializeField] protected int speed;
    protected virtual void Start()
    {
        spawnpoint = transform.position;
    }
}
