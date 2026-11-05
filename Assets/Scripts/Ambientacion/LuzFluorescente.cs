using System.Collections.Generic;
using UnityEngine;

// Tubo fluorescente: parpadeo segun patron, emision sincronizada (MaterialPropertyBlock) y zumbido.
// Se gestionan solas: cada 0,25 s (una vez para todas) quedan como luz real solo las mas cercanas a la
// camara, y las que estan cerca de un monstruo parpadean.
public class LuzFluorescente : MonoBehaviour
{
    public enum Patron { Estable, Intermitente, Muriendo, Rota }

    [SerializeField] private Patron patron = Patron.Estable;
    [SerializeField] private Light luz;
    [Tooltip("Renderers cuyo material emisivo se apaga/prende con la luz.")]
    [SerializeField] private Renderer[] emisores;
    [SerializeField] private Color colorEmision = new(1f, 0.95f, 0.8f);
    [SerializeField] private float intensidadEmision = 3f;
    [SerializeField] private AudioSource zumbido;
    [SerializeField] private AudioSource chispazo;
    [Tooltip("Chispas que solo se activan con el patron Rota.")]
    [SerializeField] private GameObject chispas;
    [Tooltip("Modelo de la luminaria: con el patron Rota queda medio caido.")]
    [SerializeField] private Transform modelo;
    [Tooltip("Si es true, en calidad baja se apaga el componente Light (queda solo la emision).")]
    [SerializeField] private bool prescindibleEnCalidadBaja = true;

    private static readonly List<LuzFluorescente> todas = new();
    public static IReadOnlyList<LuzFluorescente> Todas => todas;

    private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock bloque;
    private float intensidadBase;
    private float volumenZumbido;
    private float semilla;
    private float perturbacion;      // 0..1, decae sola
    private float apagadaHasta;      // parpadeo forzado
    private float nivelActual = 1f;
    private float proximoEvento;

    public Patron PatronActual { get => patron; set => patron = value; }

    // Calidad baja: WebGL o el nivel de calidad "Mobile" (indice 0). Menos luces reales y menos particulas.
    public static bool CalidadBaja =>
        Application.platform == RuntimePlatform.WebGLPlayer || QualitySettings.GetQualityLevel() == 0;

    private const int LucesMaximas = 40, LucesMaximasCalidadBaja = 12;
    private const float RadioLuces = 40f, DistanciaMonstruo = 27f, RadioMonstruo = 9f;
    private static float proximoTick;

    // Una sola vez cada 0,25 s para todas las luminarias.
    private static void Tick()
    {
        if (Time.unscaledTime < proximoTick) return;
        proximoTick = Time.unscaledTime + 0.25f;
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 pos = cam.transform.position;
        ActualizarCulling(pos, CalidadBaja ? LucesMaximasCalidadBaja : LucesMaximas, RadioLuces);
        foreach (var e in Enemigo.Activos)
        {
            if (e == null || !e.isActiveAndEnabled) continue;
            if (Vector3.Distance(pos, e.transform.position) > DistanciaMonstruo) continue;
            bool caza = e.EstadoActual == Enemigo.Estado.Persiguiendo || e.EstadoActual == Enemigo.Estado.Atacando;
            PerturbarEnRadio(e.transform.position, RadioMonstruo, caza ? 0.9f : 0.35f, 0.5f);
        }
    }

    // ---- Culling de luces reales: solo las N mas cercanas a la camara tienen componente Light activo
    // (Forward+ / WebGL tienen un tope de luces visibles; las lejanas quedan solo con emision). ----
    private static readonly List<(float d, LuzFluorescente l)> orden = new();
    private const int ZumbidosMaximos = 6;

    public static void ActualizarCulling(Vector3 camara, int maximo, float radio)
    {
        orden.Clear();
        float r2 = radio * radio;
        // En modo edicion (capturas) OnEnable no corre: se buscan en la escena.
        IEnumerable<LuzFluorescente> fuente = Application.isPlaying
            ? todas
            : FindObjectsByType<LuzFluorescente>(FindObjectsSortMode.None);
        foreach (var l in fuente)
        {
            if (l.luz == null) continue;
            float d = (l.transform.position - camara).sqrMagnitude;
            orden.Add((d, l));
        }
        orden.Sort((a, b) => a.d.CompareTo(b.d));
        bool baja = CalidadBaja;
        for (int i = 0; i < orden.Count; i++)
        {
            var l = orden[i].l;
            bool activa = i < maximo && orden[i].d < r2 && !(baja && l.prescindibleEnCalidadBaja);
            if (l.luz.enabled != activa) l.luz.enabled = activa;
            // Zumbido solo en las mas cercanas (evita cientos de voces de audio en loop).
            if (l.zumbido != null)
            {
                bool suena = i < ZumbidosMaximos && orden[i].d < 15f * 15f && l.patron != Patron.Rota;
                if (l.zumbido.enabled != suena) l.zumbido.enabled = suena;
            }
        }
    }

