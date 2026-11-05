using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Parte 2 de la herramienta: los prefabs de ambientacion (todos en Assets/Prefabs/Ambientacion).
//   Luminaria         tubo fluorescente (patron por instancia; trae sus chispas para el patron Rota)
//   Rejilla           rejilla de ventilacion con vapor
//   Tuberia           tramo de cano con codo opcional (hijo "Codo")
//   Cable             cables colgando del techo
//   PlacaTecho        placa de techo caida
//   Escombro*         EscombroCaja / Silla / Bidon / Papeles / Botella: flotan cuando sube el agua
//   CartelSalida      cartel SALIDA con brillo verde
//   NieblaAgua        niebla baja que sigue la superficie del agua
//   ParticulasAgua    burbujas; va desactivado en el Player (ColliderOxigeno) y lo activa Oxigeno al entrar al agua
public static partial class CrearAmbientacion
{
    private static void CrearPrefabs()
    {
        CrearCarpeta(CarpetaPrefabs);
        ConfigurarAudio();

        CrearLuminaria();
        CrearRejilla();
        PrefabDesdeModelo("Tuberia", "Tuberia");
        PrefabDesdeModelo("Cable", "Cable");
        PrefabDesdeModelo("PlacaTecho", "PlacaTecho");
        foreach (var e in Escombros) PrefabDesdeModelo(e, e, raiz => raiz.AddComponent<FlotanteEnAgua>());
        CrearCartelSalida();
        CrearNieblaAgua();
        CrearParticulasAgua();
        PonerParticulasEnJugador();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigurarAudio()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { CarpetaAudio }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(ruta) is not AudioImporter imp) continue;
            var s = imp.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.DecompressOnLoad && imp.forceToMono) continue;
            imp.forceToMono = true;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }
    }

    // ---------------- Helpers ----------------

    private static Material Mat(string nombre) => AssetDatabase.LoadAssetAtPath<Material>($"{CarpetaMat}/{nombre}.mat");
    private static AudioClip Clip(string nombre) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{CarpetaAudio}/{nombre}.wav");
    public static readonly string[] Escombros = { "EscombroCaja", "EscombroSilla", "EscombroBidon", "EscombroPapeles", "EscombroBotella" };

    public static GameObject Prefab(string nombre) => AssetDatabase.LoadAssetAtPath<GameObject>($"{CarpetaPrefabs}/{nombre}.prefab");

    // Prefab a partir de un FBX de Blender (instancia en el hijo "Modelo"): materiales URP propios y sin
    // colliders (es decorado: no debe alterar wall runs, NavMesh ni la vision de los monstruos).
    private static void PrefabDesdeModelo(string fbx, string nombre, System.Action<GameObject> extra = null)
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>($"{CarpetaModelos}/{fbx}.fbx");
        if (modelo == null) { Debug.LogError($"[Ambientacion] Falta {CarpetaModelos}/{fbx}.fbx (corre SourceArt/Ambientacion/generar_props.py)."); return; }
        var raiz = new GameObject(nombre);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        inst.name = "Modelo";
        inst.transform.SetParent(raiz.transform, false);
        foreach (var c in raiz.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        foreach (var r in raiz.GetComponentsInChildren<Renderer>()) AsignarMaterialesProp(r);
        extra?.Invoke(raiz);
        GuardarPrefab(raiz);
    }

    private static void GuardarPrefab(GameObject go)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{CarpetaPrefabs}/{go.name}.prefab");
        Object.DestroyImmediate(go);
    }

    private static ParticleSystem NuevoPS(GameObject padre, string nombre, string material, int max)
    {
        var go = new GameObject(nombre);
        if (padre != null) go.transform.SetParent(padre.transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.maxParticles = max;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Mat(material);
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return ps;
    }

    private static ParticleSystem.MinMaxCurve R(float a, float b) => new(a, b);

    private static Gradient Desvanecer(float pico = 1f, float entrada = 0.15f, float salida = 0.7f)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(pico, entrada), new GradientAlphaKey(pico, salida), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    private static void ColorEnVida(ParticleSystem ps, Gradient g)
    {
        var c = ps.colorOverLifetime;
        c.enabled = true;
        c.color = g;
    }

    private static void TamanioEnVida(ParticleSystem ps, float desde, float hasta)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, desde, 1f, hasta));
    }

    private static void Ruido(ParticleSystem ps, float fuerza, float frecuencia)
    {
        var n = ps.noise;
        n.enabled = true;
        n.strength = fuerza;
        n.frequency = frecuencia;
        n.scrollSpeed = 0.2f;
        n.quality = ParticleSystemNoiseQuality.Low;
    }

    private static void Fuente3D(AudioSource a, AudioClip clip, float volumen, float min, float max, bool loop)
    {
        a.clip = clip;
        a.volume = volumen;
        a.loop = loop;
        a.playOnAwake = loop;
        a.spatialBlend = 1f;
        a.dopplerLevel = 0f;
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = min;
        a.maxDistance = max;
    }

    // ---------------- Prefabs ----------------

    private static void CrearLuminaria()
    {
        PrefabDesdeModelo("Luminaria", "Luminaria", raiz =>
        {
            var modelo = raiz.transform.Find("Modelo");
            Renderer difusor = null;
            foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            {
                // El FBX trae "Difusor" (emisivo) y "Metal" (carcasa).
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    bool esDifusor = mats[i] != null && mats[i].name == "MP_Difusor";
                    mats[i] = esDifusor ? Mat("M_Luminaria_Difusor") : Mat("M_Luminaria_Carcasa");
                    if (esDifusor) difusor = r;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }

            var goLuz = new GameObject("Luz");
            goLuz.transform.SetParent(raiz.transform, false);
            goLuz.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            goLuz.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var luz = goLuz.AddComponent<Light>();
            luz.type = LightType.Spot;
            luz.spotAngle = 150f;
            luz.innerSpotAngle = 70f;
            luz.range = 9f;
            luz.intensity = 4f;
            luz.color = new Color(1f, 0.95f, 0.82f);
            luz.shadows = LightShadows.None;
            luz.lightmapBakeType = LightmapBakeType.Realtime;

            // Chispas: solo con el patron Rota (LuzFluorescente las activa).
            var chispas = NuevoPS(raiz, "Chispas", "M_Particula_Chispa", 80);
            chispas.transform.localPosition = new Vector3(0.45f, -0.1f, 0f);
            var m = chispas.main;
            m.startLifetime = R(0.3f, 0.8f); m.startSpeed = R(1.5f, 4f); m.startSize = R(0.01f, 0.025f); m.gravityModifier = 1f;
            var e = chispas.emission; e.rateOverTime = 0f;
            e.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.2f, new ParticleSystem.MinMaxCurve(6, 18), 999, 1.3f) { probability = 0.35f },
                new ParticleSystem.Burst(0.9f, new ParticleSystem.MinMaxCurve(2, 6), 999, 0.7f) { probability = 0.25f },
            });
            var sh = chispas.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.03f;
            var pr = chispas.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = 0.03f; pr.lengthScale = 1f;
            ColorEnVida(chispas, Desvanecer(1f, 0.01f, 0.5f));
            chispas.gameObject.SetActive(false);

            var zumbido = raiz.AddComponent<AudioSource>();
            Fuente3D(zumbido, Clip("SFX_ZumbidoFluorescente"), 0.018f, 0.5f, 5f, true);
            var chispazo = raiz.AddComponent<AudioSource>();
            Fuente3D(chispazo, Clip("SFX_Chispazo"), 0.3f, 1f, 12f, false);

            var lf = raiz.AddComponent<LuzFluorescente>();
            var so = new SerializedObject(lf);
            so.FindProperty("luz").objectReferenceValue = luz;
            var em = so.FindProperty("emisores");
            em.arraySize = difusor != null ? 1 : 0;
            if (difusor != null) em.GetArrayElementAtIndex(0).objectReferenceValue = difusor;
            so.FindProperty("zumbido").objectReferenceValue = zumbido;
            so.FindProperty("chispazo").objectReferenceValue = chispazo;
            so.FindProperty("chispas").objectReferenceValue = chispas.gameObject;
            so.FindProperty("modelo").objectReferenceValue = modelo;
            so.ApplyModifiedPropertiesWithoutUndo();
        });
    }

    private static void CrearRejilla()
    {
        PrefabDesdeModelo("Rejilla", "Rejilla", raiz =>
        {
            var ps = NuevoPS(raiz, "Vapor", "M_Particula_Humo", 40);
            ps.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            var m = ps.main;
            m.startLifetime = R(3f, 5f); m.startSpeed = R(0.3f, 0.7f); m.startSize = R(0.45f, 0.9f); m.startRotation = R(0f, 6.28f);
            m.prewarm = true;
            var e = ps.emission; e.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.08f;
            TamanioEnVida(ps, 0.6f, 3.5f);
            ColorEnVida(ps, Desvanecer(0.8f, 0.15f, 0.4f));
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.4f, 0.4f);
            Ruido(ps, 0.3f, 0.4f);
        });
    }

    private static void CrearCartelSalida()
    {
        PrefabDesdeModelo("CartelSalida", "CartelSalida", raiz =>
        {
            var l = new GameObject("Brillo").AddComponent<Light>();
            l.transform.SetParent(raiz.transform, false);
            l.transform.localPosition = new Vector3(0f, -0.14f, 0.25f);
            l.type = LightType.Point; l.range = 1.2f; l.intensity = 0.06f; l.color = new Color(0.2f, 1f, 0.35f);
            l.shadows = LightShadows.None;
        });
    }

    private static void CrearNieblaAgua()
    {
        var raiz = new GameObject("NieblaAgua");
        raiz.AddComponent<SeguirSuperficieAgua>();
        var ps = NuevoPS(raiz, "Niebla", "M_Particula_Niebla", 70);
        var m = ps.main;
        m.startLifetime = R(9f, 15f); m.startSpeed = 0f; m.startSize = R(2.5f, 5f); m.startRotation = R(0f, 6.28f);
        m.prewarm = true;
        var e = ps.emission; e.rateOverTime = 4f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10f, 0.4f, 10f);
        ColorEnVida(ps, Desvanecer(1f, 0.25f, 0.7f));
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.08f, 0.08f);
        Ruido(ps, 0.15f, 0.1f);
        ps.GetComponent<ParticleSystemRenderer>().sortingFudge = 10f;
        GuardarPrefab(raiz);
    }

    private static void CrearParticulasAgua()
    {
        var raiz = new GameObject("ParticulasAgua");
        var burbujas = NuevoPS(raiz, "Burbujas", "M_Particula_Burbuja", 150);
        var bm = burbujas.main;
        bm.startLifetime = R(1.5f, 3f); bm.startSpeed = 0f; bm.startSize = R(0.015f, 0.07f);
        var be = burbujas.emission; be.rateOverTime = 22f;
        var bs = burbujas.shape; bs.shapeType = ParticleSystemShapeType.Sphere; bs.radius = 1.2f;
        var vel = burbujas.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = R(-0.05f, 0.05f); vel.y = R(0.5f, 1.3f); vel.z = R(-0.05f, 0.05f);
        Ruido(burbujas, 0.25f, 1.2f);
        ColorEnVida(burbujas, Desvanecer(1f, 0.1f, 0.8f));
        GuardarPrefab(raiz);
    }

    // Pone ParticulasAgua (desactivado) como hijo de ColliderOxigeno en el prefab del Player y lo asigna
    // a Oxigeno, que lo activa en EntrarAgua y lo desactiva en SalirAgua.
    private static void PonerParticulasEnJugador()
    {
        const string ruta = "Assets/Prefabs/Player.prefab";
        var prefab = Prefab("ParticulasAgua");
        var raiz = PrefabUtility.LoadPrefabContents(ruta);
        var collider = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ColliderOxigeno");
        if (collider == null || prefab == null)
        {
            Debug.LogError("[Ambientacion] No se encontro ColliderOxigeno en el Player (o falta ParticulasAgua).");
            PrefabUtility.UnloadPrefabContents(raiz);
            return;
        }
        var viejo = collider.Find("ParticulasAgua");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, collider);
        inst.name = "ParticulasAgua";
        inst.SetActive(false);
        foreach (var ox in collider.GetComponents<Oxigeno>())
        {
            var so = new SerializedObject(ox);
            so.FindProperty("particulasAgua").objectReferenceValue = inst;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    // ---------------- Materiales de props ----------------

    // Los FBX traen materiales con nombre (ej. "Metal", "Carton", "Emisivo_Verde"): se mapean a materiales URP "MP_<nombre>".
    private static void AsignarMaterialesProp(Renderer r)
    {
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            mats[i] = MaterialProp(mats[i] != null ? mats[i].name : "Default");
        r.sharedMaterials = mats;
    }

    private static Material MaterialProp(string nombre)
    {
        string baseN = nombre.Split('.')[0]; // sin sufijos de import (".001")
        string id = "MP_" + baseN;
        var existente = Mat(id);
        if (existente != null) return existente;

        var m = ObtenerMaterial(id, Shader.Find("Universal Render Pipeline/Lit"));
        string l = baseN.ToLower();
        Color c = new(0.5f, 0.5f, 0.5f); float met = 0f, suav = 0.3f; Color em = Color.black;
        if (l.Contains("metal")) { c = new Color(0.55f, 0.56f, 0.55f); met = 0.85f; suav = 0.45f; }
        else if (l.Contains("carton")) { c = new Color(0.55f, 0.42f, 0.28f); suav = 0.1f; }
        else if (l.Contains("plastico_azul")) { c = new Color(0.12f, 0.25f, 0.45f); suav = 0.55f; }
        else if (l.Contains("plastico")) { c = new Color(0.12f, 0.12f, 0.13f); suav = 0.5f; }
        else if (l.Contains("tela")) { c = new Color(0.18f, 0.2f, 0.24f); suav = 0.1f; }
        else if (l.Contains("papel")) { c = new Color(0.8f, 0.78f, 0.7f); suav = 0.1f; }
        else if (l.Contains("goma")) { c = new Color(0.05f, 0.05f, 0.05f); suav = 0.35f; }
        else if (l.Contains("amarillo")) { c = new Color(0.85f, 0.65f, 0.05f); suav = 0.4f; }
        else if (l.Contains("emisivo_verde")) { c = new Color(0.1f, 0.5f, 0.15f); em = new Color(0.2f, 1f, 0.35f) * 2.5f; }
        else if (l.Contains("blanco") || l.Contains("difusor")) { c = new Color(0.85f, 0.85f, 0.82f); suav = 0.3f; }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", met);
        m.SetFloat("_Smoothness", suav);
        if (em != Color.black)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", em);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
