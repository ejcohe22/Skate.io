using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Rigidbody rb; // assign skateboard’s rigidbody
    public Vector3 offset = new Vector3(0, 3, -6);
    public float smoothSpeed = 5f;
    public float lookHeight = 0.5f;

    void LateUpdate()
    {
        if (target == null || rb == null) return;

        // --- Find desired facing direction ---
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0; // ignore vertical motion

        Vector3 forwardDir;
        if (velocity.sqrMagnitude > 0.1f)
        {
            forwardDir = velocity.normalized; // face where we're going
        }
        else
        {
            forwardDir = target.forward; // fallback to board facing
            Vector3 localOffset = new Vector3(4, 0, 0);
            forwardDir = target.TransformDirection(localOffset + Vector3.forward);
        }

        Quaternion lookRot = Quaternion.LookRotation(forwardDir, Vector3.up);

        // --- Desired camera position ---
        Vector3 desiredPos = target.position + lookRot * offset;

        // --- Smooth follow ---
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // --- Look slightly above board ---
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}