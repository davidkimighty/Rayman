using UnityEngine;
using Random = UnityEngine.Random;

namespace Rayman
{
    public class ObjectRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 _targetDir = new(0, 0, 1);
        [SerializeField] private Vector3 _rotateAxis = Vector3.one;
        [SerializeField] private float _maxRotSpeed = 30f;
        [SerializeField] private float _minRotSpeed = 3f;
        [SerializeField] private float _randomRot = 0f;

        private Vector3 randomRot;
    
        private void Start()
        {
            randomRot = new Vector3(Random.Range(0, _randomRot), Random.Range(0, _randomRot), Random.Range(0, _randomRot));
        }

        private void Update()
        {
            Vector3 forwardXZ = Vector3.Scale(transform.forward, new Vector3(1, 0, 1));
            float angle = Vector3.Angle(forwardXZ, _targetDir);
            float speed = Mathf.Lerp(_minRotSpeed, _maxRotSpeed, angle / 180f);
            Vector3 rot = Vector3.Scale((Vector3.one * speed + randomRot) * Time.deltaTime, _rotateAxis);
            transform.Rotate(rot, Space.World);
        }
    }
}
