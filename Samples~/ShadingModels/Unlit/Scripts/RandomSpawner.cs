using System.Collections;
using Rayman;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSpawner : MonoBehaviour
{
    [SerializeField] private RaymarchRenderer raymarchRenderer;
    [SerializeField] private ShapeObject shapeObject;
    [SerializeField] private GameObject shapePrefab;
    [SerializeField] private Vector3 spawnArea = new(10f, 10f, 10f);
    [SerializeField] private int spawnCount = 1000;
    [SerializeField] private int spawnPerFrame = 50;
    [SerializeField] private Vector3 minSize = new(0.1f, 0.1f, 0.1f);
    [SerializeField] private Vector3 maxSize = new(0.3f, 0.3f, 0.3f);

    private int currentCount = 0;
    
    private void Start()
    {
        StartCoroutine(SpawnShapes());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, spawnArea);
    }

    private IEnumerator SpawnShapes()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
                Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f),
                Random.Range(-spawnArea.z / 2f, spawnArea.z / 2f));
            GameObject shape = Instantiate(shapePrefab, transform.position + randomPos, Quaternion.identity);
            
            ShapeProvider shapeProvider = shape.GetComponent<ShapeProvider>();
            Vector3 randomSize = new Vector3(
                Random.Range(minSize.x, maxSize.x),
                Random.Range(minSize.y, maxSize.y),
                Random.Range(minSize.z, maxSize.z));
            shapeProvider.Size = randomSize;
            
            ColorProvider colorProvider = shapeProvider.GetComponent<ColorProvider>();
            Color randomColor = Random.ColorHSV();
            colorProvider.Color = randomColor;
            
            shapeObject.AddShape(shapeProvider);
            currentCount++;

            if (currentCount > spawnPerFrame)
            {
                currentCount = 0;
                yield return null;                
            }
        }
        raymarchRenderer.Setup();
    }
}
