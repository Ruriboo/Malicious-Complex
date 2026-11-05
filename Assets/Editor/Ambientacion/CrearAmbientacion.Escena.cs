using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Parte 3 de la herramienta: puebla la escena abierta bajo un unico objeto raiz "Ambientacion"
// (una carpeta por zona). Re-ejecutable: borra y recrea ese objeto. Fuera de el solo toca: el perfil
// del "Global Volume", RenderSettings (niebla/ambiente/reflejos), materiales de pared/piso/techo
// por zona y el material del agua en el prefab FloodWater.
public static partial class CrearAmbientacion
{
    public const string NombreRaiz = "Ambientacion";
    private const int Semilla = 1337;

    private struct Zona
    {
        public string nombre;
        public List<Vector3> anclas;
        public List<FloodWater> aguas;
    }

    // Por zona: color e intensidad de los tubos (laboratorio frio -> backrooms amarillo).
    private static readonly (Color luz, float intensidad)[] PerfilesZona =
    {
        (new Color(0.90f, 0.97f, 1.00f), 14f),
        (new Color(0.95f, 0.98f, 0.95f), 13f),
        (new Color(1.00f, 0.95f, 0.82f), 12f),
        (new Color(1.00f, 0.91f, 0.68f), 11f),
        (new Color(1.00f, 0.87f, 0.58f), 10f),
    };

    // Niebla y luz ambiente (fijas para todo el nivel).
    private static readonly Color Niebla = new(0.21f, 0.2f, 0.17f);
    private const float DensidadNiebla = 0.02f;
    private static readonly Color Ambiente = new(0.14f, 0.135f, 0.12f);

    // Probabilidad de patron por zona: (intermitente, muriendo, rota)
    private static readonly (float inter, float muere, float rota)[] FallasZona =
    {
        (0.04f, 0.02f, 0.00f), (0.08f, 0.05f, 0.02f), (0.12f, 0.10f, 0.06f), (0.16f, 0.16f, 0.12f), (0.18f, 0.24f, 0.18f),
    };

    [MenuItem("Tools/Ambientacion/2. Poblar escena abierta")]
    public static void PoblarEscena()
    {
        var escena = EditorSceneManager.GetActiveScene();
        var viejo = GameObject.Find(NombreRaiz);
        if (viejo != null) Object.DestroyImmediate(viejo);

        var raiz = new GameObject(NombreRaiz);
        var rnd = new System.Random(Semilla);
        var zonas = LeerZonas();
        if (zonas.Count == 0) { Debug.LogError("[Ambientacion] No hay CheckpointManager con zonas en la escena."); return; }

        // Solo interiores: el piso exterior (Plane) no lleva ambientacion.
        var puntos = MuestrearNavMesh(rnd, 1.6f).Where(p => Techo(p, out _)).ToList();
        Debug.Log($"[Ambientacion] {puntos.Count} puntos interiores de NavMesh muestreados.");
        var porZona = new List<Vector3>[zonas.Count];
        for (int i = 0; i < zonas.Count; i++) porZona[i] = new List<Vector3>();
        foreach (var p in puntos) porZona[ZonaDe(zonas, p)].Add(p);

        var grupos = new Transform[zonas.Count];
        for (int i = 0; i < zonas.Count; i++)
        {
            grupos[i] = new GameObject(zonas[i].nombre).transform;
            grupos[i].SetParent(raiz.transform, false);
        }

        ConfigurarEntornoGlobal();

        int luces = 0, decals = 0, props = 0;
        var ocupado = new HashSet<Vector3Int>();
        int sombras = 0;
        for (int z = 0; z < zonas.Count; z++)
        {
            int nivel = Mathf.Clamp(z, 0, PerfilesZona.Length - 1);
            luces += ColocarLuminarias(grupos[z], porZona[z], nivel, rnd, ocupado, ref sombras);
            props += ColocarRejillas(grupos[z], porZona[z], nivel, rnd);
            decals += ColocarDecals(grupos[z], porZona[z], zonas[z], nivel, rnd);
            props += ColocarProps(grupos[z], porZona[z], zonas[z], nivel, rnd);
            ColocarNieblaAgua(grupos[z], zonas[z]);
        }
        props += ColocarCarteles(raiz.transform);
        int mats = AplicarMaterialesZona(zonas);
        Debug.Log($"[Ambientacion] {mats} renderers con material de zona.");

        AplicarMaterialAgua();
        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log($"[Ambientacion] Escena poblada: {luces} luminarias ({sombras} con sombra), {decals} decals, {props} props.");
    }

