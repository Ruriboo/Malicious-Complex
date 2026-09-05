using UnityEngine;
using UnityEngine.InputSystem;

public class FloodWater : MonoBehaviour
{
    [SerializeField] private float velocidad = 1f;

    public bool flood = false;

    void Update()
    {
        //Debug.Log("Moviéndose: " + moviendose);
        if (flood)
        {
            transform.Translate(Vector3.up * velocidad * Time.deltaTime);
        }
    }
}
