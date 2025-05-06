using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public bool useOffsetValues = false;
    public float rotateSpeed = 3f;
    public Transform pivot;
    public float smoothSpeed = 5f;

    void Start()
    {
        if (!useOffsetValues)
        {
            offset = new Vector3(0, 2, -5);
        }
    }

    void LateUpdate()
    {
        if (target == null || pivot == null)
            return;

        // Rotate the target based on mouse X
        float horizontal = Input.GetAxis("Mouse X") * rotateSpeed;
        target.Rotate(0, horizontal, 0);

        // Rotate the pivot (vertical look)
        float vertical = Input.GetAxis("Mouse Y") * rotateSpeed;
        pivot.Rotate(-vertical, 0, 0);

        // Clamp vertical rotation
        float pivotAngle = pivot.eulerAngles.x;
        if (pivotAngle > 180f) pivotAngle -= 360f;
        pivotAngle = Mathf.Clamp(pivotAngle, -30f, 30f);
        pivot.localEulerAngles = new Vector3(pivotAngle, 0, 0);

        // Calculate new camera position
        Quaternion rotation = Quaternion.Euler(pivot.eulerAngles.x, target.eulerAngles.y, 0);
        Vector3 targetPosition = target.position - (rotation * offset);

        // Obstacle check with SphereCast
        RaycastHit hit;
        Vector3 direction = (targetPosition - target.position).normalized;
        float distance = Vector3.Distance(target.position, targetPosition);

        if (Physics.SphereCast(target.position, 0.5f, direction, out hit, distance))
        {
            targetPosition = hit.point + hit.normal * 0.5f;
        }

        // Prevent camera from going below ground
        float minY = target.position.y + 1f;
        if (targetPosition.y < minY)
        {
            targetPosition.y = minY;
        }

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.LookAt(target);
    }
}
