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
            CheckpointManager manager = FindObjectOfType<CheckpointManager>();

            if (manager != null)
            {
                Vector3 spawnPos = spawnPointTransform != null ? spawnPointTransform.position : transform.position;
                
                float waterHeightToSave = useCurrentWaterHeight && manager.water != null 
                    ? manager.water.position.y 
                    : customWaterHeight;

                manager.SetCheckpoint(spawnPos, waterHeightToSave);
                isActivated = true;
            }
        }
    }
}