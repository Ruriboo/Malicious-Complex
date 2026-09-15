using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuración de Escenas")]

    [SerializeField] private string primeraEscenaJuego = "MaliciousComplex";

    [Header("Paneles UI del Menú")]

    [SerializeField] private GameObject menuButtonsPanel;

    [SerializeField] private GameObject credits;

    [SerializeField] private GameObject HowToPlayPanel;

    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);
        if (credits != null) credits.SetActive(false);
        if (HowToPlayPanel != null) HowToPlayPanel.SetActive(false);
    }

    /// <summary>
    /// Inicia el juego cargando la escena del primer nivel.
    /// </summary>
    public void Jugar()
    {
        if (!string.IsNullOrEmpty(primeraEscenaJuego))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SceneManager.LoadScene(primeraEscenaJuego);
        }
        else
        {
            Debug.LogError("MainMenuController: No se especificó el nombre de la escena del nivel en el Inspector.");
        }
    }

    /// <summary>
    /// Carga una escena específica por su nombre.
    /// </summary>
    public void CargarEscena(string nombreEscena)
    {
        if (!string.IsNullOrEmpty(nombreEscena))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SceneManager.LoadScene(nombreEscena);
        }
        else
        {
            Debug.LogError("MainMenuController: El nombre de la escena a cargar está vacío.");
        }
    }

    /// <summary>
    /// Abre el panel de Cómo Jugar y oculta los botones del menú.
    /// </summary>
    public void AbrirComoJugar()
    {
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(false);
        if (HowToPlayPanel != null) HowToPlayPanel.SetActive(true);
    }

    /// <summary>
    /// Cierra el panel de Cómo Jugar y muestra los botones del menú.
    /// </summary>
    public void CerrarComoJugar()
    {
        if (HowToPlayPanel != null) HowToPlayPanel.SetActive(false);
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);
    }

    /// <summary>
    /// Abre el panel de Créditos y oculta los botones del menú.
    /// </summary>
    public void Abrircreditos()
    {
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(false);
        if (credits != null) credits.SetActive(true);
    }

    /// <summary>
    /// Cierra el panel de Créditos y muestra los botones del menú.
    /// </summary>
    public void CerrarCreditos()
    {
        if (credits != null) credits.SetActive(false);
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);
    }
}