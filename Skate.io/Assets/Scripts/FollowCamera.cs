using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target; // the skateboard
    public Vector3 offset = new Vector3(0, 3, -6);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        transform.LookAt(target.position + Vector3.up * 0.5f); // aim slightly above ground
    }
}