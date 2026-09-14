using UnityEngine;
using StarterAssets;

public class Zipline : MonoBehaviour
{
    [SerializeField] private Zipline targetZip;
    [SerializeField] private float zipSpeed = 5f;
    [SerializeField] private float zipScale = 0.2f;
    [SerializeField] private float arrivalThreshold = 0.4f;
    [SerializeField] private LineRenderer cable;

    public Transform ZipTransform;

    private bool zipping = false;
    private GameObject localZip;
    private CharacterController characterController;
    private ThirdPersonController controller; 

    void Awake()
    {
        cable.SetPosition(0, ZipTransform.position);
        cable.SetPosition(1, targetZip.ZipTransform.position);
    }

    void Update()
    {
        if (!zipping || localZip == null) return;


        Vector3 dir = (targetZip.ZipTransform.position - ZipTransform.position).normalized;
        localZip.transform.position += dir * zipSpeed * Time.deltaTime;

        if (Vector3.Distance(localZip.transform.position, targetZip.ZipTransform.position) <= arrivalThreshold)
        {
            ResetZipline();
        }
    }

    public void StartZipline(GameObject player)
    {
        if (zipping) return;


        localZip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        localZip.transform.position = ZipTransform.position;
        localZip.transform.localScale = Vector3.one * zipScale;
        localZip.GetComponent<Collider>().isTrigger = true;
        localZip.GetComponent<MeshRenderer>().enabled = false;


        characterController = player.GetComponent<CharacterController>();
        controller = player.GetComponent<ThirdPersonController>();

        if (controller) controller.enabled = false;
        if (characterController) characterController.enabled = false;

        player.transform.parent = localZip.transform;
        player.transform.localPosition = new Vector3(0, -2f, 0);
        player.transform.localRotation = Quaternion.identity;

        zipping = true;
    }

    private void ResetZipline()
    {
        if (!zipping) return;

        GameObject player = localZip.transform.GetChild(0).gameObject;
        player.transform.parent = null;


        player.transform.position = targetZip.ZipTransform.position;

        if (characterController) characterController.enabled = true;
        if (controller) controller.enabled = true;

        Destroy(localZip);
        localZip = null;
        zipping = false;

    }
}
