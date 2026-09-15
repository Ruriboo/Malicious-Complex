using UnityEngine;

public class SideMovingPlatform : MovingPlatform
{
    [SerializeField] private int distance;
    [SerializeField] private AXIS axis;
    private int direction = 1;

    protected override void Start()
    {
        base.Start();
        distance = Mathf.Abs(distance);
    }

    void Update()
    {
        CheckDirection();
        Move();
    }

    private void Move()
    {
        Vector3 movement = Vector3.zero;
        movement[(int)axis] = direction * speed * Time.deltaTime;
        
        transform.Translate(movement, Space.World);
    }

    private void CheckDirection()
    {
        float offset = transform.position[(int)axis] - spawnpoint[(int)axis];

        if (direction > 0 && offset >= distance)
        {
            direction = -1;
        }
        else if (direction < 0 && offset <= -distance)
        {
            direction = 1;
        }
    }
}