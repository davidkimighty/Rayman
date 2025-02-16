using System.Collections;
using Rayman;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSpawner : MonoBehaviour
{
    [SerializeField] private ColorShape shapePrefab;
    [SerializeField] private int count = 500;
    [SerializeField] private Vector3 bounds = new(10f, 10f, 10f);
    [SerializeField] private RaymarchRenderer raymarchRenderer;
    [SerializeField] private RaymarchGroup raymarchGroup;

    private void Start()
    {
        StartCoroutine(SpawnShapes());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, bounds);
    }

    private IEnumerator SpawnShapes()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-bounds.x / 2f, bounds.x / 2f),
                Random.Range(-bounds.y / 2f, bounds.y / 2f),
                Random.Range(-bounds.z / 2f, bounds.z / 2f)
            );
            
            ColorShape shape = Instantiate(shapePrefab, transform.position + randomPos, Quaternion.identity);
            Color randomColor = Random.ColorHSV();
            randomColor.a = shape.Color.a;
            shape.Color = randomColor;
            raymarchGroup.AddEntity(shape);
            yield return null;
        }
        raymarchRenderer.Setup();
    }
}