    // ---------------- Zonas ----------------

    private static List<Zona> LeerZonas()
    {
        var lista = new List<Zona>();
        var cm = Object.FindFirstObjectByType<CheckpointManager>();
        if (cm == null) return lista;
        var zonas = new SerializedObject(cm).FindProperty("zonasDelNivel");
        for (int i = 0; i < zonas.arraySize; i++)
        {
            var z = zonas.GetArrayElementAtIndex(i);
            var zona = new Zona { nombre = z.FindPropertyRelative("nombreZona").stringValue, anclas = new List<Vector3>(), aguas = new List<FloodWater>() };
            if (z.FindPropertyRelative("puntoEntrada").objectReferenceValue is Transform e) zona.anclas.Add(e.position);
            var aguas = z.FindPropertyRelative("aguas");
            for (int j = 0; j < aguas.arraySize; j++)
                if (aguas.GetArrayElementAtIndex(j).objectReferenceValue is FloodWater a)
                {
                    zona.aguas.Add(a);
                    var b = a.GetComponent<Collider>().bounds;
                    zona.anclas.Add(new Vector3(b.center.x, b.max.y + 1f, b.center.z));
                }
            // Objetos hijos del GameObject de la zona (checkpoints, cierres) tambien la anclan.
            var go = GameObject.Find("/" + zona.nombre);
            if (go != null) foreach (Transform h in go.transform) zona.anclas.Add(h.position);
            lista.Add(zona);
        }
        // El cuarto inicial del jugador pertenece a la zona 1 (laboratorio limpio).
        var tpc = Object.FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        if (tpc != null && lista.Count > 0) lista[0].anclas.Add(tpc.transform.position);
        return lista;
    }

    // Zona mas cercana (la altura pesa doble: los niveles estan apilados).
    private static int ZonaDe(List<Zona> zonas, Vector3 p)
    {
        int mejor = 0; float dMin = float.MaxValue;
        for (int i = 0; i < zonas.Count; i++)
            foreach (var a in zonas[i].anclas)
            {
                Vector3 d = p - a; d.y *= 2f;
                float m = d.sqrMagnitude;
                if (m < dMin) { dMin = m; mejor = i; }
            }
        return mejor;
    }

    // Puntos repartidos sobre la NavMesh (por area), deduplicados en una grilla de 'separacion' m.
    private static List<Vector3> MuestrearNavMesh(System.Random rnd, float separacion)
    {
        var tri = NavMesh.CalculateTriangulation();
        var resultado = new List<Vector3>();
        if (tri.indices.Length == 0) { Debug.LogWarning("[Ambientacion] NavMesh vacia: hornea con Tools/Enemigos/2."); return resultado; }
        var celdas = new HashSet<Vector3Int>();
        for (int i = 0; i < tri.indices.Length; i += 3)
        {
            Vector3 a = tri.vertices[tri.indices[i]], b = tri.vertices[tri.indices[i + 1]], c = tri.vertices[tri.indices[i + 2]];
            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            int n = Mathf.Max(1, Mathf.CeilToInt(area / (separacion * separacion)));
            for (int k = 0; k < n; k++)
            {
                float r1 = (float)rnd.NextDouble(), r2 = (float)rnd.NextDouble();
                if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
                Vector3 p = a + (b - a) * r1 + (c - a) * r2;
                var celda = Vector3Int.FloorToInt(p / separacion);
                if (celdas.Add(celda)) resultado.Add(p);
            }
        }
        // Orden estable (mezclado determinista).
        return resultado.OrderBy(_ => rnd.Next()).ToList();
    }

