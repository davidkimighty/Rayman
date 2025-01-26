using System.Collections;
using UnityEngine;

public class PingPong : MonoBehaviour
{
    [SerializeField] private Transform point1;
    [SerializeField] private Transform point2;
    [SerializeField] private float moveDuration = 3f;
    [SerializeField] private AnimationCurve moveCurve;

    private void Start()
    {
        StartCoroutine(Move2Point());
    }
    
    private IEnumerator Move2Point()
    {
        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        Transform target = point1;
        
        while (true)
        {
            while (elapsedTime < moveDuration)
            {
                transform.position = Vector3.Lerp(startPos, target.position, moveCurve.Evaluate(elapsedTime / moveDuration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = target.position;
            startPos = target.position;
            target = target == point1 ? point2 : point1;
            elapsedTime = 0f;
        }
    }
}
