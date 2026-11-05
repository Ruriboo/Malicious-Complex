using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Herramientas para generar los enemigos a partir de los FBX de Assets/enemy_models
// (exportados desde los .blend de SourceArt/) y preparar la NavMesh de la escena.
//   Tools/Enemigos/1. Crear prefabs de enemigos
//   Tools/Enemigos/2. Preparar y hornear NavMesh (escena abierta)
public static class CrearEnemigos
{
    private const string CarpetaMateriales = "Assets/enemy_models/Materiales";
    private const string CarpetaAnimaciones = "Assets/Animations/Enemigos";
    private const string CarpetaPrefabs = "Assets/Prefabs/Enemigos";

    private const string FbxMonstruo1 = "Assets/enemy_models/monstruo1/monstruo1.fbx";
    private const string FbxMonstruo2 = "Assets/enemy_models/monstruo2/monstruo2.fbx";
    private const string TexturasMonstruo1 = "Assets/enemy_models/monstruo1/TEXTUR/";
    private const string TexturaMonstruo2 = "Assets/enemy_models/monstruo2/monstruo2_texture.png";

    private const string ControllerMonstruo1 = CarpetaAnimaciones + "/Monstruo1.controller";
    private const string ControllerMonstruo2 = CarpetaAnimaciones + "/Monstruo2.controller";

    // Clips de monstruo1 (pistas NLA del .blend): "Walking" = rigAction, "Scream" = rigAction.001.
    private const string TomaCaminar = "rigAction";
    private const string TomaGrito = "rigAction.001";
    // Clips de monstruo2 (acciones del .blend).
    private const string TomaCaminar2 = "Caminar";
    private const string TomaAtacar2 = "Atacar";

    private class ConfigEnemigo
    {
        public string nombre;
        public string fbx;
        public float alturaObjetivo;     // <= 0 : escala original
        public string nodoFrente;        // malla que marca el frente (si no hay root motion)
        public AnimationClip clipPose;   // pose usada para medir (null = pose de reposo)
        public AnimationClip clipCaminar;
        public RuntimeAnimatorController controller;
        public float velocidadPatrulla;
        public float velocidadPersecucion;
        public float margenZonaAtaque; // metros que el trigger de ataque sobresale del cuerpo
        public float duracionAtaque;
        public float duracionGrito;
    }

    // ======================================================================
    // 1. PREFABS
    // ======================================================================

