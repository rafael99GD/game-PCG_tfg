using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [Header("Configuración Global")]
    public int globalSeed = 12345;
    public bool randomSeedOnPlay = true;

    [Header("Arrastra tus Chunks aquí")]
    public SimpleVoxelGenerator[] chunks;

    // Usamos Awake para configurar todo ANTES de que los Chunks ejecuten su Start()
    void Awake()
    {
        // 1. Si queremos aleatoriedad, generamos una semilla nueva
        if (randomSeedOnPlay)
        {
            globalSeed = Random.Range(-100000, 100000);
            Debug.Log("Semilla generada: " + globalSeed); // Para que sepas cuál salió
        }

        // 2. Inyectamos la semilla a todos los chunks
        foreach (var chunk in chunks)
        {
            if (chunk != null)
            {
                chunk.useRandomSeed = false; // Importante: anulamos la decisión individual del chunk
                chunk.seed = globalSeed;     // Le imponemos la semilla del Manager

                // Opcional: Aseguramos que compartan escala para que encajen
                // chunk.scale = 0.1f; 
            }
        }
    }
}