    private static bool Techo(Vector3 piso, out RaycastHit hit, float max = 12f)
    {
        return Physics.Raycast(piso + Vector3.up * 0.3f, Vector3.up, out hit, max, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y < -0.6f;
    }

    private static readonly Vector3[] Direcciones = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

    // ---------------- Director y entorno ----------------

    private static void ConfigurarEntornoGlobal()
    {
        // VolumenOscuridad (local, exposicion -1 / contraste +45) oscurecia casi todo el nivel: se apaga solo
        // su componente Volume (el objeto sigue, con su NavMeshModifier).
        var oscuridad = GameObject.Find("/VolumenOscuridad");
        if (oscuridad != null && oscuridad.TryGetComponent<Volume>(out var vo) && vo.enabled)
        {
            Undo.RecordObject(vo, "Ambientacion");
            vo.enabled = false;
        }

        // Volumen global existente -> perfil base.
        var baseVol = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilBase);
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (v.isGlobal && v.transform.root.name != NombreRaiz && baseVol != null)
            {
                Undo.RecordObject(v, "Ambientacion");
                v.sharedProfile = baseVol;
                v.priority = 0f;
                v.weight = 1f;
            }

        // Sin cielo: interior cerrado, el fondo se pierde en la niebla.
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = Niebla;
        RenderSettings.fogDensity = DensidadNiebla;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Ambiente;
        var cubo = AssetDatabase.LoadAssetAtPath<Cubemap>($"{CarpetaTexGen}/CM_ReflejoInterior.cubemap");
        if (cubo != null)
        {
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cubo;
            RenderSettings.reflectionIntensity = 1f;
        }

        // Es un interior: el sol solo debe colarse muy tenue.
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.transform.root.name != NombreRaiz)
            {
                Undo.RecordObject(l, "Ambientacion");
                l.intensity = 0.15f;
                l.color = new Color(0.75f, 0.82f, 1f);
            }
    }

    private static void AplicarMaterialAgua()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{CarpetaMat}/M_AguaInundacion.mat");
        if (mat == null) return;
        const string ruta = "Assets/Prefabs/FloodWater.prefab";
        var raiz = PrefabUtility.LoadPrefabContents(ruta);
        foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>())
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
        // Instancias de escena con override de material: tambien.
        foreach (var a in Object.FindObjectsByType<FloodWater>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var r = a.GetComponent<MeshRenderer>();
            if (r == null || r.sharedMaterial == mat) continue;
            Undo.RecordObject(r, "Ambientacion");
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    // ---------------- Luminarias ----------------

    private static int ColocarLuminarias(Transform padre, List<Vector3> puntos, int nivel, System.Random rnd, HashSet<Vector3Int> ocupado, ref int sombras)
    {
        var prefab = Prefab("Luminaria");
        if (prefab == null) { Debug.LogError("[Ambientacion] Falta el prefab Luminaria: corre Tools/Ambientacion/1."); return 0; }

        var existentes = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.transform.root.name != NombreRaiz).Select(l => l.transform.position).ToList();
        var g = new GameObject("Luminarias").transform;
        g.SetParent(padre, false);
        var falla = FallasZona[nivel];
        var perfil = PerfilesZona[nivel];
        int n = 0;
        const float separacion = 5.5f;

        foreach (var p in puntos)
        {
            if (!Techo(p, out var hit) || hit.distance < 2.2f) continue;
            var celda = Vector3Int.FloorToInt(hit.point / separacion);
            if (!ocupado.Add(celda)) continue;
            if (existentes.Any(e => (e - hit.point).sqrMagnitude < 2.5f * 2.5f)) continue;

            float r = (float)rnd.NextDouble();
            var patron = r < falla.rota ? LuzFluorescente.Patron.Rota
                : r < falla.rota + falla.muere ? LuzFluorescente.Patron.Muriendo
                : r < falla.rota + falla.muere + falla.inter ? LuzFluorescente.Patron.Intermitente
                : LuzFluorescente.Patron.Estable;

            // Orientacion: el tubo a lo largo del pasillo (eje con mas espacio libre).
            float libreX = Libre(hit.point + Vector3.down * 0.4f, Vector3.right) + Libre(hit.point + Vector3.down * 0.4f, Vector3.left);
            float libreZ = Libre(hit.point + Vector3.down * 0.4f, Vector3.forward) + Libre(hit.point + Vector3.down * 0.4f, Vector3.back);
            var rot = Quaternion.Euler(0f, libreX >= libreZ ? 0f : 90f, 0f);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g);
            go.transform.SetPositionAndRotation(hit.point, rot);

            var luz = go.GetComponentInChildren<Light>();
            luz.color = perfil.luz;
            luz.intensity = perfil.intensidad * Mathf.Lerp(0.85f, 1.1f, (float)rnd.NextDouble());
            luz.range = Mathf.Clamp(hit.distance * 3f, 8f, 14f);
            // Pocas luces con sombra (el atlas de sombras adicionales es limitado): las que fallan dan mas drama.
            if (sombras < 20 && (patron != LuzFluorescente.Patron.Estable || rnd.NextDouble() < 0.06))
            {
                luz.shadows = LightShadows.Soft;
                luz.shadowStrength = 0.85f;
                sombras++;
            }
            var lf = go.GetComponent<LuzFluorescente>();
            var so = new SerializedObject(lf);
            so.FindProperty("patron").enumValueIndex = (int)patron;
            so.FindProperty("colorEmision").colorValue = perfil.luz;
            // En calidad baja queda 1 de cada 3 luces reales; el resto solo emision.
            so.FindProperty("prescindibleEnCalidadBaja").boolValue = n % 3 != 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            lf.AplicarAspectoPatron(); // rota: chispas y carcasa caida, visibles ya en el editor
            n++;
        }
        return n;
    }

    private static float Libre(Vector3 desde, Vector3 dir, float max = 15f)
        => Physics.Raycast(desde, dir, out var h, max, ~0, QueryTriggerInteraction.Ignore) ? h.distance : max;

    private static bool Pared(Vector3 desde, System.Random rnd, float max, out RaycastHit hit)
    {
        var dir = Direcciones[rnd.Next(4)];
        dir = Quaternion.Euler(0f, (float)rnd.NextDouble() * 30f - 15f, 0f) * dir;
        return Physics.Raycast(desde, dir, out hit, max, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.normal.y) < 0.3f;
    }

    // ---------------- Rejillas / niebla ----------------

    private static bool SobreAgua(Zona zona, Vector3 p)
    {
        foreach (var a in zona.aguas)
        {
            var b = a.GetComponent<Collider>().bounds;
            if (p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z) return true;
        }
        return false;
    }

    // Rejillas de ventilacion con vapor, en paredes cerca del techo (mas en zonas degradadas).
    private static int ColocarRejillas(Transform padre, List<Vector3> puntos, int nivel, System.Random rnd)
    {
        var rejilla = Prefab("Rejilla");
        if (rejilla == null) return 0;
        var g = new GameObject("Rejillas").transform;
        g.SetParent(padre, false);
        int objetivo = 1 + nivel, n = 0;
        foreach (var p in puntos)
        {
            if (n >= objetivo) break;
            if (rnd.NextDouble() > 0.3) continue;
            if (!Techo(p, out var techo) || techo.distance < 2.6f) continue;
            float altura = Mathf.Min(techo.distance - 0.4f, 2.4f);
            if (!Pared(p + Vector3.up * altura, rnd, 3f, out var hit)) continue;
            var r = (GameObject)PrefabUtility.InstantiatePrefab(rejilla, g);
            r.transform.SetPositionAndRotation(hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal, Vector3.up));
            n++;
        }
        return n;
    }

    private static void ColocarNieblaAgua(Transform padre, Zona zona)
    {
        var prefab = Prefab("NieblaAgua");
        if (prefab == null) return;
        foreach (var a in zona.aguas)
        {
            var b = a.GetComponent<Collider>().bounds;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = "NieblaAgua_" + a.name;
            go.transform.position = new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
            var seg = go.GetComponent<SeguirSuperficieAgua>();
            var so = new SerializedObject(seg);
            so.FindProperty("agua").objectReferenceValue = a;
            so.ApplyModifiedPropertiesWithoutUndo();
            var ps = go.GetComponentInChildren<ParticleSystem>();
            var sh = ps.shape; sh.scale = new Vector3(b.size.x * 0.9f, 0.4f, b.size.z * 0.9f);
            var e = ps.emission; e.rateOverTime = Mathf.Clamp(b.size.x * b.size.z / 40f, 2f, 14f);
            var m = ps.main; m.maxParticles = Mathf.RoundToInt(e.rateOverTime.constant * 16f);
        }
    }

    // ---------------- Decals ----------------

    private static int ColocarDecals(Transform padre, List<Vector3> puntos, Zona zona, int nivel, System.Random rnd)
    {
        var humedad = Mat("M_Decal_Humedad");
        var linea = Mat("M_Decal_LineaAgua");
        var moho = Mat("M_Decal_Moho");
        var charco = Mat("M_Decal_Charco");
        if (humedad == null) return 0;
        var g = new GameObject("Decals").transform;
        g.SetParent(padre, false);
        int n = 0;

        DecalProjector Decal(Material m, Vector3 pos, Quaternion rot, Vector3 tam)
        {
            var go = new GameObject("Decal_" + m.name.Replace("M_Decal_", ""));
            go.transform.SetParent(g, false);
            go.transform.SetPositionAndRotation(pos, rot);
            var d = go.AddComponent<DecalProjector>();
            d.material = m;
            d.size = tam;
            d.pivot = Vector3.zero;
            d.fadeFactor = 1f;
            d.drawDistance = 45f;
            n++;
            return d;
        }

        float F() => (float)rnd.NextDouble();
        int paredes = 4 + nivel * 7, techos = 2 + nivel * 4, pisos = 2 + nivel * 3, mohos = nivel * 3, lineas = zona.aguas.Count * (3 + nivel * 2);
        foreach (var p in puntos)
        {
            if (paredes <= 0 && techos <= 0 && pisos <= 0 && mohos <= 0 && lineas <= 0) break;
            float r = F();
            if (paredes > 0 && r < 0.35f && Pared(p + Vector3.up * Mathf.Lerp(0.6f, 2.6f, F()), rnd, 4f, out var w))
            {
                float s = Mathf.Lerp(1f, 2.8f, F());
                Decal(humedad, w.point + w.normal * 0.25f, Quaternion.LookRotation(-w.normal) * Quaternion.Euler(0, 0, (F() - 0.5f) * 20f), new Vector3(s, s * 1.3f, 0.6f));
                paredes--;
            }
            else if (mohos > 0 && r < 0.45f && Pared(p + Vector3.up * Mathf.Lerp(0.1f, 0.8f, F()), rnd, 4f, out var mh))
            {
                float s = Mathf.Lerp(0.6f, 1.6f, F());
                Decal(moho, mh.point + mh.normal * 0.2f, Quaternion.LookRotation(-mh.normal) * Quaternion.Euler(0, 0, F() * 360f), new Vector3(s, s, 0.5f));
                mohos--;
            }
            else if (techos > 0 && r < 0.6f && Techo(p, out var t))
            {
                float s = Mathf.Lerp(1.2f, 3f, F());
                Decal(humedad, t.point + Vector3.down * 0.25f, Quaternion.LookRotation(Vector3.up) * Quaternion.Euler(0, 0, F() * 360f), new Vector3(s, s, 0.6f));
                techos--;
            }
            else if (pisos > 0 && r < 0.75f)
            {
                float s = Mathf.Lerp(0.8f, 2.2f, F());
                Decal(charco, p + Vector3.up * 0.25f, Quaternion.LookRotation(Vector3.down) * Quaternion.Euler(0, 0, F() * 360f), new Vector3(s, s * 0.7f, 0.6f));
                pisos--;
            }
            else if (lineas > 0 && SobreAgua(zona, p) && Pared(p + Vector3.up * Mathf.Lerp(0.5f, 1.4f, F()), rnd, 5f, out var l))
            {
                Decal(linea, l.point + l.normal * 0.2f, Quaternion.LookRotation(-l.normal), new Vector3(Mathf.Lerp(2.5f, 4.5f, F()), 1f, 0.5f));
                lineas--;
            }
        }
        return n;
    }

    // ---------------- Props (modelos de Blender) ----------------


    private static int ColocarProps(Transform padre, List<Vector3> puntos, Zona zona, int nivel, System.Random rnd)
    {
        var g = new GameObject("Props").transform;
        g.SetParent(padre, false);
        int n = 0;
        float F() => (float)rnd.NextDouble();

        GameObject Poner(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g);
            go.transform.SetPositionAndRotation(pos, rot);
            n++;
            return go;
        }

        // Tuberias por el techo, pegadas a una pared (a veces con codo hacia el techo).
        var tuberia = Prefab("Tuberia");
        int canos = 6 + nivel * 3;
        if (tuberia != null)
            foreach (var p in puntos)
            {
                if (canos <= 0) break;
                if (F() > 0.25f || !Techo(p, out var t) || t.distance < 2.6f) continue;
                Vector3 alto = t.point + Vector3.down * 0.25f;
                foreach (var d in Direcciones.OrderBy(_ => rnd.Next()))
                {
                    if (!Physics.Raycast(alto, d, out var w, 2f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(w.normal.y) > 0.3f) continue;
                    Vector3 tangente = Vector3.Cross(Vector3.up, w.normal).normalized;
                    var pos = w.point + w.normal * 0.18f;
                    // Largo disponible a lo largo de la pared (el modelo mide 4 m en X).
                    float libre = Mathf.Min(Libre(pos, tangente, 8f), Libre(pos, -tangente, 8f));
                    if (libre < 2f) break;
                    // Modelos de Blender: frente (+Z) hacia afuera de la pared, largo en X.
                    var tubo = Poner(tuberia, pos, Quaternion.LookRotation(w.normal));
                    var codo = tubo.transform.Find("Modelo/Codo");
                    if (codo != null) codo.gameObject.SetActive(F() < 0.4f);
                    canos--;
                    break;
                }
            }

        // Cables colgando del techo (mas en zonas degradadas).
        var cable = Prefab("Cable");
        int cables = 1 + nivel * 2;
        if (cable != null)
            foreach (var p in puntos.OrderBy(_ => rnd.Next()))
            {
                if (cables <= 0) break;
                if (!Techo(p, out var t) || t.distance < 3f) continue;
                Poner(cable, t.point, Quaternion.Euler(0, F() * 360f, 0));
                cables--;
            }

        // Placas de techo caidas en el piso (zonas 3+).
        var placa = Prefab("PlacaTecho");
        int placas = Mathf.Max(0, nivel - 1) * 3;
        if (placa != null)
            foreach (var p in puntos.OrderBy(_ => rnd.Next()))
            {
                if (placas <= 0) break;
                Poner(placa, p + Vector3.up * 0.02f, Quaternion.Euler(F() * 8f, F() * 360f, F() * 8f));
                placas--;
            }

        // Escombros en el area que se inunda: flotan cuando sube el agua.
        var escombros = Escombros.Select(Prefab).Where(x => x != null).ToArray();
        if (escombros.Length > 0)
            foreach (var a in zona.aguas)
            {
                var b = a.GetComponent<Collider>().bounds;
                int cantidad = Mathf.Clamp(Mathf.RoundToInt(b.size.x * b.size.z / 25f), 3, 14);
                foreach (var p in puntos)
                {
                    if (cantidad <= 0) break;
                    if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z) continue;
                    if (F() > 0.5f) continue;
                    Poner(escombros[rnd.Next(escombros.Length)], p + Vector3.up * 0.01f, Quaternion.Euler(0, F() * 360f, 0));
                    cantidad--;
                }
            }

        return n;
    }

    // Carteles de SALIDA sobre los cierres automaticos (puertas entre zonas).
    private static int ColocarCarteles(Transform raiz)
    {
        var salida = Prefab("CartelSalida");
        if (salida == null) return 0;
        var g = new GameObject("CartelesSalida").transform;
        g.SetParent(raiz, false);
        int n = 0;
        foreach (var t in Object.FindObjectsByType<TriggerCierrePuerta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var col = t.GetComponent<Collider>();
            if (col == null) continue;
            var b = col.bounds;
            // Eje fino del trigger = direccion de paso por la puerta.
            Vector3 normal = b.size.x < b.size.z ? Vector3.right : Vector3.forward;
            Vector3 centro = b.center;
            if (!Physics.Raycast(centro, Vector3.up, out var techo, 8f, ~0, QueryTriggerInteraction.Ignore)) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(salida, g);
            go.transform.SetPositionAndRotation(techo.point + Vector3.down * 0.25f, Quaternion.LookRotation(normal));
            n++;
        }
        return n;
    }

    // ---------------- Materiales por zona ----------------

    // Clasifica un material (original o ya reemplazado) en pared / piso / techo.
    private static string Categoria(Material m)
    {
        if (m == null) return null;
        string n = m.name;
        if (n == "Pared-Laboratorio" || n.EndsWith("_Pared")) return "Pared";
        if (n == "Piso-Laboratorio" || n.EndsWith("_Piso")) return "Piso";
        if (n == "Techo" || n.EndsWith("_Techo")) return "Techo";
        return null;
    }

    private static int AplicarMaterialesZona(List<Zona> zonas)
    {
        var originales = new Dictionary<string, Material>
        {
            ["Pared"] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Concrete_017_SD/Pared-Laboratorio.mat"),
            ["Piso"] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/2K/Piso-Laboratorio.mat"),
            ["Techo"] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/tiling_72-1K/Techo.mat"),
        };
        Material Para(string cat, int zona)
        {
            var m = Mat($"M_Z{zona + 1}_{cat}");
            return m != null ? m : originales[cat];
        }

        int n = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r.transform.root.name == NombreRaiz) continue;
            var mats = r.sharedMaterials;
            bool cambio = false;
            int zona = -1;
            for (int i = 0; i < mats.Length; i++)
            {
                string cat = Categoria(mats[i]);
                if (cat == null) continue;
                if (zona < 0) zona = Mathf.Min(ZonaDe(zonas, r.bounds.center), 4);
                var nuevo = Para(cat, zona);
                if (nuevo != null && mats[i] != nuevo) { mats[i] = nuevo; cambio = true; }
            }
            if (!cambio) continue;
            Undo.RecordObject(r, "Ambientacion");
            r.sharedMaterials = mats;
            n++;
        }
        return n;
    }

    // ======================================================================
    // Todo en batchmode
    // ======================================================================

    public static void TodoBatch()
    {
        GenerarAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/MaliciousComplex.unity");
        PoblarEscena();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }
}
