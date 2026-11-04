using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuración de Escenas")]

    [SerializeField] private string primeraEscenaJuego = "MaliciousComplex";

    [Header("Paneles UI del Menú")]

    [SerializeField] private GameObject menuButtonsPanel;

    [SerializeField] private GameObject credits;

    [SerializeField] private GameObject HowToPlayPanel;

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxUI;

    private bool paused = false;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);
        if (credits != null) credits.SetActive(false);
        if (HowToPlayPanel != null) HowToPlayPanel.SetActive(false);

        var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            btn.onClick.AddListener(SFXUi);
        }
    }

    private void Update()
    {
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            SFXUi();
            if (paused)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }

        }
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

    public void Pausar()
    {
        paused = true;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pausePanel != null) pausePanel.SetActive(true);
    }
    public void Reanudar()
    {
        paused = false;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (pausePanel != null) pausePanel.SetActive(false);
    }
    public void SFXUi()
    {
        if (audioSource == null || sfxUI == null) return;
        audioSource.PlayOneShot(sfxUI);
    }
}