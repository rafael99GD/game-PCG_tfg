using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [Header("Configuración Global")]
    public int globalSeed = 12345;

    [Tooltip("Si está marcado, genera un mundo nuevo al azar al pulsar Play o G.\nSi está desmarcado, usa siempre el número escrito arriba.")]
    public bool randomSeedOnPlay = true;

    [Header("Arrastra tus Chunks aquí")]
    public SimpleVoxelGenerator[] chunks;

    void Awake()
    {
        // Al iniciar el juego, generamos el mundo
        GenerateNewWorld();
    }

    private void Update()
    {
        // Si pulsamos G, regeneramos según la configuración del checkbox
        if (Input.GetKeyDown(KeyCode.G))
        {
            GenerateNewWorld();
        }
    }

    void GenerateNewWorld()
    {
        // --- CORRECCIÓN AQUÍ ---

        // 1. Solo cambiamos la semilla SI el checkbox está activado
        if (randomSeedOnPlay)
        {
            globalSeed = Random.Range(-100000, 100000);
            Debug.Log("Semilla Aleatoria Generada: " + globalSeed);
        }
        else
        {
            Debug.Log("Usando Semilla Fija: " + globalSeed);
        }

        // 2. Inyectamos la semilla (sea nueva o fija) a todos los chunks
        foreach (var chunk in chunks)
        {
            if (chunk != null)
            {
                chunk.useRandomSeed = false; // El Manager manda, anulamos el random individual
                chunk.seed = globalSeed;     // Le pasamos el número
                chunk.Regenerate();          // ¡Orden de reconstruir!
            }
        }
    }
}