    [MenuItem("Tools/Enemigos/1. Crear prefabs de enemigos")]
    public static void CrearPrefabs()
    {
        CrearCarpeta(CarpetaMateriales);
        CrearCarpeta(CarpetaAnimaciones);
        CrearCarpeta(CarpetaPrefabs);

        ConfigurarClipsMonstruo1(ConfigurarImportador(FbxMonstruo1, CrearMaterialesMonstruo1()));
        ConfigurarClipsMonstruo2(ConfigurarImportador(FbxMonstruo2, CrearMaterialesMonstruo2()));

        // ---- monstruo1: Caminar / Quieto / Gritar / Atacar (sacados de "Walking" y "Scream")
        Dictionary<string, AnimationClip> clips = CargarClips(FbxMonstruo1);
        if (!clips.TryGetValue("Caminar", out AnimationClip caminar) ||
            !clips.TryGetValue("Quieto", out AnimationClip quieto) ||
            !clips.TryGetValue("Gritar", out AnimationClip gritar) ||
            !clips.TryGetValue("Atacar", out AnimationClip atacar))
        {
            Debug.LogError("[CrearEnemigos] No se encontraron los clips de monstruo1 (Caminar/Quieto/Gritar/Atacar). Revisa el importador del FBX.");
            return;
        }

        // Velocidad "natural" del ciclo de caminar (m/s) sacada del root motion.
        float velCaminar1 = caminar.averageSpeed.magnitude;
        if (velCaminar1 < 0.1f) velCaminar1 = 1.7f;
        AnimatorController controller = CrearAnimator(ControllerMonstruo1, quieto, 1f, caminar, velCaminar1, 4.5f, gritar, atacar);

        // ---- monstruo2: Caminar / Atacar (acciones del .blend). Camina en el sitio, sin root motion.
        Dictionary<string, AnimationClip> clips2 = CargarClips(FbxMonstruo2);
        if (!clips2.TryGetValue("Caminar", out AnimationClip caminar2) ||
            !clips2.TryGetValue("Atacar", out AnimationClip atacar2))
        {
            Debug.LogError("[CrearEnemigos] No se encontraron los clips de monstruo2 (Caminar/Atacar). Revisa el importador del FBX.");
            return;
        }
        // Sin clip de reposo: en reposo se reproduce el ciclo de caminar muy lento (balanceo).
        AnimatorController controller2 = CrearAnimator(ControllerMonstruo2, caminar2, 0.25f, caminar2, 1.2f, 3.2f, null, atacar2);

        ConstruirPrefab(new ConfigEnemigo
        {
            nombre = "Enemigo_Monstruo1",
            fbx = FbxMonstruo1,
            alturaObjetivo = 0f, // ~1.6 m encorvado: ya tiene buena escala
            nodoFrente = "Mask.004",
            clipPose = quieto,
            clipCaminar = caminar,
            controller = controller,
            velocidadPatrulla = 1.7f,
            velocidadPersecucion = 4.5f,
            margenZonaAtaque = 0.6f,
            duracionAtaque = atacar.length,
            duracionGrito = gritar.length * 0.8f,
        });

        ConstruirPrefab(new ConfigEnemigo
        {
            nombre = "Enemigo_Monstruo2",
            fbx = FbxMonstruo2,
            alturaObjetivo = 2.6f, // el modelo original mide ~6 m
            nodoFrente = "teeth top",
            clipPose = caminar2,
            clipCaminar = caminar2,
            controller = controller2,
            velocidadPatrulla = 1.2f,
            velocidadPersecucion = 3.2f,
            margenZonaAtaque = 0.6f,
            duracionAtaque = atacar2.length,
            duracionGrito = 0f,
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CrearEnemigos] Prefabs creados en {CarpetaPrefabs}. Ahora: Tools/Enemigos/2. Preparar y hornear NavMesh.");
    }

    // ---------------------------------------------------------------- materiales

    private static Dictionary<string, Material> CrearMaterialesMonstruo1()
    {
        return new Dictionary<string, Material>
        {
            { "rock", CrearMaterialLit("M_Monstruo1_Roca", TexturasMonstruo1 + "Rock Color.png", TexturasMonstruo1 + "Rock Normal.png", 0f, 0.25f) },
            { "LEATHER", CrearMaterialLit("M_Monstruo1_Cuero", TexturasMonstruo1 + "Leather color.png", TexturasMonstruo1 + "Leather Normal.png", 0f, 0.35f) },
            { "Metal", CrearMaterialLit("M_Monstruo1_Metal", TexturasMonstruo1 + "Metal color.png", TexturasMonstruo1 + "Metal Normal.png", 0.8f, 0.45f) },
            { "LIGHT EYE", CrearMaterialEmisivo("M_Monstruo1_Ojos", new Color(1f, 0.03f, 0.03f), 6f) },
            { "light2", CrearMaterialEmisivo("M_Monstruo1_Luz", new Color(1f, 0.2f, 0f), 4f) },
        };
    }

    private static Dictionary<string, Material> CrearMaterialesMonstruo2()
    {
        Material piel = CrearMaterialLit("M_Monstruo2_Piel", TexturaMonstruo2, null, 0f, 0.4f);
        return new Dictionary<string, Material>
        {
            { "ALIEN MONSTER Material", piel },
            { "teeth", CrearMaterialLit("M_Monstruo2_Dientes", TexturaMonstruo2, null, 0f, 0.6f) },
        };
    }

    private static Material CrearMaterialLit(string nombre, string texBase, string texNormal, float metallic, float smoothness)
    {
        Material m = ObtenerMaterial(nombre);
        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(texBase);
        if (baseMap == null) Debug.LogWarning($"[CrearEnemigos] Falta textura {texBase}");
        m.SetTexture("_BaseMap", baseMap);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);

        if (!string.IsNullOrEmpty(texNormal))
        {
            MarcarComoNormalMap(texNormal);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texNormal);
            m.SetTexture("_BumpMap", normal);
            if (normal != null) m.EnableKeyword("_NORMALMAP");
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material CrearMaterialEmisivo(string nombre, Color color, float intensidad)
    {
        Material m = ObtenerMaterial(nombre);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.8f);
        m.SetColor("_EmissionColor", color * intensidad);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material ObtenerMaterial(string nombre)
    {
        string ruta = $"{CarpetaMateriales}/{nombre}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(lit != null ? lit : Shader.Find("Standard"));
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    private static void MarcarComoNormalMap(string ruta)
    {
        if (AssetImporter.GetAtPath(ruta) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
        {
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
    }

    // ---------------------------------------------------------------- importador

    private static ModelImporter ConfigurarImportador(string ruta, Dictionary<string, Material> materiales)
    {
        if (!(AssetImporter.GetAtPath(ruta) is ModelImporter imp))
        {
            Debug.LogError($"[CrearEnemigos] No se encontro el modelo {ruta}");
            return null;
        }

        imp.globalScale = 1f;
        imp.useFileScale = true;
        imp.importCameras = false;
        imp.importLights = false;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        foreach (var par in materiales)
        {
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), par.Key), par.Value);
        }

        imp.animationType = ModelImporterAnimationType.Generic;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.importAnimation = true;
        imp.clipAnimations = new ModelImporterClipAnimation[0]; // tomas por defecto para leerlas
        imp.SaveAndReimport();
        return imp;
    }

    private static ModelImporterClipAnimation BuscarToma(ModelImporterClipAnimation[] tomas, string nombre)
    {
        // Blender nombra las tomas "Armature|Accion" (a veces repetido): se compara el final.
        return tomas.FirstOrDefault(c => c.takeName == nombre || c.takeName.EndsWith("|" + nombre));
    }

    private static void ConfigurarClipsMonstruo2(ModelImporter imp)
    {
        if (imp == null) return;

        ModelImporterClipAnimation[] porDefecto = imp.defaultClipAnimations;
        ModelImporterClipAnimation tomaCaminar = BuscarToma(porDefecto, TomaCaminar2);
        ModelImporterClipAnimation tomaAtacar = BuscarToma(porDefecto, TomaAtacar2);
        if (tomaCaminar == null || tomaAtacar == null)
        {
            Debug.LogError("[CrearEnemigos] monstruo2, tomas encontradas en el FBX: " + string.Join(", ", porDefecto.Select(c => c.takeName)));
            return;
        }

        // Camina en el sitio: todo el movimiento de la raiz queda en la pose (sin root motion).
        imp.motionNodeName = "";
        imp.clipAnimations = new[]
        {
            Clip(tomaCaminar, "Caminar", tomaCaminar.firstFrame, tomaCaminar.lastFrame, true, false),
            Clip(tomaAtacar, "Atacar", tomaAtacar.firstFrame, tomaAtacar.lastFrame, false, false),
        };
        imp.SaveAndReimport();
    }

    private static void ConfigurarClipsMonstruo1(ModelImporter imp)
    {
        if (imp == null) return;
        string ruta = imp.assetPath;

        // Root motion: el ciclo "Walking" avanza el cuerpo (hueso DEF-spine) ~1.7 m.
        // Se extrae como root motion y, con applyRootMotion = false, el clip queda en el sitio
        // y el NavMeshAgent es quien mueve al enemigo.
        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        Transform columna = BuscarHijo(modelo.transform, "DEF-spine");
        if (columna != null) imp.motionNodeName = AnimationUtility.CalculateTransformPath(columna, modelo.transform);
        else Debug.LogWarning("[CrearEnemigos] No se encontro DEF-spine para el root motion.");

        ModelImporterClipAnimation[] porDefecto = imp.defaultClipAnimations;
        ModelImporterClipAnimation tomaCaminar = BuscarToma(porDefecto, TomaCaminar);
        ModelImporterClipAnimation tomaGrito = BuscarToma(porDefecto, TomaGrito);
        if (tomaCaminar == null || tomaGrito == null)
        {
            Debug.LogError("[CrearEnemigos] monstruo1, tomas encontradas en el FBX: " + string.Join(", ", porDefecto.Select(c => c.takeName)));
            return;
        }

        float g0 = tomaGrito.firstFrame, g1 = tomaGrito.lastFrame;
        imp.clipAnimations = new[]
        {
            Clip(tomaCaminar, "Caminar", tomaCaminar.firstFrame, tomaCaminar.lastFrame, true, true),
            // El grito arranca en la pose de reposo (de pie) en el primer frame: se salta.
            Clip(tomaGrito, "Gritar", g0 + 2f, g0 + 34f, false, true),
            // Final del grito: se agacha y se lanza hacia delante -> ataque.
            Clip(tomaGrito, "Atacar", g0 + 34f, g1, false, true),
            // Tramo encorvado del grito, casi estatico -> reposo en bucle.
            Clip(tomaGrito, "Quieto", g0 + 10f, g0 + 36f, true, true),
        };
        imp.SaveAndReimport();
    }

    private static ModelImporterClipAnimation Clip(ModelImporterClipAnimation toma, string nombre, float desde, float hasta, bool bucle, bool extraerXZ)
    {
        return new ModelImporterClipAnimation
        {
            takeName = toma.takeName,
            name = nombre,
            firstFrame = desde,
            lastFrame = hasta,
            loopTime = bucle,
            loopPose = bucle,
            wrapMode = bucle ? WrapMode.Loop : WrapMode.Once,
            // Rotacion y altura quedan en la pose; XZ se extrae como root motion si extraerXZ.
            lockRootRotation = true,
            keepOriginalOrientation = true,
            lockRootHeightY = true,
            keepOriginalPositionY = true,
            lockRootPositionXZ = !extraerXZ,
            keepOriginalPositionXZ = true,
        };
    }

    private static Dictionary<string, AnimationClip> CargarClips(string ruta)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ruta)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .GroupBy(c => c.name)
            .ToDictionary(g => g.Key, g => g.First());
    }

    // ---------------------------------------------------------------- animator

    // Locomocion 1D por "Velocidad" (reposo -> caminar -> correr acelerando el ciclo)
    // + estados Gritar (opcional) y Atacar disparados por trigger desde cualquier estado.
    private static AnimatorController CrearAnimator(string ruta, AnimationClip quieto, float escalaQuieto,
        AnimationClip caminar, float velCaminar, float velCorrer, AnimationClip gritar, AnimationClip atacar)
    {
        AssetDatabase.DeleteAsset(ruta);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Gritar", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState locomocion = ctrl.CreateBlendTreeInController("Locomocion", out BlendTree arbol, 0);
        arbol.blendType = BlendTreeType.Simple1D;
        arbol.blendParameter = "Velocidad";
        arbol.useAutomaticThresholds = false;
        arbol.AddChild(quieto, 0f);
        arbol.AddChild(caminar, velCaminar);
        arbol.AddChild(caminar, velCorrer);
        ChildMotion[] hijos = arbol.children;
        hijos[0].timeScale = escalaQuieto;
        hijos[2].timeScale = velCorrer / velCaminar; // corre acelerando el ciclo de caminar
        arbol.children = hijos;
        sm.defaultState = locomocion;

        if (gritar != null) AgregarAccion(sm, locomocion, "Gritar", gritar);
        AgregarAccion(sm, locomocion, "Atacar", atacar);

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    private static void AgregarAccion(AnimatorStateMachine sm, AnimatorState volverA, string trigger, AnimationClip clip)
    {
        AnimatorState estado = sm.AddState(trigger);
        estado.motion = clip;

        AnimatorStateTransition entrada = sm.AddAnyStateTransition(estado);
        entrada.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        entrada.hasExitTime = false;
        entrada.duration = 0.15f;
        entrada.canTransitionToSelf = false;

        AnimatorStateTransition salida = estado.AddTransition(volverA);
        // Sale cuando el clip termino del todo: la muerte del jugador se aplica al final del
        // ataque y pausa el juego, asi que la pose final debe ser la del clip, no una mezcla.
        salida.hasExitTime = true;
        salida.exitTime = 1f;
        salida.duration = 0.2f;
    }

    // ---------------------------------------------------------------- prefab

    private static void ConstruirPrefab(ConfigEnemigo c)
    {
        GameObject modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(c.fbx);
        if (modeloAsset == null)
        {
            Debug.LogError($"[CrearEnemigos] No se pudo cargar {c.fbx}");
            return;
        }

        GameObject raiz = new GameObject(c.nombre);
        try
        {
            Transform pivote = new GameObject("Modelo").transform;
            pivote.SetParent(raiz.transform, false);

            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloAsset, pivote);
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;

            // Pose para medir (monstruo1 en reposo esta de pie, en sus animaciones va encorvado).
            if (c.clipPose != null) c.clipPose.SampleAnimation(modelo, 0f);

            // Orientacion: el frente del modelo debe mirar a +Z (forward del NavMeshAgent).
            float yaw = CalcularYaw(c, modelo);
            pivote.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Bounds b = CalcularBounds(modelo);
            float escala = c.alturaObjetivo > 0f && b.size.y > 0.01f ? c.alturaObjetivo / b.size.y : 1f;
            pivote.localScale = Vector3.one * escala;

            // Pies en y = 0 y centrado en XZ.
            b = CalcularBounds(modelo);
            pivote.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = CalcularBounds(modelo);

            // Se deshace la pose muestreada para no guardar overrides de cada hueso.
            PrefabUtility.RevertPrefabInstance(modelo, InteractionMode.AutomatedAction);
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;

            ConfigurarRenderers(modelo);
            ConfigurarAnimator(modelo, c.controller);
            ConfigurarFisica(raiz, b);
            ZonaAtaqueEnemigo zona = ConfigurarZonaAtaque(raiz, b, c.margenZonaAtaque);
            NavMeshAgent agente = ConfigurarAgente(raiz, b, c);
            ConfigurarAudio(raiz);
            ConfigurarEnemigo(raiz, c, b, modelo.GetComponent<Animator>(), zona);

            string ruta = $"{CarpetaPrefabs}/{c.nombre}.prefab";
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta, out bool ok);
            Debug.Log(ok
                ? $"[CrearEnemigos] {ruta}: tamano {b.size:F2} m, escala {escala:F2}, giro {yaw:F0}°, radio agente {agente.radius:F2}"
                : $"[CrearEnemigos] Error guardando {ruta}");
        }
        finally
        {
            Object.DestroyImmediate(raiz);
        }
    }

    private static float CalcularYaw(ConfigEnemigo c, GameObject modelo)
    {
        Vector3 frente = Vector3.zero;

        // 1) Direccion del root motion del ciclo de caminar (lo mas fiable).
        if (c.clipCaminar != null && c.clipCaminar.averageSpeed.sqrMagnitude > 0.01f)
        {
            frente = c.clipCaminar.averageSpeed;
        }
        // 2) Posicion de la malla de la cara respecto al centro del cuerpo.
        else if (!string.IsNullOrEmpty(c.nodoFrente))
        {
            Transform cara = BuscarHijo(modelo.transform, c.nodoFrente);
            Renderer r = cara != null ? cara.GetComponent<Renderer>() : null;
            if (r != null) frente = r.bounds.center - CalcularBounds(modelo).center;
        }

        frente.y = 0f;
        if (frente.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"[CrearEnemigos] {c.nombre}: no se pudo detectar el frente, se deja sin girar.");
            return 0f;
        }

        float rumbo = Mathf.Atan2(frente.x, frente.z) * Mathf.Rad2Deg;
        Debug.Log($"[CrearEnemigos] {c.nombre}: frente detectado {frente:F2} (rumbo {rumbo:F0}°)");
        return Mathf.Round(-rumbo / 90f) * 90f;
    }

    private static Bounds CalcularBounds(GameObject go)
    {
        bool hay = false;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                // Skinning manual (huesos * bindposes): posiciones de mundo exactas de la pose actual,
                // sin depender de la escala del renderer ni de BakeMesh.
                Mesh mesh = smr.sharedMesh;
                Transform[] huesos = smr.bones;
                Matrix4x4[] bind = mesh.bindposes;
                Vector3[] vs = mesh.vertices;
                BoneWeight[] pesos = mesh.boneWeights;
                bool skin = huesos.Length > 0 && pesos.Length == vs.Length && bind.Length == huesos.Length;
                Matrix4x4[] skinM = new Matrix4x4[huesos.Length];
                for (int h = 0; h < huesos.Length; h++)
                    skinM[h] = huesos[h] != null ? huesos[h].localToWorldMatrix * bind[h] : smr.transform.localToWorldMatrix;

                for (int i = 0; i < vs.Length; i += 4)
                {
                    Vector3 w;
                    if (skin)
                    {
                        BoneWeight p = pesos[i];
                        w = skinM[p.boneIndex0].MultiplyPoint3x4(vs[i]) * p.weight0
                          + skinM[p.boneIndex1].MultiplyPoint3x4(vs[i]) * p.weight1
                          + skinM[p.boneIndex2].MultiplyPoint3x4(vs[i]) * p.weight2
                          + skinM[p.boneIndex3].MultiplyPoint3x4(vs[i]) * p.weight3;
                    }
                    else
                    {
                        w = smr.transform.localToWorldMatrix.MultiplyPoint3x4(vs[i]);
                    }
                    if (!hay) { b = new Bounds(w, Vector3.zero); hay = true; }
                    else b.Encapsulate(w);
                }
            }
            else if (r is MeshRenderer)
            {
                if (!hay) { b = r.bounds; hay = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        return b;
    }

    private static void ConfigurarRenderers(GameObject modelo)
    {
        foreach (SkinnedMeshRenderer smr in modelo.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            // Las animaciones sacan partes fuera de los bounds de reposo: evita que desaparezcan.
            smr.updateWhenOffscreen = true;
        }
    }

    private static void ConfigurarAnimator(GameObject modelo, RuntimeAnimatorController controller)
    {
        Animator anim = modelo.GetComponent<Animator>();
        if (controller == null)
        {
            if (anim != null) Object.DestroyImmediate(anim);
            return;
        }

        if (anim == null) anim = modelo.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    private static void ConfigurarFisica(GameObject raiz, Bounds b)
    {
        // Collider solido (para la linea de vista y para que el jugador choque con el).
        CapsuleCollider cap = raiz.AddComponent<CapsuleCollider>();
        bool alargado = b.size.z > b.size.y;
        cap.direction = alargado ? 2 : 1;
        cap.radius = Mathf.Min(b.size.x, alargado ? b.size.y : b.size.z) * 0.5f * 0.85f;
        cap.height = Mathf.Max(alargado ? b.size.z : b.size.y, cap.radius * 2f);
        cap.center = b.center - raiz.transform.position;

        Rigidbody rb = raiz.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
    }

    // Trigger grande alrededor del cuerpo (como el trigger del agua): si el jugador entra,
    // el enemigo ataca; si sigue dentro cuando termina la animacion, recibe el dano.
    private static ZonaAtaqueEnemigo ConfigurarZonaAtaque(GameObject raiz, Bounds b, float margen)
    {
        GameObject go = new GameObject("ZonaAtaque");
        go.transform.SetParent(raiz.transform, false);

        BoxCollider caja = go.AddComponent<BoxCollider>();
        caja.isTrigger = true;
        // Desde el suelo hasta un poco por encima de la cabeza del jugador (su ColliderOxigeno esta a ~1.7 m).
        float alto = Mathf.Max(b.size.y, 2.2f);
        caja.size = new Vector3(b.size.x + margen * 2f, alto, b.size.z + margen * 2f);
        caja.center = new Vector3(b.center.x - raiz.transform.position.x, alto * 0.5f, b.center.z - raiz.transform.position.z);

        return go.AddComponent<ZonaAtaqueEnemigo>();
    }

    private static NavMeshAgent ConfigurarAgente(GameObject raiz, Bounds b, ConfigEnemigo c)
    {
        NavMeshAgent agente = raiz.AddComponent<NavMeshAgent>();
        // Tipo "Monster" (el mismo que hornea el NavMeshSurface) y solo camina por el area "Monster".
        agente.agentTypeID = IdTipoMonster();
        agente.areaMask = MascaraAreaMonster();
        agente.radius = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * 0.5f * 0.8f, 0.3f, 1.5f);
        agente.height = b.size.y;
        agente.baseOffset = 0f;
        agente.speed = c.velocidadPatrulla;
        agente.angularSpeed = 300f;
        agente.acceleration = 12f;
        agente.stoppingDistance = 0.4f;
        agente.autoBraking = true;
        agente.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        return agente;
    }

    private static void ConfigurarAudio(GameObject raiz)
    {
        AudioSource src = raiz.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 2f;
        src.maxDistance = 25f;
    }

    private static void ConfigurarEnemigo(GameObject raiz, ConfigEnemigo c, Bounds b, Animator anim, ZonaAtaqueEnemigo zona)
    {
        Enemigo e = raiz.AddComponent<Enemigo>();
        var so = new SerializedObject(e);
        so.FindProperty("velocidadPatrulla").floatValue = c.velocidadPatrulla;
        so.FindProperty("velocidadPersecucion").floatValue = c.velocidadPersecucion;
        so.FindProperty("zonaAtaque").objectReferenceValue = zona;
        so.FindProperty("porcentajeDanio").floatValue = 0.3f;
        so.FindProperty("duracionAtaque").floatValue = c.duracionAtaque;
        so.FindProperty("duracionGrito").floatValue = c.duracionGrito;
        so.FindProperty("alturaOjos").floatValue = b.size.y * 0.8f;
        so.FindProperty("adelantoOjos").floatValue = b.size.z * 0.4f; // cabeza, no centro del cuerpo
        so.FindProperty("animator").objectReferenceValue = c.controller != null ? anim : null;
        so.FindProperty("audioSource").objectReferenceValue = raiz.GetComponent<AudioSource>();
        so.FindProperty("obstaculosVision").intValue = ~LayerMask.GetMask("Ignore Raycast", "Water", "UI");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ======================================================================
    // 2. NAVMESH
    // ======================================================================

    [MenuItem("Tools/Enemigos/2. Preparar y hornear NavMesh (escena abierta)")]
    public static void PrepararNavMesh()
    {
        var escena = EditorSceneManager.GetActiveScene();
        NavMeshSurface surface = PrepararSurface(out int ignorados, out int obstaculos);

        EditorSceneManager.MarkSceneDirty(escena);
        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });
        Selection.activeObject = surface.gameObject;
        Debug.Log($"[CrearEnemigos] Horneando NavMesh en '{escena.name}' ({ignorados} objetos excluidos, {obstaculos} obstaculos de puerta). Guarda la escena al terminar.");
    }

    // Para linea de comandos (-batchmode -executeMethod CrearEnemigos.Batch):
    // crea los prefabs, abre el nivel, hornea de forma sincrona y guarda la escena.
    public static void Batch()
    {
        CrearPrefabs();
        HornearBatch();
    }

    // Solo NavMesh (-executeMethod CrearEnemigos.HornearBatch): configura el NavMeshSurface de la
    // escena (el que haya, o crea uno), hornea y guarda.
    public static void HornearBatch()
    {
        const string rutaEscena = "Assets/Scenes/MaliciousComplex.unity";
        var escena = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);
        NavMeshSurface surface = PrepararSurface(out int ignorados, out int obstaculos);

        surface.RemoveData();
        surface.BuildNavMesh();
        if (surface.navMeshData == null)
        {
            Debug.LogError("[CrearEnemigos] El horneado no genero datos.");
            return;
        }

        // Igual que NavMeshAssetManager: carpeta con el nombre de la escena.
        string carpeta = rutaEscena.Substring(0, rutaEscena.Length - ".unity".Length);
        CrearCarpeta(carpeta);
        string rutaDatos = $"{carpeta}/NavMesh-{surface.gameObject.name}.asset";
        AssetDatabase.DeleteAsset(rutaDatos);
        AssetDatabase.CreateAsset(surface.navMeshData, rutaDatos);

        var tri = NavMesh.CalculateTriangulation();
        Debug.Log($"[CrearEnemigos] NavMesh horneada: {tri.vertices.Length} vertices, {tri.indices.Length / 3} triangulos, {ignorados} excluidos, {obstaculos} obstaculos de puerta -> {rutaDatos}");

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
    }

    private static NavMeshSurface PrepararSurface(out int ignorados, out int obstaculos)
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject go = new GameObject("NavMesh");
            Undo.RegisterCreatedObjectUndo(go, "Crear NavMeshSurface");
            surface = go.AddComponent<NavMeshSurface>();
        }

        Undo.RecordObject(surface, "Configurar NavMeshSurface");
        surface.agentTypeID = IdTipoMonster();
        surface.defaultArea = AreaMonster(); // todo lo horneado es area "Monster"
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~LayerMask.GetMask("Ignore Raycast", "Water", "UI", "Player");
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;

        ignorados = 0;
        obstaculos = 0;

        // Agua, triggers (zonas, checkpoints...), jugador y plataformas moviles no son suelo.
        foreach (FloodWater agua in Object.FindObjectsByType<FloodWater>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ignorados += Ignorar(agua.gameObject, true);
        foreach (MovingPlatform p in Object.FindObjectsByType<MovingPlatform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ignorados += Ignorar(p.gameObject, true);
        foreach (CharacterController cc in Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ignorados += Ignorar(cc.gameObject, true);
        foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (col.gameObject.GetComponents<Collider>().All(x => x.isTrigger))
                ignorados += Ignorar(col.gameObject, false);
        }

        // Hojas de las puertas: se mueven, asi que no se hornean; las tallan NavMeshObstacles.
        foreach (PuertaPortal puerta in Object.FindObjectsByType<PuertaPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(puerta);
            foreach (string campo in new[] { "hojaIzquierda", "hojaDerecha" })
            {
                if (so.FindProperty(campo)?.objectReferenceValue is Transform hoja)
                {
                    ignorados += Ignorar(hoja.gameObject, true);
                    obstaculos += AgregarObstaculo(hoja.gameObject);
                }
            }
        }

        return surface;
    }

    private static int Ignorar(GameObject go, bool hijos)
    {
        NavMeshModifier mod = go.GetComponent<NavMeshModifier>();
        if (mod != null && mod.ignoreFromBuild) return 0;
        if (mod == null) mod = Undo.AddComponent<NavMeshModifier>(go);
        Undo.RecordObject(mod, "Ignorar en NavMesh");
        mod.ignoreFromBuild = true;
        mod.applyToChildren = hijos;
        return 1;
    }

    private static int AgregarObstaculo(GameObject hoja)
    {
        if (hoja.GetComponent<NavMeshObstacle>() != null) return 0;

        Renderer r = hoja.GetComponentInChildren<Renderer>();
        Collider col = hoja.GetComponentInChildren<Collider>();
        Bounds mundo = col != null ? col.bounds : r != null ? r.bounds : new Bounds(hoja.transform.position, Vector3.one);

        NavMeshObstacle obs = Undo.AddComponent<NavMeshObstacle>(hoja);
        obs.shape = NavMeshObstacleShape.Box;
        obs.carving = true;
        obs.carveOnlyStationary = false;
        // Bounds de mundo -> espacio local de la hoja.
        Vector3 escala = hoja.transform.lossyScale;
        obs.center = hoja.transform.InverseTransformPoint(mundo.center);
        obs.size = new Vector3(
            mundo.size.x / Mathf.Max(0.0001f, Mathf.Abs(escala.x)),
            mundo.size.y / Mathf.Max(0.0001f, Mathf.Abs(escala.y)),
            mundo.size.z / Mathf.Max(0.0001f, Mathf.Abs(escala.z)));
        return 1;
    }

    // ======================================================================

    // Tipo de agente y area definidos en Project Settings > AI Navigation (NavMeshAreas.asset).
    private const string NombreTipoMonster = "Monster";
    private const string NombreAreaMonster = "Monster";

    private static int IdTipoMonster()
    {
        for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
        {
            int id = NavMesh.GetSettingsByIndex(i).agentTypeID;
            if (NavMesh.GetSettingsNameFromID(id) == NombreTipoMonster) return id;
        }
        Debug.LogError($"[CrearEnemigos] No existe el tipo de agente '{NombreTipoMonster}' (Project Settings > AI Navigation > Agents). Se usa Humanoid.");
        return 0;
    }

    private static int AreaMonster()
    {
        int area = NavMesh.GetAreaFromName(NombreAreaMonster);
        if (area < 0)
        {
            Debug.LogError($"[CrearEnemigos] No existe el area '{NombreAreaMonster}' (Project Settings > AI Navigation > Areas). Se usa Walkable.");
            return 0;
        }
        return area;
    }

    private static int MascaraAreaMonster() => 1 << AreaMonster();

    private static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nombre) return t;
        }
        return null;
    }

    private static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }
}
