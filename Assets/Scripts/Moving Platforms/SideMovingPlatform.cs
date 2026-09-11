using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class SideMovingPlatform : MovingPlatform
{
    //Distancia en la que se mueven las plataformas
    [SerializeField] private int distance;
    //Eje en el cual se mueven
    [SerializeField] private AXIS axis;
    //Direccion en la que se mueve
    private int direction = 1;
    protected override void Start()
    {
        //Llama al start de la clase abstracta
        base.Start();
        //Establece la distancia a positivo, por las dudas
        distance = Mathf.Abs(distance);
    }

    void Update()
    {
       //Verifica si tiene que cambiar la direccion
       CheckDirection();
        //Mueve
        Move();
    }

    private void Move()
    {
        //Establece el vector3 para el translate
        Vector3 movement = Vector3.zero;
        //Multiplica el vector 3 por la direccion, velocidad y deltatime
        movement[(int)axis] = direction * speed * Time.deltaTime;
        transform.Translate(movement);
    }

    private void CheckDirection()
    {
        //Distancia que se movio la plataforma
        float offset = transform.position[(int)axis] - spawnpoint[(int)axis];

        //Si la plataforma se pasa de la distancia, cambiar la direccion
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
