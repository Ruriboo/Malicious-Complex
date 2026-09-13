using UnityEngine;
using UnityEngine.SceneManagement;

public class LoseWinCondition : MonoBehaviour
{
    [Header("Nombres de las Escenas")]
    [SerializeField] private string winScene = "WinScene";
    [SerializeField] private string loseScene = "LoseScene";

    //Método público para cargar la escena de derrota
    public void CargarLoseScene()
    {
        SceneManager.LoadScene(loseScene);
    }

    //Método público para cargar la escena de victoria
    public void CargarWinScene()
    {
        SceneManager.LoadScene(winScene);
    }

    // Detecta cuando el jugador entra en el objeto de meta
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CargarWinScene();
        }
    }
}