using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoseWinCondition : MonoBehaviour
{
    [Header("Referencias de Scripts")]
    [SerializeField] private CheckpointManager checkpointManager;

    [Header("Paneles de UI")]
    [SerializeField] private GameObject WinPanel;
    [SerializeField] private GameObject LosePanel;
    
    [Header("Textos de Estadísticas (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI winStatsText;
    [SerializeField] private TextMeshProUGUI loseStatsText;

    private void Start()
    {
        if (WinPanel != null) WinPanel.SetActive(false);
        if (LosePanel != null) LosePanel.SetActive(false);

        if (checkpointManager == null)
        {
            checkpointManager = FindFirstObjectByType<CheckpointManager>();
        }
    }

    /// <summary>
    /// Activa la pantalla de Victoria
    /// </summary>
    public void CargarWinScene()
    {

        if (WinPanel != null)
        {
            WinPanel.SetActive(true);
            ActualizarEstadisticas(winStatsText);
        }

        PausarJuego();
    }

    /// <summary>
    /// Activa la pantalla de Derrota
    /// </summary>
    public void CargarLoseScene()
    {

        if (LosePanel != null)
        {
            LosePanel.SetActive(true);
            ActualizarEstadisticas(loseStatsText);
        }

        PausarJuego();
    }

    private void ActualizarEstadisticas(TextMeshProUGUI campoTexto)
    {
        if (campoTexto != null && checkpointManager != null)
        {
            float tiempoTotal = checkpointManager.tiempoTranscurrido;
            int minutos = Mathf.FloorToInt(tiempoTotal / 60F);
            int segundos = Mathf.FloorToInt(tiempoTotal % 60F);
            string tiempoFormateado = string.Format("{0:00}:{1:00}", minutos, segundos);

            campoTexto.text = $"Tiempo empleado: {tiempoFormateado}\n" +
                              $"Checkpoints alcanzados: {checkpointManager.checkpointsAlcanzados}\n" +
                              $"Muertes totales: {checkpointManager.muertes}";
        }
    }

    private void PausarJuego()
    {
        if (checkpointManager != null)
        {
            checkpointManager.FinalizarJuego();
        }

        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Botones de UI para reiniciar o volver al menú principal
    /// </summary>

    public void ReintentarDesdeCheckpoint()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (LosePanel != null) LosePanel.SetActive(false);

        if (checkpointManager != null)
        {
            checkpointManager.RespawnPlayer();
        }
    }

    public void ReiniciarNivelCompleto()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Detecta la colisión con el jugador para activar la pantalla de victoria
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CargarWinScene();
        }
    }
}