using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SimpleDungeonGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject pasilloPrefab;
    public GameObject salaPrefab;

    [Header("Contenedor")]
    public Transform worldContainer;

    public void GenerarPruebaSimple()
    {
        // 1. Limpieza
        if (worldContainer != null)
        {
            // Usamos un bucle inverso para borrar en modo editor sin errores
            for (int i = worldContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(worldContainer.GetChild(i).gameObject);
            }
        }

        // 2. Instanciar Pasillo Base
        GameObject nuevoPasillo = Instantiate(pasilloPrefab, Vector3.zero, Quaternion.identity);
        nuevoPasillo.name = "Pasillo_Raiz";
        if (worldContainer != null) nuevoPasillo.transform.parent = worldContainer;

        // 3. Buscar TODAS las puertas del pasillo (CAMBIO IMPORTANTE)
        List<Transform> puertasDelPasillo = BuscarTodasLasPuertas(nuevoPasillo);

        if (puertasDelPasillo.Count == 0)
        {
            Debug.LogError("El pasillo no tiene puertas.");
            return;
        }

        Debug.Log($"Se han encontrado {puertasDelPasillo.Count} puertas en el pasillo. Generando salas...");

        // 4. Bucle: Generar una sala por cada puerta encontrada
        foreach (Transform puertaPasillo in puertasDelPasillo)
        {
            GenerarYPegarSala(puertaPasillo);
        }
    }

    void GenerarYPegarSala(Transform puertaObjetivoDelPasillo)
    {
        // A. Instanciar Sala
        GameObject nuevaSala = Instantiate(salaPrefab, new Vector3(0, -100, 0), Quaternion.identity);
        nuevaSala.name = "Sala_Anexa";
        if (worldContainer != null) nuevaSala.transform.parent = worldContainer;

        // B. Buscar el conector de la nueva sala (Usamos el primero que encontremos para conectar)
        Transform puertaDeLaSala = BuscarPrimeraPuerta(nuevaSala);

        if (puertaDeLaSala == null)
        {
            Debug.LogError("La sala prefab no tiene puertas. Destruyendo.");
            DestroyImmediate(nuevaSala);
            return;
        }

        // C. Alinear
        AlinearSalaAlPasillo(nuevaSala.transform, puertaDeLaSala, puertaObjetivoDelPasillo);
    }

    void AlinearSalaAlPasillo(Transform salaRaiz, Transform puertaSala, Transform puertaPasillo)
    {
        // --- CORRECCIÓN DE ROTACIÓN (SÓLO EJE Y) ---

        // 1. Queremos que la puerta de la sala mire al lado contrario que la del pasillo
        Vector3 forwardPasillo = puertaPasillo.forward;
        Vector3 forwardSala = puertaSala.forward;

        // 2. Calculamos el ángulo NECESARIO solo en el eje Y (evita vuelcos)
        // Queremos alinear forwardSala con -forwardPasillo
        float anguloGiro = Vector3.SignedAngle(forwardSala, -forwardPasillo, Vector3.up);

        // 3. Aplicamos la rotación a la sala
        salaRaiz.Rotate(Vector3.up, anguloGiro);

        // --- CORRECCIÓN DE POSICIÓN ---

        // 4. Ahora que la rotación es correcta, calculamos el desplazamiento
        Vector3 offset = puertaSala.position - salaRaiz.position;
        salaRaiz.position = puertaPasillo.position - offset;
    }

    // --- MÉTODOS DE BÚSQUEDA ---

    // Nuevo: Devuelve una lista con todas las puertas
    List<Transform> BuscarTodasLasPuertas(GameObject estructura)
    {
        List<Transform> listaPuertas = new List<Transform>();
        Transform[] todosLosHijos = estructura.GetComponentsInChildren<Transform>();

        foreach (Transform hijo in todosLosHijos)
        {
            if (hijo.CompareTag("Puerta"))
            {
                listaPuertas.Add(hijo);
            }
        }
        return listaPuertas;
    }

    // El antiguo: Devuelve solo la primera (para la sala que vamos a pegar)
    Transform BuscarPrimeraPuerta(GameObject estructura)
    {
        Transform[] todosLosHijos = estructura.GetComponentsInChildren<Transform>();
        foreach (Transform hijo in todosLosHijos)
        {
            if (hijo.CompareTag("Puerta")) return hijo;
        }
        return null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SimpleDungeonGenerator))]
public class SimpleDungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        SimpleDungeonGenerator script = (SimpleDungeonGenerator)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Generar Todas las Salas"))
        {
            script.GenerarPruebaSimple();
        }
    }
}
#endif