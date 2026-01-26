using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoomSorter : MonoBehaviour
{
    [Header("Arrastra aquí la sala a probar")]
    public GameObject salaParaAnalizar;

    [Header("Resultados del Análisis")]
    public GameObject sueloDetectado;
    public List<GameObject> paredesCiegas = new List<GameObject>();   // Paredes normales
    public List<GameObject> paredesConPuerta = new List<GameObject>(); // Paredes con hueco

    public void ProcesarSalaDesdeBoton()
    {
        if (salaParaAnalizar != null) ProcesarSala(salaParaAnalizar);
        else Debug.LogError("Arrastra una sala primero.");
    }

    public void ProcesarSala(GameObject sala)
    {
        // 1. Limpieza inicial
        sueloDetectado = null;
        paredesCiegas.Clear();
        paredesConPuerta.Clear();

        // 2. Obtener todos los hijos directos (Estructuras principales)
        // Nota: Ahora NO filtramos por MeshRenderer porque dijiste que tu pared
        // es un objeto vacío que contiene cubos dentro.
        List<Transform> estructuras = new List<Transform>();
        foreach (Transform hijo in sala.transform)
        {
            estructuras.Add(hijo);
        }

        if (estructuras.Count == 0) return;

        // 3. Buscar el Suelo (El que tenga la Y más baja)
        Transform candidatoSuelo = estructuras[0];
        float alturaMinima = estructuras[0].localPosition.y;

        for (int i = 1; i < estructuras.Count; i++)
        {
            // Buscamos el mínimo
            if (estructuras[i].localPosition.y < alturaMinima)
            {
                alturaMinima = estructuras[i].localPosition.y;
                candidatoSuelo = estructuras[i];
            }
        }

        // Asignamos el suelo
        sueloDetectado = candidatoSuelo.gameObject;
        Debug.Log($"SUELO DETECTADO: '{sueloDetectado.name}' (Y: {alturaMinima})");

        // 4. Analizar el resto (Paredes)
        foreach (Transform estructura in estructuras)
        {
            if (estructura == candidatoSuelo) continue; // Saltamos el suelo

            // Ahora preguntamos: ¿Esta pared tiene dentro el marcador de puerta?
            if (TienePuertaDentro(estructura))
            {
                paredesConPuerta.Add(estructura.gameObject);
                Debug.Log($"PARED CON PUERTA: '{estructura.name}'");
            }
            else
            {
                paredesCiegas.Add(estructura.gameObject);
                Debug.Log($"PARED CIEGA: '{estructura.name}'");
            }
        }
    }

    // --- LÓGICA DE DETECCIÓN DE PUERTA ---
    bool TienePuertaDentro(Transform paredPadre)
    {
        // GetComponentsInChildren busca en el padre, en los hijos, en los nietos...
        // Busca cualquier Transform (cualquier objeto)
        Transform[] todosLosHijos = paredPadre.GetComponentsInChildren<Transform>();

        foreach (Transform hijo in todosLosHijos)
        {
            if (hijo.CompareTag("Puerta"))
            {
                return true; // ¡Encontrado el marcador!
            }
        }
        return false; // No hay rastro de puerta aquí
    }
}

// --- BOTÓN DEL INSPECTOR ---
#if UNITY_EDITOR
[CustomEditor(typeof(RoomSorter))]
public class RoomSorterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RoomSorter script = (RoomSorter)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Procesar Sala (Analizar Puertas)"))
        {
            script.ProcesarSalaDesdeBoton();
        }
    }
}
#endif