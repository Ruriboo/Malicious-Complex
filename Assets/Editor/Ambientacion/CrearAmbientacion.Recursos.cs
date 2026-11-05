using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Herramienta de ambientacion (laboratorio -> backrooms). Menu:
//   Tools/Ambientacion/1. Generar assets         texturas, materiales, perfil de post-proceso y prefabs
//   Tools/Ambientacion/2. Poblar escena abierta  coloca todo en la escena bajo el objeto "Ambientacion"
// Parte 1 (este archivo): rutas, texturas, materiales y render pipeline.
// Parte 2 (CrearAmbientacion.Prefabs.cs): prefabs.  Parte 3 (CrearAmbientacion.Escena.cs): escena.
public static partial class CrearAmbientacion
{
    // Donde vive cada cosa (carpetas compartidas del proyecto).
    public const string CarpetaPrefabs = "Assets/Prefabs/Ambientacion";
    public const string CarpetaMat = "Assets/Materials/Ambientacion";
    public const string CarpetaTex = "Assets/Textures/Ambientacion";
    public const string CarpetaTexGen = CarpetaTex + "/Generadas";
    public const string CarpetaCC0 = CarpetaTex + "/CC0";
    public const string CarpetaHorneadas = CarpetaTex + "/Horneadas";
    public const string CarpetaModelos = "Assets/assets_models/Ambientacion";
    public const string CarpetaAudio = "Assets/Audio/SFX/Luces";
    public const string RutaPerfilBase = "Assets/Settings/VP_Base.asset";

    [MenuItem("Tools/Ambientacion/1. Generar assets")]
    public static void GenerarAssets()
    {
        CrearTexturasYMateriales();
        CrearPerfilYPipeline();
        CrearPrefabs();
        Debug.Log("[Ambientacion] Assets generados.");
    }

    // ======================================================================
    // Texturas y materiales
    // ======================================================================

