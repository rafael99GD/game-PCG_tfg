using UnityEngine;
using UnityEngine.Tilemaps; // ¡Importante para usar Tilemaps!

public class TerrariaGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public Tilemap tilemap;
    public TileBase grassTile;
    public TileBase dirtTile;
    public TileBase stoneTile;

    [Header("Dimensiones del Mundo")]
    public int width = 100;
    public int height = 100;

    [Header("Configuración de Generación")]
    public int seed = 0;
    public bool randomSeed = true;

    [Header("Superficie (Ruido 1D)")]
    public float surfaceScale = 0.1f;   // Frecuencia de las colinas
    public int heightMultiplier = 15;   // Altura de las colinas
    public int groundLevel = 60;        // A qué altura empieza el suelo

    [Header("Cuevas (Ruido 2D)")]
    public float caveScale = 0.05f;     // Tamaño de los agujeros
    [Range(0f, 1f)]
    public float caveThreshold = 0.5f;  // Cuanto mayor, más cuevas

    private void Start()
    {
        if (randomSeed) seed = Random.Range(-10000, 10000);
        GenerateWorld();
    }

    private void Update()
    {
        // Solo para probar rápido en el editor: tecla G para regenerar
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (randomSeed) seed = Random.Range(-10000, 10000);
            tilemap.ClearAllTiles(); // Borrar mapa anterior
            GenerateWorld();
        }
    }

    void GenerateWorld()
    {
        // 1. Recorremos el ancho del mapa (Eje X)
        for (int x = 0; x < width; x++)
        {
            // 2. Calculamos la altura de la superficie en este punto X
            float xCoord = (x + seed) * surfaceScale;
            int surfaceHeight = groundLevel + Mathf.FloorToInt(Mathf.PerlinNoise(xCoord, 0) * heightMultiplier);

            // 3. Rellenamos desde el fondo (0) hasta la altura calculada
            for (int y = 0; y < surfaceHeight; y++)
            {
                // -- Lógica de Cuevas --
                // Usamos ruido 2D (X e Y) para saber si aquí hay un agujero
                float caveNoise = Mathf.PerlinNoise((x + seed) * caveScale, (y + seed) * caveScale);

                // Si el valor del ruido supera el límite, es un hueco (no ponemos bloque)
                if (caveNoise > caveThreshold)
                {
                    continue; // Saltar a la siguiente iteración (dejar vacío)
                }

                // -- Elección del Bloque --
                TileBase tileToPlace;

                if (y == surfaceHeight - 1) // Es el bloque más alto
                {
                    tileToPlace = grassTile;
                }
                else if (y < surfaceHeight - 15) // Profundidad (Piedra)
                {
                    tileToPlace = stoneTile;
                }
                else // Capa intermedia (Tierra)
                {
                    tileToPlace = dirtTile;
                }

                // -- Colocar en el Tilemap --
                tilemap.SetTile(new Vector3Int(x, y, 0), tileToPlace);
            }
        }
    }
}