using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SkateboardController : MonoBehaviour, Controls.ISkateboardActions
{
    [Header("References")]
    public Transform deckMesh;   // visual mesh of the skateboard (to tilt when carving)

    [Header("Settings")]
    public float pushForce = 10f;
    public float maxSpeed = 15f;
    public float turnSpeed = 5f;     // torque applied for turning
    public float leanAngle = 25f; // how much to tilt visually when turning
    public float groundFriction = 2f;

    private Rigidbody rb;
    private Controls controls;
    private float turnInput;
    private TrickSystem trickSystem;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        trickSystem = new TrickSystem(rb, deckMesh);

        controls = new Controls();
        controls.Skateboard.SetCallbacks(this);
    }

    void OnEnable() => controls.Skateboard.Enable();
    void OnDisable() => controls.Skateboard.Disable();

    void FixedUpdate()
    {
        // Turning
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            // When moving, carve around an imaginary circle
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                // Front truck pivot offset from board center
                float pivotOffset = 0.5f; 
                Vector3 pivot = transform.position + transform.forward * pivotOffset;
                float angle = turnInput * 90f * Time.fixedDeltaTime;
                transform.RotateAround(pivot, Vector3.up, angle);

                // Align velocity along new forward
                rb.linearVelocity = transform.forward * rb.linearVelocity.magnitude;
            }
            else
            {
                // in-place yaw when almost stopped
                float yawSpeed = 30f; // degrees per second
                float angle = turnInput * yawSpeed * Time.fixedDeltaTime;
                transform.Rotate(Vector3.up, angle, Space.World);
            }
        }

        // Limit max speed
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // Convert velocity into local board space
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        // Sideways: kill hard (like rubber wheels resisting slide)
        localVel.x *= (1f - groundFriction * Time.fixedDeltaTime);

        // Forward: barely dampen (simulate rolling resistance)
        localVel.z *= (1f - 0.000000000001f * Time.fixedDeltaTime);

        // Up/down: leave unchanged (gravity handles it)
        rb.linearVelocity = transform.TransformDirection(localVel);

        // Tilt mesh visually
        if (deckMesh != null)
        {
            float targetTilt = -turnInput * leanAngle;
            Quaternion targetRot = Quaternion.Euler(targetTilt, 0f, 0f);
            deckMesh.localRotation = Quaternion.Lerp(deckMesh.localRotation, targetRot, 8f * Time.deltaTime);
        }

        // Show Forces in Scene view
        Vector3 drawPos = transform.position + Vector3.up * 1f;
        Debug.DrawLine(drawPos, drawPos + rb.linearVelocity, Color.green, 0.1f);
        Debug.DrawLine(drawPos, drawPos + transform.forward * 3f, Color.blue, 0.1f);

        trickSystem.UpdatePhysics(Time.fixedDeltaTime);
    }

    // ===== Input System Callbacks =====

    public void OnPush(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector3 pushDir = transform.forward;
            rb.AddForce(pushDir * pushForce, ForceMode.VelocityChange);

            // Clamp max speed
            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    public void OnTurn(InputAction.CallbackContext context)
    {
        turnInput = context.ReadValue<float>();
    }

    public void OnTrickUp(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (trickSystem.Phase == TrickSystem.TrickPhase.Charging && !trickSystem.IsNollie)
                trickSystem.Pop();
            else if (trickSystem.Phase == TrickSystem.TrickPhase.None)
                trickSystem.StartCharge(true); // nollie
        }
        else if (context.canceled)
        {
            if (trickSystem.Phase == TrickSystem.TrickPhase.InAir && !trickSystem.IsNollie)
                trickSystem.Catch();
        }
    }

    public void OnTrickDown(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (trickSystem.Phase == TrickSystem.TrickPhase.Charging && trickSystem.IsNollie)
                trickSystem.Pop();
            else if (trickSystem.Phase == TrickSystem.TrickPhase.None)
                trickSystem.StartCharge(false); // ollie
        }
        else if (context.canceled)
        {
            if (trickSystem.Phase == TrickSystem.TrickPhase.InAir && trickSystem.IsNollie)
                trickSystem.Catch();
        }
    }

    public void OnTrickLeft(InputAction.CallbackContext context)
    {
        if (context.performed)
            trickSystem.ApplyYaw(-1f);
    }

    public void OnTrickRight(InputAction.CallbackContext context)
    {
        if (context.performed)
            trickSystem.ApplyYaw(1f);
    }
}