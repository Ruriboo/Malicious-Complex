using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public Transform spawnPointTransform;
    public float customWaterHeight = 0f;
    public bool useCurrentWaterHeight = true;
    [SerializeField] private AudioSource audioSource;

    private bool isActivated = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;

        if (other.CompareTag("Player"))
        {
            if (CheckpointManager.Instance != null)
            {
                Vector3 spawnPos = spawnPointTransform != null ? spawnPointTransform.position : transform.position;
                
                float waterHeightToSave = useCurrentWaterHeight && CheckpointManager.Instance.Water != null   
                    ? CheckpointManager.Instance.Water.position.y                                             
                    : customWaterHeight;

                CheckpointManager.Instance.SetCheckpoint(spawnPos, waterHeightToSave);
                if (audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }
                isActivated = true;
            }
        }
    }
}