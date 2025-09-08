using UnityEngine;

public class TrickSystem
{
    public enum TrickPhase { None, Charging, InAir }
    public enum TrickInput { UpPress, UpHold, UpRelease, DownPress, DownHold, DownRelease, LeftPress, LeftHold, LeftRelease, RightPress, RightHold, RightRelease }

    public TrickPhase Phase { get; private set; } = TrickPhase.None;
    public bool IsNollie { get; private set; } = false;

    private Rigidbody boardRb;
    private Transform boardTransform;

    private float chargeTime;
    private float yawCharge;

    private float accumulatedYaw;
    private float accumulatedFlip;

    public struct TrickInfo { public int shuvits; public int kickflips; }
    public TrickInfo LastTrick { get; private set; }

    // Tunables
    public float maxChargeTime = 2f;
    public float basePopForce = 2.5f;
    public float flipTorque = 2f;
    public float spinTorque = 2f;
    public float levelForce = 3f;
    public float holdLevelForce = 2f;
    public float airDamping = 0.995f;
    public float popOffset = 0.22f;
    public float groundCheckDist = .06f;
    public LayerMask groundMask = ~0; // all layers

    public TrickSystem(Rigidbody rb, Transform t)
    {
        boardRb = rb;
        boardTransform = t;
    }

    public void Tick(float dt)
    {
        if (IsGrounded() && Phase != TrickPhase.Charging) Phase = TrickPhase.None;
        if (Phase == TrickPhase.Charging) chargeTime = Mathf.Min(chargeTime + dt, maxChargeTime);
        if (Phase == TrickPhase.InAir)
        {
            // Air damping
            boardRb.angularVelocity *= airDamping;
            accumulatedYaw += Vector3.Dot(boardRb.angularVelocity, Vector3.up) * Mathf.Rad2Deg * dt;
        }
    }

    public void OnInput(TrickInput input)
    {
        //if (IsGrounded() && Phase != TrickPhase.Charging) Phase = TrickPhase.None;
        switch (Phase)
        {
            case TrickPhase.None:
                if (input == TrickInput.UpPress) StartCharge(true); // Nollie
                if (input == TrickInput.DownPress) StartCharge(false); // Ollie
                break;

            case TrickPhase.Charging:
                // Start "real trick"
                // ollie
                if (input == TrickInput.DownRelease && !IsNollie) Pop();
                // nollie
                if (input == TrickInput.UpRelease && IsNollie) Pop();

                // prep shuv
                if (input == TrickInput.LeftPress) AddYawCharge(-1f);
                if (input == TrickInput.RightPress) AddYawCharge(+1f);

                // allow early kickflip for skill sake
                if (input == TrickInput.LeftHold) ApplyFlip(+1f, Time.fixedDeltaTime);
                if (input == TrickInput.RightHold) ApplyFlip(-1f, Time.fixedDeltaTime);
                break;

            case TrickPhase.InAir:
                // Level out
                // ollie
                if (input == TrickInput.UpPress && !IsNollie) { Level(); }
                //nollie
                if (input == TrickInput.DownPress && IsNollie) { Level(); }
                
                // kickflip / heelflip
                if (input == TrickInput.LeftHold) ApplyFlip(+1f, Time.fixedDeltaTime);
                if (input == TrickInput.RightHold) ApplyFlip(-1f, Time.fixedDeltaTime);
                break;
        }

        Log($"Handled input {input} in phase {Phase}");
    }

    private bool IsGrounded()
    {
        bool val = Physics.Raycast(boardTransform.position, Vector3.down, groundCheckDist, groundMask);

        if (val) Log("Grounded");
        return val;
    }

    public void StartCharge(bool nollie)
    {
        if (Phase != TrickPhase.None) return;
        Phase = TrickPhase.Charging;
        IsNollie = nollie;
        chargeTime = yawCharge = 0f;
        Log($"StartCharge {(nollie ? "Nollie" : "Ollie")}");
    }

    public void AddYawCharge(float dir)
    {
        yawCharge += dir;
        Log($"AddYawCharge {dir} → {yawCharge}");
    }

