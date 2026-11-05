using UnityEngine;

// Muestra en la pantalla del televisor lo que ve la cámara de seguridad (CameraTV).
// El prefab no puede referenciar objetos de la escena, así que si "camaraTV" está vacío
// se busca en la escena por nombre.
public class TelevisorCCTV : MonoBehaviour
{
    [Header("Cámara")]
    [SerializeField] private Camera camaraTV;
    [SerializeField] private string nombreCamaraEnEscena = "CameraTV";

    [Header("Pantalla")]
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Material materialPantalla;
    [Tooltip("Si se deja vacío, busca el hijo con el nombre de abajo.")]
    [SerializeField] private Renderer rendererPantalla;
    [SerializeField] private string nombrePantalla = "pantalla";
    [Tooltip("Hijo del modelo que está detrás de la pantalla; se usa para saber cuál es el frente.")]
    [SerializeField] private string nombreTrasera = "base";
    [Tooltip("Activar si la imagen aparece en la parte de atrás del televisor.")]
    [SerializeField] private bool invertirLado;
    [Tooltip("Separación (m) entre la imagen y la cara de la pantalla, para evitar z-fighting.")]
    [SerializeField] private float separacionPantalla = 0.005f;
    [Tooltip("Tamaño de la imagen respecto a la cara de la pantalla (1 = toda la cara).")]
    [Range(0.5f, 1f)]
    [SerializeField] private float margenPantalla = 1f;
    [SerializeField] private bool espejoHorizontal;
    [SerializeField] private bool espejoVertical;

    private void Start()
    {
        ConfigurarPantalla();
        ConfigurarCamara();
    }

    private void ConfigurarPantalla()
    {
        if (rendererPantalla == null)
        {
            Transform hijo = BuscarHijo(transform, nombrePantalla);
            if (hijo != null) rendererPantalla = hijo.GetComponent<Renderer>();
        }

        MeshFilter filtro = rendererPantalla != null ? rendererPantalla.GetComponent<MeshFilter>() : null;
        if (filtro == null || filtro.sharedMesh == null || materialPantalla == null)
        {
            Debug.LogWarning($"[TelevisorCCTV] Falta la pantalla o su material en '{name}'.", this);
            return;
        }

        // La malla "pantalla" del FBX es un cubo con el unwrap por defecto de Blender: cada cara usa solo
        // 1/4 x 1/4 de la textura y rotada 90°. Por eso no se le pone la textura a esa malla: se crea un
        // Quad (UV 0..1 correctas) pegado a su cara frontal.
        Transform tPantalla = rendererPantalla.transform;
        Bounds b = filtro.sharedMesh.bounds; // espacio local de la malla (no requiere Read/Write)

        // Semiejes del cubo en espacio mundo (incluyen escala y rotación del modelo)
        Vector3[] semiejes =
        {
            tPantalla.TransformVector(new Vector3(b.extents.x, 0f, 0f)),
            tPantalla.TransformVector(new Vector3(0f, b.extents.y, 0f)),
            tPantalla.TransformVector(new Vector3(0f, 0f, b.extents.z)),
        };

        // Eje más fino = grosor (normal de la pantalla); de los otros dos, el más vertical = alto
        int iGrosor = 0;
        for (int i = 1; i < 3; i++)
            if (semiejes[i].sqrMagnitude < semiejes[iGrosor].sqrMagnitude) iGrosor = i;
        int iA = (iGrosor + 1) % 3, iB = (iGrosor + 2) % 3;
        bool aEsAlto = Mathf.Abs(Vector3.Dot(semiejes[iA].normalized, Vector3.up)) >=
                       Mathf.Abs(Vector3.Dot(semiejes[iB].normalized, Vector3.up));
        Vector3 semiAlto = aEsAlto ? semiejes[iA] : semiejes[iB];
        Vector3 semiAncho = aEsAlto ? semiejes[iB] : semiejes[iA];
        if (Vector3.Dot(semiAlto, Vector3.up) < 0f) semiAlto = -semiAlto;

        // Lado frontal: el opuesto a la carcasa trasera ("base" en el FBX)
        Vector3 centro = tPantalla.TransformPoint(b.center);
        Vector3 frente = semiejes[iGrosor];
        Transform trasera = BuscarHijo(transform, nombreTrasera);
        if (trasera != null && Vector3.Dot(frente, centro - trasera.position) < 0f) frente = -frente;
        if (invertirLado) frente = -frente;

        // Quad: su cara visible mira hacia -Z local, así que su +Z apunta hacia dentro del televisor
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "PantallaCCTV";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(transform, false);
        quad.transform.SetPositionAndRotation(
            centro + frente + frente.normalized * separacionPantalla,
            Quaternion.LookRotation(-frente.normalized, semiAlto.normalized));
        Vector3 escalaPadre = transform.lossyScale;
        quad.transform.localScale = new Vector3(
            semiAncho.magnitude * 2f * margenPantalla / escalaPadre.x,
            semiAlto.magnitude * 2f * margenPantalla / escalaPadre.y,
            1f);

        // Instancia propia del material para poder espejar sin modificar el asset compartido
        Material mat = new Material(materialPantalla);
        if (renderTexture != null) mat.SetTexture("_BaseMap", renderTexture);
        mat.SetTextureScale("_BaseMap", new Vector2(espejoHorizontal ? -1f : 1f, espejoVertical ? -1f : 1f));
        mat.SetTextureOffset("_BaseMap", new Vector2(espejoHorizontal ? 1f : 0f, espejoVertical ? 1f : 0f));

        MeshRenderer rQuad = quad.GetComponent<MeshRenderer>();
        rQuad.sharedMaterial = mat;
        rQuad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rQuad.receiveShadows = false;
    }

    private void ConfigurarCamara()
    {
        if (camaraTV == null)
        {
            GameObject go = GameObject.Find(nombreCamaraEnEscena);
            if (go != null) camaraTV = go.GetComponent<Camera>();
        }

        if (camaraTV == null)
        {
            Debug.LogWarning($"[TelevisorCCTV] No se encontró la cámara '{nombreCamaraEnEscena}'.", this);
            return;
        }

        // Renderiza a la textura en vez de a la pantalla del jugador
        camaraTV.targetTexture = renderTexture;

        // Solo puede haber un AudioListener activo (el de la cámara principal)
        AudioListener listener = camaraTV.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = false;

        if (camaraTV.GetComponent<CamaraSeguridad>() == null)
            camaraTV.gameObject.AddComponent<CamaraSeguridad>();
    }

    private static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre)
        {
            if (hijo.name == nombre) return hijo;
            Transform encontrado = BuscarHijo(hijo, nombre);
            if (encontrado != null) return encontrado;
        }
        return null;
    }
}