    private void Awake()
    {
        if (luz == null) luz = GetComponentInChildren<Light>();
        bloque = new MaterialPropertyBlock();
        intensidadBase = luz != null ? luz.intensity : 1f;
        volumenZumbido = zumbido != null ? zumbido.volume : 0f;
        semilla = Random.value * 100f;
        if (zumbido != null && zumbido.clip != null)
        {
            zumbido.time = Random.value * zumbido.clip.length;
            zumbido.pitch = Random.Range(0.97f, 1.03f);
        }
    }

    private void OnEnable() => todas.Add(this);
    private void OnDisable() => todas.Remove(this);

    private void Start()
    {
        AplicarAspectoPatron();
        if (patron == Patron.Rota) Aplicar(0f);
    }

    // Rota: chispas encendidas y carcasa inclinada. La herramienta lo aplica tambien al colocar (se ve en el editor).
    public void AplicarAspectoPatron()
    {
        bool rota = patron == Patron.Rota;
        if (chispas != null && chispas.activeSelf != rota) chispas.SetActive(rota);
        if (modelo != null) modelo.localRotation = rota ? Quaternion.Euler(0f, 0f, 9f) : Quaternion.identity;
    }

    // Parpadeo forzado (monstruo cerca, desde Tick): cantidad 0..1 durante 'segundos'.
    public void Perturbar(float cantidad, float segundos)
    {
        perturbacion = Mathf.Max(perturbacion, cantidad);
        if (cantidad > 0.6f && Random.value < 0.5f) apagadaHasta = Time.time + Random.Range(0.05f, Mathf.Min(0.4f, segundos));
    }

    public static void PerturbarEnRadio(Vector3 centro, float radio, float cantidad, float segundos)
    {
        float r2 = radio * radio;
        foreach (var l in todas)
        {
            float d2 = (l.transform.position - centro).sqrMagnitude;
            if (d2 > r2) continue;
            l.Perturbar(cantidad * (1f - d2 / r2), segundos);
        }
    }

    private void Update()
    {
        Tick();
        float t = Time.time;
        float nivel;
        switch (patron)
        {
            case Patron.Rota:
                // Casi siempre apagada, chispazos ocasionales.
                nivel = 0f;
                if (t > proximoEvento)
                {
                    proximoEvento = t + Random.Range(3f, 9f);
                    apagadaHasta = -1f;
                    StartCoroutine(Chispazo());
                }
                break;
            case Patron.Muriendo:
                // Tenue, inestable, se corta seguido.
                nivel = 0.45f + 0.25f * Mathf.PerlinNoise(semilla, t * 6f);
                if (t > proximoEvento)
                {
                    proximoEvento = t + Random.Range(0.4f, 2.5f);
                    apagadaHasta = t + Random.Range(0.05f, 0.6f);
                    if (Random.value < 0.3f) Sonar();
                }
                break;
            case Patron.Intermitente:
                nivel = 1f;
                if (t > proximoEvento)
                {
                    proximoEvento = t + Random.Range(2f, 7f);
                    apagadaHasta = t + Random.Range(0.04f, 0.25f);
                }
                break;
            default:
                nivel = 1f - 0.04f * Mathf.PerlinNoise(semilla, t * 30f); // zumbido visual minimo
                break;
        }

        if (perturbacion > 0f)
        {
            float ruido = Mathf.PerlinNoise(semilla + 7f, t * 25f);
            nivel *= Mathf.Lerp(1f, ruido > 0.5f ? 1f : 0.1f, perturbacion);
            perturbacion = Mathf.MoveTowards(perturbacion, 0f, Time.deltaTime * 0.5f);
        }

        if (t < apagadaHasta) nivel = 0.02f;
        if (Mathf.Abs(nivel - nivelActual) > 0.01f) Aplicar(nivel);
    }

    private System.Collections.IEnumerator Chispazo()
    {
        Sonar();
        for (int i = 0; i < Random.Range(1, 4); i++)
        {
            Aplicar(Random.Range(0.6f, 1f));
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
            Aplicar(0f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.15f));
        }
    }

    private void Sonar()
    {
        if (chispazo != null && chispazo.clip != null)
        {
            chispazo.pitch = Random.Range(0.85f, 1.15f);
            chispazo.PlayOneShot(chispazo.clip, Random.Range(0.4f, 1f));
        }
    }

    private void Aplicar(float nivel)
    {
        nivelActual = nivel;
        if (luz != null) luz.intensity = intensidadBase * nivel;
        if (zumbido != null) zumbido.volume = volumenZumbido * Mathf.Clamp01(nivel * 1.2f);
        if (emisores == null) return;
        Color c = colorEmision * (intensidadEmision * nivel);
        foreach (var r in emisores)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, c);
            r.SetPropertyBlock(bloque);
        }
    }
}
