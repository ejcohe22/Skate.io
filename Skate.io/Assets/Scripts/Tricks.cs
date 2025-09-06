using UnityEngine;

/// <summary>
/// Physics-driven skateboard trick system: handles ollie/nollie hops,
/// kickflips, shoveits, and landing skill checks.
/// </summary>
public class TrickSystem
{
    public enum TrickPhase
    {
        None,
        Charging,
        Popping,
        InAir,
        Catching
    }

    public TrickPhase Phase { get; private set; } = TrickPhase.None;
    public bool IsNollie { get; private set; } = false;

    private Rigidbody boardRb;
    private Transform deckMesh;
    private Transform boardTransform;

    private float chargeTime;

    private const float maxChargeTime = 0.5f;
    private const float basePopForce = 5f;
    private const float flipTorque = 8f;
    private const float spinTorque = 6f;
    private const float catchSkillThreshold = 0.85f;

    public TrickSystem(Rigidbody boardRb, Transform deckMesh)
    {
        this.boardRb = boardRb;
        this.deckMesh = deckMesh;
        this.boardTransform = boardRb.transform;
    }

    #region Trick Control

    public void StartCharge(bool nollie)
    {
        if (Phase != TrickPhase.None) return;

        Phase = TrickPhase.Charging;
        IsNollie = nollie;
        chargeTime = 0f;
    }

    public void UpdateCharge(float deltaTime)
    {
        if (Phase != TrickPhase.Charging) return;
        chargeTime += deltaTime;
        if (chargeTime > maxChargeTime) chargeTime = maxChargeTime;
    }

    public void Pop()
    {
        if (Phase != TrickPhase.Charging) return;

        Phase = TrickPhase.Popping;

        float popForce = basePopForce * (1f + chargeTime / maxChargeTime);

        if (IsNollie)
            PopNollie(popForce);
        else
            PopOllie(popForce);

        Phase = TrickPhase.InAir;
    }

    public void ApplyRoll(float dir = 1f)
    {
        if (Phase != TrickPhase.InAir) return;

        // Roll along board's forward axis
        boardRb.AddTorque(boardTransform.forward * dir * flipTorque, ForceMode.VelocityChange);
    }

    public void ApplyYaw(float dir = 1f)
    {
        if (Phase != TrickPhase.InAir) return;

        // Yaw spin
        boardRb.AddTorque(Vector3.up * dir * spinTorque, ForceMode.VelocityChange);
    }

    public void UpdatePhysics(float deltaTime)
    {
        if (Phase == TrickPhase.InAir)
        {
            // Optional: damp angular velocity slightly in air
            boardRb.angularVelocity *= 0.995f;
        }
    }

    public void Catch()
    {
        if (Phase != TrickPhase.InAir) return;

        Phase = TrickPhase.Catching;

        if (CanCatch())
        {
            // Successful landing: damp rotation and velocity
            boardRb.angularVelocity *= 0.2f;
            boardRb.linearVelocity *= 0.8f;
        }
        else
        {
            // Failed catch: leave physics alone, board may flip
        }

        Reset();
    }

    public void Reset()
    {
        Phase = TrickPhase.None;
        IsNollie = false;
        chargeTime = 0f;
    }

    #endregion

    #region Private Helpers

    private void PopOllie(float force)
    {
        // Front truck pop
        Vector3 popPoint = boardTransform.position + boardTransform.forward * 0.25f;
        boardRb.AddForceAtPosition(Vector3.up * force, popPoint, ForceMode.VelocityChange);
    }

    private void PopNollie(float force)
    {
        // Back truck pop
        Vector3 popPoint = boardTransform.position - boardTransform.forward * 0.25f;
        boardRb.AddForceAtPosition(Vector3.up * force, popPoint, ForceMode.VelocityChange);
    }

    private bool CanCatch()
    {
        if (deckMesh == null) return true;

        float alignment = Vector3.Dot(deckMesh.up, Vector3.up);
        return alignment >= catchSkillThreshold;
    }

    #endregion
}