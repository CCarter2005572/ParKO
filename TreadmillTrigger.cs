using UnityEngine;

public class TreadmillTrigger : MonoBehaviour
{
    public Vector3 pushDirection = Vector3.right;
    public float pushStrength = 5f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetExternalForce(pushDirection.normalized * pushStrength);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetExternalForce(Vector3.zero);
            }
        }
    }
}