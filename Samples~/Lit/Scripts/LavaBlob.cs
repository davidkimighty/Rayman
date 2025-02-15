using System.Collections;
using Rayman;
using UnityEngine;
using Random = UnityEngine.Random;

public class LavaBlob : MonoBehaviour
{
    private static readonly float TopRadius = 0.2f;
    private static readonly float BottomRadius = 0.35f;
    
    [SerializeField] private RaymarchShape raymarchShape;
    [SerializeField] private Transform topLava;
    [SerializeField] private Transform bottomLava;
    [SerializeField] private float squashAmount;
    [SerializeField] private float squashFrequency;
    [SerializeField] private float squashDuration;
    [SerializeField] private Vector2 moveSpeedMinMax;

    private Transform currentTarget;
    private Vector3 targetPoint;
    private Vector3 dir;
    private float moveSpeed;
    private IEnumerator squash;

    private void Start()
    {
        if (Random.value > 0.5f)
            SetTargetPoint(topLava, TopRadius);
        else
            SetTargetPoint(bottomLava, BottomRadius);
    }

    private void Update()
    {
        float dst2Target = Vector3.Distance(transform.position, targetPoint + dir * 0.3f);
        if (dst2Target < 0.01f)
        {
            if (currentTarget == topLava)
                SetTargetPoint(bottomLava, BottomRadius);
            else
                SetTargetPoint(topLava, TopRadius);
            
            if (squash != null)
                StopCoroutine(squash);
            squash = Squash();
            StartCoroutine(squash);
        }
        
        dir = (targetPoint - transform.position).normalized;
        transform.position += dir * (moveSpeed * Time.deltaTime);
        transform.position += dir * (raymarchShape.Size.y * Time.deltaTime);
    }

    private void SetTargetPoint(Transform target, float radius)
    {
        currentTarget = target;
        targetPoint = currentTarget.position + Random.insideUnitSphere * radius;
        moveSpeed = Random.Range(moveSpeedMinMax.x, moveSpeedMinMax.y);
    }

    private IEnumerator Squash()
    {
        float elapsedTime = 0;
        Vector3 startSize = raymarchShape.Size;
        
        while (elapsedTime < squashDuration)
        {
            float s = Mathf.Sin(elapsedTime * squashFrequency);
            float amount = Mathf.Lerp(squashAmount, 0, elapsedTime / squashDuration);
            float squashFactor = Mathf.Lerp(1 - amount, 1 + amount, (s + 1) / 2);
            float squashSize = startSize.z * squashFactor;
            raymarchShape.Size = new Vector3(startSize.x, startSize.y, squashSize);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
