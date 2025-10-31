using UnityEngine;

public class Test : MonoBehaviour
{
    [Header("Prefab to spawn")]
    public GameObject grassTilePrefab;

    [Header("Grid settings")]
    public int floorSizeX = 50;
    public int floorSizeZ = 50;
    public float tileSpacing = 39.0f;
    public float baseHeight = -10.0f;

    private void Start()
    {
        if (grassTilePrefab == null)
        {
            Debug.LogError("GrassTilePrefab is not assigned!");
            return;
        }

        int tilesAmount = 0;

        for (int x = -(floorSizeX / 2); x < floorSizeX / 2; x++)
        {
            for (int z = -(floorSizeZ / 2); z < floorSizeZ / 2; z++)
            {
                // Compute position similar to glm::vec3(x * tileSpacing, -10, z * tileSpacing)
                Vector3 position = new Vector3(x * tileSpacing, baseHeight, z * tileSpacing);

                // Random Y rotation (0–360 degrees)
                Quaternion rotation = Quaternion.Euler(-90f, 0, 0f);

                // Instantiate prefab at position and rotation
                Instantiate(grassTilePrefab, position, rotation, transform);

                tilesAmount++;
            }
        }

        Debug.Log($"Spawned {tilesAmount} grass tiles.");
    }
}
