using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para usar listas avanzadas

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonGenerator : MonoBehaviour
{
    [Header("Configuración")]
    public int maxPiezas = 20;
    public LayerMask layerDungeon; // Pon aquí "Default" o la capa de tus suelos/paredes

    [Header("Prefabs")]
    public List<GameObject> prefabsPasillos;
    public List<GameObject> prefabsSalas;
    public GameObject prefabMuroCierre;

    [Header("Contenedor")]
    public Transform worldContainer;

    // Listas internas
    private List<Transform> conectoresPendientes = new List<Transform>();
    private int piezasActuales = 0;

    public void GenerarMapaCompleto()
    {
        // 1. Limpieza inicial
        piezasActuales = 0;
        conectoresPendientes.Clear();
        if (worldContainer != null)
        {
            // Bucle inverso para borrar hijos sin errores
            for (int i = worldContainer.childCount - 1; i >= 0; i--)
                DestroyImmediate(worldContainer.GetChild(i).gameObject);
        }

        // 2. Pieza Inicial (Spawn)
        if (prefabsPasillos.Count == 0) return;
        GameObject piezaInicial = Instantiate(prefabsPasillos[0], Vector3.zero, Quaternion.identity);
        FinalizarPieza(piezaInicial, "Spawn", null); // null porque no tiene puerta de entrada

        // 3. BUCLE PRINCIPAL
        int seguridad = 0;
        while (conectoresPendientes.Count > 0 && piezasActuales < maxPiezas && seguridad < 500)
        {
            seguridad++;

            // Cogemos la primera puerta pendiente (FIFO - Primero en entrar, primero en salir)
            // Esto hace que el mapa crezca "a lo ancho" equilibradamente.
            Transform puertaOrigen = conectoresPendientes[0];
            conectoresPendientes.RemoveAt(0);

            // Intentamos poner una pieza nueva
            GameObject prefabElegido = ElegirPiezaAleatoria();
            bool exito = IntentarColocarPieza(prefabElegido, puertaOrigen);

            if (!exito)
            {
                // Si falló (porque chocaba), tapiamos esta puerta
                ColocarMuro(puertaOrigen);
            }
        }

        // 4. EL BARRENDERO: Tapiar todo lo que haya quedado abierto al final
        Debug.Log("Cerrando puertas restantes...");
        foreach (Transform puertaAbierta in conectoresPendientes)
        {
            ColocarMuro(puertaAbierta);
        }
        conectoresPendientes.Clear(); // Limpiamos la lista final

        Debug.Log($"Mapa terminado: {piezasActuales} piezas generadas.");
    }

    bool IntentarColocarPieza(GameObject prefab, Transform puertaOrigen)
    {
        // A. Instanciar temporalmente
        GameObject nuevaPieza = Instantiate(prefab, new Vector3(0, -1000, 0), Quaternion.identity);
        if (worldContainer != null) nuevaPieza.transform.parent = worldContainer;

        // B. Buscar puerta de entrada y alinear
        Transform puertaEntrada = BuscarPrimeraPuerta(nuevaPieza);
        if (puertaEntrada == null) { DestroyImmediate(nuevaPieza); return false; }

        AlinearPieza(nuevaPieza.transform, puertaEntrada, puertaOrigen);

        // --- C. DETECCIÓN DE COLISIONES ---
        // Forzamos a Unity a actualizar las físicas YA (si no, espera al siguiente frame)
        Physics.SyncTransforms();

        if (DetectarColision(nuevaPieza))
        {
            // ¡Choca! Destruimos y devolvemos false (para que el bucle ponga un muro)
            DestroyImmediate(nuevaPieza);
            return false;
        }

        // D. Si no choca, la aceptamos
        FinalizarPieza(nuevaPieza, $"Pieza_{piezasActuales}", puertaEntrada);
        return true;
    }

    void ColocarMuro(Transform puertaOrigen)
    {
        GameObject muro = Instantiate(prefabMuroCierre, new Vector3(0, -1000, 0), Quaternion.identity);
        if (worldContainer != null) muro.transform.parent = worldContainer;
        muro.name = "Muro_Cierre";

        Transform puertaMuro = BuscarPrimeraPuerta(muro);
        if (puertaMuro != null)
        {
            AlinearPieza(muro.transform, puertaMuro, puertaOrigen);
        }

        // Opcional: Destruir el script de puerta del muro si molesta, o dejarlo.
    }

    // --- LÓGICA DE FÍSICA ---
    bool DetectarColision(GameObject pieza)
    {
        // Buscamos el collider de la pieza nueva
        BoxCollider collider = pieza.GetComponent<BoxCollider>();
        if (collider == null) return false; // Si no tiene collider, asumimos que cabe (peligroso)

        // Calculamos el centro y tamaño en coordenadas mundiales
        Vector3 centro = pieza.transform.TransformPoint(collider.center);
        Vector3 tamaño = Vector3.Scale(collider.size, pieza.transform.lossyScale); // Escala global

        // Reducimos un pelín la caja de chequeo para evitar falsos positivos con la pieza a la que nos pegamos
        Vector3 tamañoChequeo = tamaño * 0.95f;

        // Lanzamos la caja imaginaria y vemos qué toca
        Collider[] chocalos = Physics.OverlapBox(centro, tamañoChequeo / 2, pieza.transform.rotation, layerDungeon);

        foreach (Collider c in chocalos)
        {
            // Si choca con algo que NO sea ella misma (ni sus hijos)
            if (c.transform.root != pieza.transform)
            {
                // ¡Ha chocado con otra sala!
                return true;
            }
        }
        return false;
    }

    void FinalizarPieza(GameObject pieza, string nombre, Transform puertaEntradaIgnorar)
    {
        pieza.name = nombre;
        piezasActuales++;

        // Añadimos sus nuevas puertas a la lista de pendientes
        List<Transform> puertas = BuscarTodasLasPuertas(pieza);
        foreach (Transform p in puertas)
        {
            if (p != puertaEntradaIgnorar) // No añadimos la puerta por la que entramos
            {
                conectoresPendientes.Add(p);
            }
        }
    }

    // --- UTILIDADES (Tus funciones de siempre) ---
    void AlinearPieza(Transform piezaRaiz, Transform puertaPieza, Transform puertaDestino)
    {
        Vector3 direccionDestino = -puertaDestino.forward;
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionDestino, Vector3.up);
        float diferenciaY = puertaPieza.localEulerAngles.y;
        piezaRaiz.rotation = rotacionObjetivo * Quaternion.Euler(0, -diferenciaY, 0);

        Vector3 offset = puertaPieza.position - piezaRaiz.position;
        piezaRaiz.position = puertaDestino.position - offset;
    }

    GameObject ElegirPiezaAleatoria()
    {
        bool ponerSala = Random.value > 0.5f; // 50% probabilidad
        if (ponerSala && prefabsSalas.Count > 0) return prefabsSalas[Random.Range(0, prefabsSalas.Count)];
        return prefabsPasillos[Random.Range(0, prefabsPasillos.Count)];
    }

    List<Transform> BuscarTodasLasPuertas(GameObject estructura)
    {
        List<Transform> lista = new List<Transform>();
        foreach (Transform hijo in estructura.GetComponentsInChildren<Transform>())
            if (hijo.CompareTag("Puerta")) lista.Add(hijo);
        return lista;
    }

    Transform BuscarPrimeraPuerta(GameObject estructura)
    {
        foreach (Transform hijo in estructura.GetComponentsInChildren<Transform>())
            if (hijo.CompareTag("Puerta")) return hijo;
        return null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DungeonGenerator script = (DungeonGenerator)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Generar Mapa")) script.GenerarMapaCompleto();
    }
}
#endif