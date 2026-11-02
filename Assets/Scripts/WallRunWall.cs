using UnityEngine;

public class WallRunWall : MonoBehaviour
{
    [SerializeField] private float wallRunSpeed = 7f;
    [SerializeField] private float wallRunDuration = 2f;

    public float WallRunSpeed => wallRunSpeed;
    public float WallRunDuration => wallRunDuration;
}