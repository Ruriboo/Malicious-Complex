using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Renderiza PNGs desde puntos fijos de cada zona para revisar la ambientacion sin jugar.
// Uso: Tools/Ambientacion/Capturar vistas  (salida: Capturas/ en la raiz del proyecto, gitignoreable)
// Batchmode (sin -nographics): -executeMethod CapturasAmbientacion.CapturarBatch -capturas <carpeta> -estado <normal|bajoagua|monstruo|oxigeno|muerte>
public static class CapturasAmbientacion
{
    private const string Escena = "Assets/Scenes/MaliciousComplex.unity";
    private const int Ancho = 960, Alto = 540;

    [MenuItem("Tools/Ambientacion/Capturar vistas")]
    public static void CapturarMenu() => Capturar(Path.GetFullPath("Capturas"), "normal");

    public static void CapturarBatch()
    {
        string carpeta = Argumento("-capturas") ?? Path.GetFullPath("Capturas");
        string estado = Argumento("-estado") ?? "normal";
        EditorSceneManager.OpenScene(Escena);
        string filtro = Argumento("-vistas");
        foreach (var e in estado.Split(',')) Capturar(carpeta, e.Trim(), filtro);
    }

    private static string Argumento(string nombre)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == nombre) return args[i + 1];
        return null;
    }

    public static void Capturar(string carpeta, string estado, string filtro = null)
    {
        Directory.CreateDirectory(carpeta);
        var vistas = Vistas();

        var go = new GameObject("CamaraCaptura") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 300f;
        var datos = go.AddComponent<UniversalAdditionalCameraData>();
        datos.renderPostProcessing = Argumento("-sinpost") == null;
        datos.antialiasing = AntialiasingMode.FastApproximateAntialiasing;


        // "inundado": sube temporalmente el agua para ver el shader, la niebla y las goteras sobre ella.
        var aguas = new List<(Transform t, Vector3 p)>();
        if (estado == "inundado")
            foreach (var fw in Object.FindObjectsByType<FloodWater>(FindObjectsSortMode.None))
            {
                aguas.Add((fw.transform, fw.transform.position));
                fw.transform.position += Vector3.up * 1.4f;
            }

        var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
        cam.targetTexture = rt;

        foreach (var v in vistas)
        {
            if (filtro != null && System.Array.IndexOf(filtro.Split(','), v.nombre) < 0) continue;
            go.transform.SetPositionAndRotation(v.pos, Quaternion.LookRotation(v.mira - v.pos));
            LuzFluorescente.ActualizarCulling(v.pos, 40, 40f);

            if (Argumento("-debugluces") != null)
            {
                var luces = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                int activas = 0; string cerca = "";
                foreach (var l in luces)
                {
                    if (!l.isActiveAndEnabled) continue;
                    activas++;
                    float d = Vector3.Distance(l.transform.position, v.pos);
                    if (d < 8f) cerca += $" [{l.name} {l.type} d={d:F1} i={l.intensity:F1} r={l.range:F1} fwd={l.transform.forward:F1}]";
                }
                Debug.Log($"[Ambientacion] LUCES {v.nombre}: activas={activas} total={luces.Length} cerca:{cerca}");
            }
            // Particulas: en modo edicion no corren solas; se simulan unos segundos.
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                if (ps.transform.parent == null || ps.transform.parent.GetComponent<ParticleSystem>() == null)
                    ps.Simulate(6f, true, true, false);
            // Varios renders para que se estabilicen volumenes, TAA, etc.
            for (int i = 0; i < 3; i++) cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string archivo = Path.Combine(carpeta, $"{v.nombre}_{estado}.png");
            File.WriteAllBytes(archivo, tex.EncodeToPNG());
            Debug.Log($"[Ambientacion] Captura {archivo}");
        }

        foreach (var (t, p) in aguas) t.position = p;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }

    private static readonly string[] PrefabsAmbientacion =
        { "Luminaria", "Rejilla", "Tuberia", "Cable", "PlacaTecho", "EscombroCaja", "EscombroSilla", "EscombroBidon", "EscombroPapeles", "EscombroBotella", "CartelSalida", "NieblaAgua" };

    private struct Vista { public string nombre; public int zona; public Vector3 pos, mira; }

    // Puntos de vista: dos por zona, desde el punto de entrada mirando hacia su agua, mas el cuarto inicial.
    private static List<Vista> Vistas()
    {
        var lista = new List<Vista>();
        // -props: una vista frente a la primera instancia de cada prefab de prop / luminaria / VFX.
        if (Argumento("-props") != null)
        {
            var vistos = new HashSet<string>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (System.Array.IndexOf(PrefabsAmbientacion, t.name) < 0) continue;
                if (t.root.name != "Ambientacion" || !vistos.Add(t.name)) continue;
                var rs = t.GetComponentsInChildren<Renderer>();
                Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.one * 0.5f);
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float d = Mathf.Max(1.2f, b.extents.magnitude * 2.2f);
                bool techo = Physics.Raycast(t.position + Vector3.down * 0.3f, Vector3.up, 0.6f);
                Vector3 dir = t.forward; dir.y = 0; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
                Vector3 pos = b.center + dir.normalized * d + Vector3.up * (techo ? -1.2f : 0.6f);
                lista.Add(new Vista { nombre = "P_" + t.name, zona = 3, pos = pos, mira = b.center });
            }
            return lista;
        }
        var cm = Object.FindFirstObjectByType<CheckpointManager>();
        if (cm == null) return lista;
        var zonas = new SerializedObject(cm).FindProperty("zonasDelNivel");
        for (int i = 0; i < zonas.arraySize; i++)
        {
            var z = zonas.GetArrayElementAtIndex(i);
            var entrada = z.FindPropertyRelative("puntoEntrada").objectReferenceValue as Transform;
            if (entrada == null) continue;
            Vector3 p = entrada.position + Vector3.up * 1.2f;
            Vector3 centro = p + Vector3.forward;
            var aguas = z.FindPropertyRelative("aguas");
            if (aguas.arraySize > 0 && aguas.GetArrayElementAtIndex(0).objectReferenceValue is FloodWater a)
                centro = new Vector3(a.transform.position.x, p.y - 1f, a.transform.position.z);
            Vector3 dir = centro - p; dir.y = Mathf.Clamp(dir.y, -2f, 0f);
            if (dir.sqrMagnitude < 1f) dir = Vector3.forward;
            lista.Add(new Vista { nombre = $"Z{i + 1}_a", zona = i, pos = p, mira = p + dir });
            lista.Add(new Vista { nombre = $"Z{i + 1}_b", zona = i, pos = p, mira = p - new Vector3(dir.x, 0, dir.z) });
        }
        var jugador = GameObject.Find("Player/PlayerArmature") ?? GameObject.FindWithTag("Player");
        if (jugador != null)
        {
            Vector3 p = jugador.transform.position + Vector3.up * 1.6f;
            lista.Add(new Vista { nombre = "Z0_inicio", zona = 0, pos = p, mira = p + jugador.transform.forward });
            lista.Add(new Vista { nombre = "Z0_inicio_b", zona = 0, pos = p, mira = p - jugador.transform.forward });
        }
        return lista;
    }
}
