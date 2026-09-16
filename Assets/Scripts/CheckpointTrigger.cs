using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public Transform spawnPointTransform;
    public float customWaterHeight = 0f;
    public bool useCurrentWaterHeight = true;

    private bool isActivated = false;

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
                isActivated = true;
            }
        }
    }
}