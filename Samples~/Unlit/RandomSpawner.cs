using System.Collections;
using Rayman;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSpawner : MonoBehaviour
{
    [SerializeField] private ShapeElement shapePrefab;
    [SerializeField] private Vector3 spawnArea = new(10f, 10f, 10f);
    [SerializeField] private int spawnCount = 1000;
    [SerializeField] private Vector3 minSize = new(0.1f, 0.1f, 0.1f);
    [SerializeField] private Vector3 maxSize = new(0.3f, 0.3f, 0.3f);
    [SerializeField] private RaymarchRenderer raymarchRenderer;
    [SerializeField] private RaymarchObject raymarchGroup;
    [SerializeField] private int spawnPerFrame = 50;

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
            ShapeElement shape = Instantiate(shapePrefab, transform.position + randomPos, Quaternion.identity);
            
            Vector3 randomSize = new Vector3(
                Random.Range(minSize.x, maxSize.x),
                Random.Range(minSize.y, maxSize.y),
                Random.Range(minSize.z, maxSize.z));
            shape.Size = randomSize;
            
            Color randomColor = Random.ColorHSV();
            randomColor.a = shape.Color.a;
            shape.Color = randomColor;
            
            ((IRaymarchElementControl)raymarchGroup).AddEntity(shape);
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
