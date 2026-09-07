using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class SideMovingPlatform : MovingPlatform
{
    [SerializeField] private int distance;
    [SerializeField] int axis;
    private Vector3 movement = Vector3.zero;
    void Start()
    {
        movement[axis] = 1;
        movement*= speed * Time.deltaTime;
    }

    void Update()
    {
       CheckDirection();
       transform.Translate(movement);
    }

    private void CheckDirection()
    {
        if (Mathf.Abs(transform.position[axis] - spawnpoint[axis]) > 5)
        {
            movement[axis] *= -1;
        }
    }
}
