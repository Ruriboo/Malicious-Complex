using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Genera Assets/Prefabs/ServerRack.prefab a partir de Assets/assets_models/Server_Rack_final.fbx.
//   Tools/Crear prefab Server Rack
// Linea de comandos: -batchmode -executeMethod CrearRack.Crear -quit
public static class CrearRack
{
    private const string Fbx = "Assets/assets_models/Server_Rack_final.fbx";
    private const string RutaPrefab = "Assets/Prefabs/ServerRack.prefab";
    private const string Sfx = "Assets/Audio/SFX/Botones/ButtonPress.WAV";
    private const float MargenInteraccion = 1.2f; // metros que el trigger de rango sobresale del rack

    [MenuItem("Tools/Crear prefab Server Rack")]
    public static void Crear()
    {
        GameObject modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        if (modeloAsset == null)
        {
            Debug.LogError($"[CrearRack] No se pudo cargar {Fbx}");
            return;
        }

        GameObject raiz = new GameObject("ServerRack");
        try
        {
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloAsset, raiz.transform);
            modelo.name = "Modelo";
            modelo.transform.localPosition = Vector3.zero;

            // Base del modelo en y = 0 y centrado en XZ.
            Bounds b = CalcularBounds(modelo);
            modelo.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = CalcularBounds(modelo);

            // Collider solido para que el jugador choque con el rack.
            BoxCollider solido = raiz.AddComponent<BoxCollider>();
            solido.center = b.center;
            solido.size = b.size;

            // Trigger de rango de interaccion (ObjetoInteractuable usa OnTriggerEnter/Exit).
            BoxCollider rango = raiz.AddComponent<BoxCollider>();
            rango.isTrigger = true;
            rango.center = b.center;
            rango.size = b.size + new Vector3(MargenInteraccion * 2f, 0.5f, MargenInteraccion * 2f);

            // Luz indicadora (roja = listo, verde = usado), arriba al frente.
            GameObject luzGO = new GameObject("Luz Indicador");
            luzGO.transform.SetParent(raiz.transform, false);
            luzGO.transform.localPosition = new Vector3(0f, b.max.y * 0.85f, b.max.z + 0.3f);
            Light luz = luzGO.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = Color.red;
            luz.intensity = 1.5f;
            luz.range = 2.5f;

            AudioSource src = raiz.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = 20f;
            src.outputAudioMixerGroup = BuscarGrupoSFX();

            RackEliminarEnemigos rack = raiz.AddComponent<RackEliminarEnemigos>();
            var so = new SerializedObject(rack);
            so.FindProperty("luzIndicador").objectReferenceValue = luz;
            so.FindProperty("audioSource").objectReferenceValue = src;
            so.FindProperty("sfxActivar").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out bool ok);
            Debug.Log(ok
                ? $"[CrearRack] {RutaPrefab} creado (tamano {b.size:F2} m)"
                : $"[CrearRack] Error guardando {RutaPrefab}");
        }
        finally
        {
            Object.DestroyImmediate(raiz);
        }
    }

    private static AudioMixerGroup BuscarGrupoSFX()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioMixer"))
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GUIDToAssetPath(guid));
            if (mixer == null) continue;
            AudioMixerGroup[] grupos = mixer.FindMatchingGroups("SFX");
            if (grupos.Length > 0) return grupos[0];
        }
        return null;
    }

    private static Bounds CalcularBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
