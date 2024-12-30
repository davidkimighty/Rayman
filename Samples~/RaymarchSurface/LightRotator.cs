using UnityEngine;

public class LightRotator : MonoBehaviour
{
    [SerializeField] private Vector3 _targetDir = new(0, 0, 1);
    [SerializeField] private float _maxRotSpeed = 30f;
    [SerializeField] private float _minRotSpeed = 3f;

    private void Update()
    {
        Vector3 forwardXZ = Vector3.Scale(transform.forward, new Vector3(1, 0, 1));
        float angle = Vector3.Angle(forwardXZ, _targetDir);
        float speed = Mathf.Lerp(_minRotSpeed, _maxRotSpeed, angle / 180f);
        transform.Rotate(0, speed * Time.deltaTime, 0, Space.World);
    }
}
