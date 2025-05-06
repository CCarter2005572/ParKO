using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector3 pointA;
    public Vector3 pointB;
    public float moveSpeed = 2f;

    private bool movingToB = true;

    void Update()
    {
        if (movingToB)
            transform.position = Vector3.MoveTowards(transform.position, pointB, moveSpeed * Time.deltaTime);
        else
            transform.position = Vector3.MoveTowards(transform.position, pointA, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, pointB) < 0.1f)
            movingToB = false;
        if (Vector3.Distance(transform.position, pointA) < 0.1f)
            movingToB = true;
    }
}