    private static void CrearTexturasYMateriales()
    {
        CrearCarpeta(CarpetaTexGen);
        CrearCarpeta(CarpetaMat);

        // --- Texturas de particulas ---
        GuardarTextura("T_Humo", 128, 128, (u, v) =>
        {
            float d = Radial(u, v);
            float n = Fbm(u * 4f, v * 4f, 4);
            float a = Mathf.Clamp01(1f - d * 1.15f) * Mathf.Lerp(0.35f, 1f, n);
            return new Color(1, 1, 1, a * a);
        });
        GuardarTextura("T_Chispa", 16, 64, (u, v) =>
        {
            float x = Mathf.Abs(u - 0.5f) * 2f;
            float a = Mathf.Clamp01(1f - x) * Mathf.Clamp01(1f - Mathf.Abs(v - 0.5f) * 2f);
            return new Color(1f, 0.9f, 0.6f, a * a);
        });
        GuardarTextura("T_Onda", 128, 128, (u, v) =>
        {
            float d = Radial(u, v);
            float anillo = Mathf.Exp(-Mathf.Pow((d - 0.8f) * 9f, 2f)) + 0.35f * Mathf.Exp(-Mathf.Pow((d - 0.55f) * 12f, 2f));
            return new Color(1, 1, 1, Mathf.Clamp01(anillo) * Mathf.Clamp01((1f - d) * 6f));
        });

        // --- Texturas de decals (con alpha) ---
        GuardarTextura("T_Decal_Humedad", 512, 512, (u, v) =>
        {
            float d = Radial(u, v);
            float n = Fbm(u * 3f + 11f, v * 3f + 7f, 5);
            float forma = Mathf.Clamp01((0.85f - d * 0.9f + (n - 0.5f) * 0.6f) * 3f);
            float borde = Mathf.Clamp01(1f - Mathf.Abs(forma - 0.5f) * 2.4f);
            float tono = Mathf.Lerp(0.55f, 0.25f, borde) * Mathf.Lerp(0.85f, 1.1f, Fbm(u * 18f, v * 18f, 3));
            // Escurrimiento hacia abajo.
            float chorreo = Mathf.Clamp01(Fbm(u * 22f, 3f, 2) * 2.2f - 1.1f) * Mathf.Clamp01(1f - v * 1.6f) * Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 2.5f);
            float a = Mathf.Max(forma * 0.75f, chorreo * 0.6f) * Mathf.Clamp01(1f - d * 0.95f + 0.3f);
            return new Color(0.36f * tono * 2f, 0.31f * tono * 2f, 0.2f * tono * 2f, a);
        });
        GuardarTextura("T_Decal_LineaAgua", 512, 128, (u, v) =>
        {
            float n = Fbm(u * 12f, v * 2f, 4);
            float linea = Mathf.Exp(-Mathf.Pow((v - 0.62f - (n - 0.5f) * 0.12f) * 18f, 2f));
            float abajo = Mathf.Clamp01(0.62f - v) * 1.3f * Mathf.Lerp(0.6f, 1f, Fbm(u * 30f, v * 6f, 3));
            float bordes = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 8f);
            float a = Mathf.Clamp01(linea * 0.85f + abajo * 0.45f) * bordes;
            return new Color(0.3f, 0.27f, 0.17f, a);
        });
        GuardarTextura("T_Decal_Moho", 256, 256, (u, v) =>
        {
            float d = Radial(u, v);
            float puntos = Mathf.Clamp01(Fbm(u * 25f, v * 25f, 3) * 3f - 1.5f);
            float masa = Mathf.Clamp01((Fbm(u * 4f + 3f, v * 4f, 4) - 0.35f) * 3f) * Mathf.Clamp01(1f - d);
            float a = Mathf.Clamp01(masa * 0.8f + puntos * masa);
            return new Color(0.12f, 0.16f, 0.08f, a);
        });
        GuardarTextura("T_Decal_Charco", 256, 256, (u, v) =>
        {
            float d = Radial(u, v);
            float n = Fbm(u * 3f + 5f, v * 3f, 4);
            float a = Mathf.Clamp01((0.8f - d + (n - 0.5f) * 0.5f) * 6f);
            return new Color(0.05f, 0.05f, 0.04f, a * 0.55f);
        });

        AssetDatabase.Refresh();
        ConfigurarImportParticulas();

        // --- Materiales ---
        MaterialParticula("M_Particula_Humo", "T_Humo", false, new Color(0.85f, 0.87f, 0.83f, 0.55f), true);
        MaterialParticula("M_Particula_Niebla", "T_Humo", false, new Color(0.55f, 0.6f, 0.5f, 0.22f), true);
        MaterialParticula("M_Particula_Chispa", "T_Chispa", true, new Color(1f, 0.75f, 0.35f, 1f), false);
        MaterialParticula("M_Particula_Burbuja", "T_Onda", false, new Color(0.8f, 0.95f, 0.9f, 0.6f), false);

        MaterialDecal("M_Decal_Humedad", "T_Decal_Humedad");
        MaterialDecal("M_Decal_LineaAgua", "T_Decal_LineaAgua");
        MaterialDecal("M_Decal_Moho", "T_Decal_Moho");
        MaterialDecal("M_Decal_Charco", "T_Decal_Charco");

        // Agua y pantalla

        // Luminaria: difusor emisivo y carcasa
        var lente = ObtenerMaterial("M_Luminaria_Difusor", Shader.Find("Universal Render Pipeline/Lit"));
        lente.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.9f));
        lente.EnableKeyword("_EMISSION");
        lente.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.8f) * 3f);
        lente.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        lente.SetFloat("_Smoothness", 0.3f);
        EditorUtility.SetDirty(lente);
        var carcasa = ObtenerMaterial("M_Luminaria_Carcasa", Shader.Find("Universal Render Pipeline/Lit"));
        carcasa.SetColor("_BaseColor", new Color(0.72f, 0.71f, 0.66f));
        carcasa.SetFloat("_Metallic", 0.6f);
        carcasa.SetFloat("_Smoothness", 0.35f);
        EditorUtility.SetDirty(carcasa);

        CrearReflejoAmbiente();
        ConfigurarImportPBR();
        CrearMaterialAgua();
        CrearMaterialesZona();
        AssetDatabase.SaveAssets();
        Debug.Log("[Ambientacion] Texturas y materiales listos.");
    }

    // ---------------- Texturas PBR (CC0 + horneadas en Blender) ----------------


    private static void ConfigurarImportPBR()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaCC0, CarpetaHorneadas }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(ruta) is not TextureImporter imp) continue;
            string n = System.IO.Path.GetFileNameWithoutExtension(ruta);
            bool normal = n.EndsWith("NormalGL");
            bool lineal = n.EndsWith("_MS") || n.EndsWith("AmbientOcclusion") || n.EndsWith("Roughness");
            bool conAlfa = n.EndsWith("_MS") || n == "Agua_Color"; // MS: suavidad en A; agua: transparencia en A
            var tipo = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            var web = imp.GetPlatformTextureSettings("WebGL");
            var alfa = conAlfa ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            bool ok = imp.textureType == tipo && imp.textureShape == TextureImporterShape.Texture2D && imp.alphaSource == alfa
                      && imp.sRGBTexture == !(lineal || normal) && imp.maxTextureSize == 2048 && web.overridden && web.maxTextureSize == 1024;
            if (ok) continue;
            imp.textureType = tipo;
            imp.textureShape = TextureImporterShape.Texture2D;
            imp.sRGBTexture = !(lineal || normal);
            imp.maxTextureSize = 2048;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 4;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.alphaSource = alfa;
            imp.alphaIsTransparency = n == "Agua_Color";
            web.overridden = true;
            web.maxTextureSize = 1024;
            imp.SetPlatformTextureSettings(web);
            imp.SaveAndReimport();
        }
    }

    private static Texture2D Tex(string ruta) => AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    private static string Cc0(string id, string mapa) => $"{CarpetaCC0}/{id}/{id}_2K-JPG_{mapa}.jpg";
    private static string Hor(string nombre) => $"{CarpetaHorneadas}/{nombre}.png";

    private static Material MaterialPBR(string nombre, string color, string normal, string ms, string ao, Color tinte, float tiling, float suavidad = 1f, float fuerzaNormal = 1f)
    {
        var m = ObtenerMaterial(nombre, Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", Tex(color));
        m.SetColor("_BaseColor", tinte);
        var esc = new Vector2(tiling, tiling);
        m.SetTextureScale("_BaseMap", esc);
        if (normal != null && Tex(normal) != null)
        {
            m.SetTexture("_BumpMap", Tex(normal));
            m.SetFloat("_BumpScale", fuerzaNormal);
            m.EnableKeyword("_NORMALMAP");
        }
        if (ms != null && Tex(ms) != null)
        {
            m.SetTexture("_MetallicGlossMap", Tex(ms));
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", suavidad);
        }
        else m.SetFloat("_Smoothness", 0.2f * suavidad);
        if (ao != null && Tex(ao) != null)
        {
            m.SetTexture("_OcclusionMap", Tex(ao));
            m.SetFloat("_OcclusionStrength", 1f);
            m.EnableKeyword("_OCCLUSIONMAP");
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // Agua: URP Lit transparente, estatica, con una sola textura (Horneadas/Agua_Color, alfa incluida).
    private static void CrearMaterialAgua()
    {
        var m = ObtenerMaterial("M_AguaInundacion", Shader.Find("Universal Render Pipeline/Lit"));
        var tex = Tex(Hor("Agua_Color"));
        if (tex == null) Debug.LogError($"[Ambientacion] Falta {Hor("Agua_Color")} (corre SourceArt/Ambientacion/hornear_texturas.py).");
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", new Vector2(0.25f, 0.25f)); // un tile cada 4 m
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", 0.92f); // refleja las luces de los tubos
        m.SetFloat("_Surface", 1f);       // transparente
        m.SetFloat("_Blend", 0f);         // alpha
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", (float)CullMode.Off); // tambien se ve desde abajo del agua
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
    }

    // Materiales por zona: 1-2 conservan el laboratorio original (salvo el techo),
    // 3 es transicion (yeso + azulejo sucio), 4-5 son backrooms (papel amarillo + alfombra humeda).
    private static void CrearMaterialesZona()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaCC0)) { Debug.LogWarning("[Ambientacion] Sin texturas CC0: se omiten materiales de zona."); return; }
        const string techo = "OfficeCeiling001";
        Color[] tinteTecho = { new(1f, 1f, 1f), new(0.97f, 0.97f, 0.95f), new(0.93f, 0.9f, 0.8f), new(0.88f, 0.82f, 0.64f), new(0.8f, 0.73f, 0.54f) };
        for (int z = 0; z < 5; z++)
            MaterialPBR($"M_Z{z + 1}_Techo", Cc0(techo, "Color"), Cc0(techo, "NormalGL"), Hor(techo + "_MS"), Cc0(techo, "AmbientOcclusion"), tinteTecho[z], 1f / 3.6f, 0.9f);

        MaterialPBR("M_Z3_Pared", Cc0("PaintedPlaster017", "Color"), Cc0("PaintedPlaster017", "NormalGL"), Hor("PaintedPlaster017_MS"), null, new Color(0.93f, 0.9f, 0.8f), 0.4f);
        MaterialPBR("M_Z3_Piso", Cc0("Tiles133B", "Color"), Cc0("Tiles133B", "NormalGL"), Hor("Tiles133B_MS"), Cc0("Tiles133B", "AmbientOcclusion"), new Color(0.82f, 0.82f, 0.76f), 0.5f);
        MaterialPBR("M_Z4_Pared", Hor("Papel_Backrooms_Color"), Cc0("Wallpaper001A", "NormalGL"), Hor("Wallpaper001A_MS"), null, Color.white, 0.5f, 0.8f);
        MaterialPBR("M_Z4_Piso", Hor("Alfombra_Humeda_Color"), Cc0("Fabric028", "NormalGL"), Hor("Alfombra_Humeda_MS"), null, Color.white, 0.5f);
        MaterialPBR("M_Z5_Pared", Hor("Papel_Moho_Color"), Cc0("Wallpaper002B", "NormalGL"), Hor("Wallpaper002B_MS"), null, new Color(0.95f, 0.93f, 0.88f), 0.5f, 0.8f);
        MaterialPBR("M_Z5_Piso", Hor("Alfombra_Humeda_Color"), Cc0("Fabric028", "NormalGL"), Hor("Alfombra_Humeda_MS"), null, new Color(0.85f, 0.8f, 0.7f), 0.5f, 1.35f);

        // Materiales de props texturizados (los usa MaterialProp por nombre).
        var oxido = MaterialPBR("MP_Metal_Oxido", Cc0("Metal021", "Color"), Cc0("Metal021", "NormalGL"), Hor("Metal021_MS"), null, new Color(0.8f, 0.78f, 0.74f), 1f);
        oxido.SetTextureScale("_BaseMap", new Vector2(4f, 1f));
        MaterialPBR("MP_PlacaTecho", Cc0(techo, "Color"), Cc0(techo, "NormalGL"), Hor(techo + "_MS"), Cc0(techo, "AmbientOcclusion"), tinteTecho[3], 1f / 3.6f * 0.6f, 0.9f);
    }

    // Cubemap oscuro y calido para reflejos por defecto (evita que todo refleje el skybox azul en interiores).
    private static void CrearReflejoAmbiente()
    {
        string ruta = CarpetaTexGen + "/CM_ReflejoInterior.cubemap";
        if (AssetDatabase.LoadAssetAtPath<Cubemap>(ruta) != null) return;
        const int n = 32;
        var cm = new Cubemap(n, TextureFormat.RGBA32, false);
        for (int cara = 0; cara < 6; cara++)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float v = (float)y / (n - 1);
                Color c;
                if (cara == (int)CubemapFace.PositiveY) c = new Color(0.22f, 0.21f, 0.17f) * (1f + 0.6f * Mathf.Abs(Mathf.Sin(x * 0.6f)));   // techo con luces
                else if (cara == (int)CubemapFace.NegativeY) c = new Color(0.02f, 0.02f, 0.02f);
                else c = Color.Lerp(new Color(0.12f, 0.11f, 0.08f), new Color(0.04f, 0.04f, 0.035f), v);
                px[y * n + x] = c;
            }
            cm.SetPixels(px, (CubemapFace)cara);
        }
        cm.Apply();
        AssetDatabase.CreateAsset(cm, ruta);
    }

    private static void ConfigurarImportParticulas()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaTexGen }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(ruta) is not TextureImporter imp) continue;
            bool cambio = !imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            var web = imp.GetPlatformTextureSettings("WebGL");
            if (!web.overridden || web.maxTextureSize != 256)
            {
                web.overridden = true;
                web.maxTextureSize = 256;
                imp.SetPlatformTextureSettings(web);
                cambio = true;
            }
            if (cambio) imp.SaveAndReimport();
        }
    }

    private static Material MaterialParticula(string nombre, string textura, bool aditivo, Color color, bool suave)
    {
        var m = ObtenerMaterial(nombre, Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{CarpetaTexGen}/{textura}.png"));
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", aditivo ? 2f : 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAMODULATE_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if (suave)
        {
            m.SetFloat("_SoftParticlesEnabled", 1f);
            m.SetFloat("_SoftParticlesNearFadeDistance", 0f);
            m.SetFloat("_SoftParticlesFarFadeDistance", 1.5f);
            m.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f / 1.5f, 0f, 0f));
            m.EnableKeyword("_SOFTPARTICLES_ON");
            // Fundido cerca de la camara (polvo/niebla que pasa delante)
            m.SetFloat("_CameraFadingEnabled", 1f);
            m.SetFloat("_CameraNearFadeDistance", 0.3f);
            m.SetFloat("_CameraFarFadeDistance", 1.2f);
            m.SetVector("_CameraFadeParams", new Vector4(0.3f, 1f / 0.9f, 0f, 0f));
            m.EnableKeyword("_FADING_ON");
        }
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material MaterialDecal(string nombre, string textura)
    {
        var m = ObtenerMaterial(nombre, Shader.Find("Shader Graphs/Decal"));
        m.SetTexture("Base_Map", AssetDatabase.LoadAssetAtPath<Texture2D>($"{CarpetaTexGen}/{textura}.png"));
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Material ObtenerMaterial(string nombre, Shader shader)
    {
        string ruta = $"{CarpetaMat}/{nombre}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            if (shader == null) { Debug.LogError($"[Ambientacion] Shader faltante para {nombre}"); shader = Shader.Find("Universal Render Pipeline/Lit"); }
            m = new Material(shader) { name = nombre };
            AssetDatabase.CreateAsset(m, ruta);
        }
        else if (shader != null && m.shader != shader) m.shader = shader;
        return m;
    }

    // ======================================================================
    // 3. Volumenes y render pipeline
    // ======================================================================

    // ======================================================================
    // Post-proceso y render pipeline
    // ======================================================================

    private static void CrearPerfilYPipeline()
    {
        // Unico perfil: tonemapping (necesario con HDR) + grano. Lo usa el "Global Volume" de la escena.
        var vBase = NuevoPerfil("VP_Base");
        var tm = Comp<Tonemapping>(vBase); tm.mode.Override(TonemappingMode.Neutral);
        var fg = Comp<FilmGrain>(vBase); fg.type.Override(FilmGrainLookup.Medium3); fg.intensity.Override(0.25f); fg.response.Override(0.75f);
        Guardar(vBase);

        ConfigurarPipeline();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigurarPipeline()
    {
        foreach (string ruta in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
        {
            var datos = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ruta);
            if (datos == null) { Debug.LogError($"[Ambientacion] No se encontro {ruta}"); continue; }

            QuitarFeature(datos, "PantallaFX (Ambientacion)"); // efecto VHS de versiones anteriores
            var dec = BuscarOAgregarFeature<DecalRendererFeature>(datos, "Decals (Ambientacion)");
            var so = new SerializedObject(dec);
            var tecnica = so.FindProperty("m_Settings.technique");
            if (tecnica != null) tecnica.intValue = 2; // ScreenSpace: funciona en WebGL y no exige DBuffer
            var dist = so.FindProperty("m_Settings.maxDrawDistance");
            if (dist != null) dist.floatValue = 60f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }

    }

    private static void QuitarFeature(ScriptableRendererData datos, string nombre)
    {
        var so = new SerializedObject(datos);
        var lista = so.FindProperty("m_RendererFeatures");
        var mapa = so.FindProperty("m_RendererFeatureMap");
        for (int i = lista.arraySize - 1; i >= 0; i--)
        {
            var f = lista.GetArrayElementAtIndex(i).objectReferenceValue;
            if (f == null || f.name != nombre) continue;
            lista.DeleteArrayElementAtIndex(i);
            if (lista.arraySize > i && lista.GetArrayElementAtIndex(i).objectReferenceValue == f) lista.DeleteArrayElementAtIndex(i);
            mapa.DeleteArrayElementAtIndex(i);
            so.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(f, true);
            so.Update();
        }
        EditorUtility.SetDirty(datos);
    }

    private static T BuscarOAgregarFeature<T>(ScriptableRendererData datos, string nombre) where T : ScriptableRendererFeature
    {
        foreach (var f in datos.rendererFeatures)
            if (f is T t && (f.name == nombre || typeof(T) == typeof(DecalRendererFeature))) return t;

        var nueva = ScriptableObject.CreateInstance<T>();
        nueva.name = nombre;
        AssetDatabase.AddObjectToAsset(nueva, datos);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(nueva, out _, out long id);

        var so = new SerializedObject(datos);
        var lista = so.FindProperty("m_RendererFeatures");
        var mapa = so.FindProperty("m_RendererFeatureMap");
        lista.arraySize++;
        lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = nueva;
        mapa.arraySize++;
        mapa.GetArrayElementAtIndex(mapa.arraySize - 1).longValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
        return nueva;
    }

    private static VolumeProfile NuevoPerfil(string nombre)
    {
        string ruta = RutaPerfilBase;
        var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (p != null)
        {
            // Se regenera desde cero: borrar componentes previos (sub-assets).
            foreach (var c in p.components.ToArray()) Object.DestroyImmediate(c, true);
            p.components.Clear();
            return p;
        }
        p = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(p, ruta);
        return p;
    }

    private static T Comp<T>(VolumeProfile p) where T : VolumeComponent
    {
        var c = p.Add<T>(true);
        c.name = typeof(T).Name;
        c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(c, p);
        // Add<T>(true) marca todos los parametros como override: se desmarcan y cada uno se activa con Override().
        foreach (var param in c.parameters) param.overrideState = false;
        return c;
    }

    private static void Guardar(VolumeProfile p) => EditorUtility.SetDirty(p);

    // ======================================================================
    // Utilidades
    // ======================================================================

    public static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }

    private static float Radial(float u, float v) => Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f);

    private static float Fbm(float x, float y, int octavas)
    {
        float s = 0f, a = 0.5f, f = 1f;
        for (int i = 0; i < octavas; i++) { s += a * Mathf.PerlinNoise(x * f + i * 17.3f, y * f + i * 9.1f); f *= 2f; a *= 0.5f; }
        return s / (1f - Mathf.Pow(0.5f, octavas));
    }

    private static void GuardarTextura(string nombre, int ancho, int alto, System.Func<float, float, Color> f)
    {
        string ruta = $"{CarpetaTexGen}/{nombre}.png";
        if (File.Exists(ruta)) return;
        var t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        var px = new Color[ancho * alto];
        for (int y = 0; y < alto; y++)
        for (int x = 0; x < ancho; x++)
            px[y * ancho + x] = f((x + 0.5f) / ancho, (y + 0.5f) / alto);
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(ruta, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }
}
