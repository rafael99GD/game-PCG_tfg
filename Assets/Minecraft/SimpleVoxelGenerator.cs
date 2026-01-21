using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SimpleVoxelGenerator : MonoBehaviour
{
    [Header("Configuración del Terreno")]
    public int width = 16;
    public int height = 16;
    public float scale = 0.1f;

    [Header("Sistema de Semillas (Seed)")]
    public int seed = 0;                  // El número mágico que define el mundo
    public bool useRandomSeed = true;     // Si es true, ignora el número de arriba y elige uno al azar

    [Header("Debug & TFG Demo")]
    [Tooltip("Si está activo, borra las caras ocultas. Si no, dibuja todos los bloques completos.")]
    public bool useFaceCulling = true;

    // Datos internos
    private byte[,,] mapData;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    // Desplazamientos calculados por la seed
    private float seedOffsetX;
    private float seedOffsetZ;

    private void Start()
    {
        // Al arrancar, si es aleatorio, elegimos semilla propia
        if (useRandomSeed)
        {
            seed = Random.Range(-100000, 100000);
        }

        Regenerate(); // Llamamos a la función principal
    }

    private void OnValidate()
    {
        // Permite cambiar el checkbox en tiempo real (Play Mode)
        if (mapData != null && Application.isPlaying)
        {
            GenerateMesh(); // Aquí solo regeneramos la malla visual, no los datos del ruido
        }
    }

    // --- NUEVO: Función Pública para regenerar desde fuera (WorldManager) ---
    public void Regenerate()
    {
        // 1. Recalcular los offsets basados en la semilla actual
        System.Random prng = new System.Random(seed);
        seedOffsetX = prng.Next(-100000, 100000);
        seedOffsetZ = prng.Next(-100000, 100000);

        // 2. Generar datos y malla
        GenerateMapData();
        GenerateMesh();
    }

    void GenerateMapData()
    {
        mapData = new byte[width, height, width];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < width; z++)
            {
                // APLICAMOS LA SEED AQUÍ: Sumamos el offset generado
                float xCoord = ((x + transform.position.x) * scale) + seedOffsetX;
                float zCoord = ((z + transform.position.z) * scale) + seedOffsetZ;

                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(xCoord, zCoord) * height);

                for (int y = 0; y < height; y++)
                {
                    if (y <= terrainHeight) mapData[x, y, z] = 1;
                    else mapData[x, y, z] = 0;
                }
            }
        }
    }

    void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    if (mapData[x, y, z] == 1)
                    {
                        CheckAndDrawFace(x, y, z, Vector3.up);
                        CheckAndDrawFace(x, y, z, Vector3.down);
                        CheckAndDrawFace(x, y, z, Vector3.forward);
                        CheckAndDrawFace(x, y, z, Vector3.back);
                        CheckAndDrawFace(x, y, z, Vector3.right);
                        CheckAndDrawFace(x, y, z, Vector3.left);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        if (GetComponent<MeshCollider>())
            GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void CheckAndDrawFace(int x, int y, int z, Vector3 direction)
    {
        if (!useFaceCulling)
        {
            CreateFace(new Vector3(x, y, z), direction);
            return;
        }

        int nx = x + (int)direction.x;
        int ny = y + (int)direction.y;
        int nz = z + (int)direction.z;

        if (nx < 0 || nx >= width || ny < 0 || ny >= height || nz < 0 || nz >= width)
        {
            CreateFace(new Vector3(x, y, z), direction);
        }
        else if (mapData[nx, ny, nz] == 0)
        {
            CreateFace(new Vector3(x, y, z), direction);
        }
    }

    void CreateFace(Vector3 position, Vector3 direction)
    {
        int vCount = vertices.Count;

        vertices.Add(position + GetVertexPos(direction, 0));
        vertices.Add(position + GetVertexPos(direction, 1));
        vertices.Add(position + GetVertexPos(direction, 2));
        vertices.Add(position + GetVertexPos(direction, 3));

        triangles.Add(vCount);
        triangles.Add(vCount + 2);
        triangles.Add(vCount + 1);

        triangles.Add(vCount);
        triangles.Add(vCount + 3);
        triangles.Add(vCount + 2);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));
    }

    Vector3 GetVertexPos(Vector3 dir, int index)
    {
        Vector3[] cubeCorners = {
            new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0),
            new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 0, 1)
        };

        if (dir == Vector3.up) { int[] o = { 2, 6, 5, 1 }; return cubeCorners[o[index]]; }
        if (dir == Vector3.down) { int[] o = { 4, 7, 3, 0 }; return cubeCorners[o[index]]; }
        if (dir == Vector3.forward) { int[] o = { 4, 5, 6, 7 }; return cubeCorners[o[index]]; }
        if (dir == Vector3.back) { int[] o = { 3, 2, 1, 0 }; return cubeCorners[o[index]]; }
        if (dir == Vector3.right) { int[] o = { 3, 7, 6, 2 }; return cubeCorners[o[index]]; }
        if (dir == Vector3.left) { int[] o = { 0, 1, 5, 4 }; return cubeCorners[o[index]]; }

        return Vector3.zero;
    }
}