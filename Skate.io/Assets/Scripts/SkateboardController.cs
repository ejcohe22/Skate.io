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

    [Header("Trick Settings")]
    public float maxChargeTime = 1.5f;
    public float flipTorque = 8f;
    public float spinTorque = 4f;
    public float levelForce = 4f;
    public float holdLevelForce = 2f;
    public float airDamping = 0.995f;
    public float basePopForce = 8f;

    private Rigidbody rb;
    private Controls controls;
    private float turnInput;
    private TrickSystem trickSystem;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        trickSystem = new TrickSystem(rb, deckMesh);
        trickSystem.maxChargeTime = maxChargeTime;
        trickSystem.flipTorque = flipTorque;
        trickSystem.spinTorque = spinTorque;
        trickSystem.levelForce = levelForce;
        trickSystem.holdLevelForce = holdLevelForce;
        trickSystem.airDamping = airDamping;
        trickSystem.basePopForce = basePopForce;

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
        localVel.z *= 1f;

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

        trickSystem.Tick(Time.fixedDeltaTime);
    }

    // ===== Input System Callbacks =====

    public void OnPush(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector3 pushDir = transform.forward;
            rb.AddForce(pushDir * pushForce, ForceMode.Impulse);

            // Clamp max speed
            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    public void OnTurn(InputAction.CallbackContext context)
    {
        turnInput = context.ReadValue<float>();
    }

    public void OnTrickUp(InputAction.CallbackContext context) => ForwardTrickInput(context, TrickSystem.TrickInput.UpPress, TrickSystem.TrickInput.UpHold, TrickSystem.TrickInput.UpRelease);
    public void OnTrickDown(InputAction.CallbackContext context) => ForwardTrickInput(context, TrickSystem.TrickInput.DownPress, TrickSystem.TrickInput.DownHold, TrickSystem.TrickInput.DownRelease);
    public void OnTrickLeft(InputAction.CallbackContext context) => ForwardTrickInput(context, TrickSystem.TrickInput.LeftPress, TrickSystem.TrickInput.LeftHold, TrickSystem.TrickInput.LeftRelease);
    public void OnTrickRight(InputAction.CallbackContext context) => ForwardTrickInput(context, TrickSystem.TrickInput.RightPress, TrickSystem.TrickInput.RightHold, TrickSystem.TrickInput.RightRelease);

    private void ForwardTrickInput(InputAction.CallbackContext context, TrickSystem.TrickInput press, TrickSystem.TrickInput hold, TrickSystem.TrickInput release)
    {
        if (context.started) trickSystem.OnInput(press);
        if (context.performed) trickSystem.OnInput(hold);
        if (context.canceled) trickSystem.OnInput(release);
    }
}