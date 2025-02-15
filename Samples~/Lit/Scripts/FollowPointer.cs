using UnityEngine;
using UnityEngine.InputSystem;

public class FollowPointer : MonoBehaviour
{
    [SerializeField] private InputActionReference mouseActionRef;
    [SerializeField] private Transform followObject;
    [SerializeField] private float followSpeedFactor = 0.1f;

    private void Start()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector3 mousePoint = mouseActionRef.action.ReadValue<Vector2>();
        mousePoint.z = -Camera.main.transform.position.z;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(mousePoint);
        followObject.position = Vector2.Lerp(followObject.position, mousePos, followSpeedFactor);
    }
}
