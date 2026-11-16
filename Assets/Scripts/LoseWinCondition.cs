using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Audio;

public class LoseWinCondition : MonoBehaviour
{
    [Header("Referencias de Scripts")]
    [SerializeField] private CheckpointManager checkpointManager;
    [SerializeField] private MonoBehaviour cameraController; 

    [Header("Paneles de UI")]
    [SerializeField] private GameObject WinPanel;
    [SerializeField] private GameObject LosePanel;
    
    [Header("Textos de Estadísticas (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI winStatsText;
    [SerializeField] private TextMeshProUGUI loseStatsText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip winAudioClip;
    [SerializeField] private AudioClip loseAudioClip;

    [Header("Música adaptativa (snapshots del mixer)")]
    [SerializeField] private AudioMixerSnapshot snapshotNormal;
    [SerializeField] private AudioMixerSnapshot snapshotVictoria;
    [SerializeField] private AudioMixerSnapshot snapshotDerrota;

    private void Start()
    {
        if (WinPanel != null) WinPanel.SetActive(false);
        if (LosePanel != null) LosePanel.SetActive(false);
        audioSource = GetComponent<AudioSource>();

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
            audioSource.clip = winAudioClip;
            audioSource.Play();
        }

        // Estado de música "Victoria": la música sube en 2 segundos
        if (snapshotVictoria != null) snapshotVictoria.TransitionTo(2f);

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
            audioSource.clip = loseAudioClip;
            audioSource.Play();
        }

        // Estado de música "Derrota": la música baja en 1 segundo
        if (snapshotDerrota != null) snapshotDerrota.TransitionTo(1f);

        PausarJuego();
    }

    private void ActualizarEstadisticas(TextMeshProUGUI campoTexto)
    {
        if (campoTexto != null && checkpointManager != null)
        {
            float tiempoTotal = checkpointManager.TiempoTranscurrido;
            int minutos = Mathf.FloorToInt(tiempoTotal / 60F);
            int segundos = Mathf.FloorToInt(tiempoTotal % 60F);
            string tiempoFormateado = string.Format("{0:00}:{1:00}", minutos, segundos);

            campoTexto.text = $"Tiempo empleado: {tiempoFormateado}\n" +
                            $"Checkpoints alcanzados: {checkpointManager.CheckpointsAlcanzados}\n" +
                            $"Muertes totales: {checkpointManager.Muertes}"; 
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
        
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }
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
        VolverAMusicaNormal();

        if (checkpointManager != null)
        {
            checkpointManager.RespawnPlayer();
        }
    }

    public void ReiniciarNivelCompleto()
    {
        Time.timeScale = 1f;
        VolverAMusicaNormal();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        VolverAMusicaNormal();
        SceneManager.LoadScene("MainMenu");
    }

    // El mixer conserva su estado al cambiar de escena, por eso hay que volver a Normal
    private void VolverAMusicaNormal()
    {
        if (snapshotNormal != null) snapshotNormal.TransitionTo(0.5f);
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