    public void Pop()
    {
        if (Phase != TrickPhase.Charging) return;

        float popForce = basePopForce * (1f + chargeTime / maxChargeTime);

        // Pop offset: nose up for ollie, tail up for nollie
        Vector3 popPoint = boardTransform.position + (boardTransform.forward * popOffset * (IsNollie ? -1f : 1f));
        
        // Apply upward force at the pop point to simulate lift
        boardRb.AddForceAtPosition(Vector3.up * popForce, popPoint, ForceMode.VelocityChange);

        // Seesaw torque with level mechanic to simulate the snap
        Vector3 seesawAxis = boardTransform.up; 
        float snapTorque = levelForce * (IsNollie ? 1f : -1f);
        boardRb.AddTorque(seesawAxis * snapTorque, ForceMode.VelocityChange);

        accumulatedYaw = accumulatedFlip = 0f;
        Phase = TrickPhase.InAir;
        // Apply yaw/spin torque if charged
        if (Mathf.Abs(yawCharge) > 0.01f)
            boardRb.AddTorque(Vector3.up * yawCharge * spinTorque, ForceMode.VelocityChange);

        Log("Pop executed → InAir");
    }

    public void Level()
    {
        // Leveling is secondary rotation to even out the board
        if (Phase != TrickPhase.InAir) return;

        Vector3 levelPoint = boardTransform.position + (boardTransform.forward * popOffset * (IsNollie ? 1f : -1f));
        boardRb.AddForceAtPosition(Vector3.up * levelForce, levelPoint, ForceMode.VelocityChange);
        boardRb.AddTorque(boardTransform.up * levelForce * (IsNollie ? -1f : 1f), ForceMode.VelocityChange);
        Log("Leveled out");
    }
    /**
    public void Pop()
    {
        if (Phase != TrickPhase.Charging) return;
        float popForce = basePopForce * (1f + chargeTime / maxChargeTime);
        Vector3 popPoint = IsNollie
            ? boardTransform.position - boardTransform.forward * popOffset
            : boardTransform.position + boardTransform.forward * popOffset;

        boardRb.AddForceAtPosition(Vector3.up * popForce, popPoint, ForceMode.VelocityChange);
        if (Mathf.Abs(yawCharge) > 0.01f)
            boardRb.AddTorque(Vector3.up * yawCharge * spinTorque, ForceMode.VelocityChange);

        accumulatedYaw = accumulatedFlip = 0f;
        Phase = TrickPhase.InAir;
        Log("Pop executed → InAir");
    }

    public void Level()
    {
        Vector3 popPoint = IsNollie
            ? boardTransform.position + boardTransform.forward * popOffset
            : boardTransform.position - boardTransform.forward * popOffset;

        boardRb.AddForceAtPosition(Vector3.up * levelForce, popPoint, ForceMode.VelocityChange);
        Log($"LevelBurst");
    }*/

    public void ApplyFlip(float dir, float dt)
    {
        // Use the board's right axis for flip tricks (like a football spiraling)
        boardRb.AddTorque(boardTransform.right * dir * flipTorque, ForceMode.VelocityChange);

        // Track accumulated flip angle around the right axis
        accumulatedFlip += Vector3.Dot(boardRb.angularVelocity, boardTransform.right) * Mathf.Rad2Deg * dt;

        Log($"ApplyFlip {dir}");
    }

    public void Catch()
    {
        if (Phase != TrickPhase.InAir) return;
        LastTrick = new TrickInfo
        {
            shuvits = Mathf.RoundToInt(accumulatedYaw / 180f),
            kickflips = Mathf.RoundToInt(accumulatedFlip / 360f)
        };
        boardRb.angularVelocity *= 0.2f;
        Phase = TrickPhase.None;
        Log($"Catch → shuvits:{LastTrick.shuvits}, kickflips:{LastTrick.kickflips}");
    }

    public void Reset()
    {
        Phase = TrickPhase.None;
        IsNollie = false;
        chargeTime = yawCharge = 0f;
        accumulatedYaw = accumulatedFlip = 0f;
    }

    private void Log(string msg) => Debug.Log($"[TrickSystem] {msg}");
}