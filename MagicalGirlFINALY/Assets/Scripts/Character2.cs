using UnityEngine;

public partial class Character : MonoBehaviour
{
    private void CheckIsGrounded()
    {
        if (state.dead || state.Stunned) return;

        var mask = state.dropping ? platformLayerDrop : platformLayer;

        var rayDist = groundRange + groundCheckHeight;

        var groundHit = false;
        RaycastHit hit;

        foreach (var feet in CurrentBattleModel.Foots)
        {
            Vector3 transformedFeetPos =
                transform.position - Vector3.right * 0.5f - Vector3.up * (1 - groundCheckHeight);
            transformedFeetPos.x = feet.position.x;
            groundHit = Physics.Raycast(
                transformedFeetPos, Vector3.down, out hit,
                rayDist, mask);
            if (groundHit) break;
        }

        if (Velocity.y > 0) groundHit = false;
        if (groundHit)
        {
            state.groundFrames = groundFrames;
            if (!state.grounded)
            {
                OnTouchGround();
            }
        }
        else if (state.grounded || state.ledged)
        {
            OnAirborne();
        }
    }

    private void InitStats()
    {
        if (CurrentFrameData) frameDataDict = CurrentFrameData.MakeDictionary();

        normalModel.ResetHitboxes();
        transformedModel.ResetHitboxes();
        normalModel.ResetFx();
        transformedModel.ResetFx();

        state.grounded = false;
        airJumpsLeft = maxAirJumps;

        gravityMultiplier = 1f;
        rb.velocity = Vector3.zero;

        state.ResetStates();

        CumulDamage = 0;
        OnPercentChanged?.Invoke(0, 0);
        if (!firstTransform)
            CumulUltimate = 0;

        useVelocityFrames = 0;
        hasMoved = false;

        normalModel.Show(true);
        transformedModel.Show(false);
    }

    public void ApplyPlayerOptions(GameManager.PlayerOptions options, GameManager.PlayerData data)
    {
        normalModel = Instantiate(options.NormalModel, ModelParent);
        //normalModel.transform.localPosition = Vector3.zero;
        normalModel.gameObject.name = "NormalModel";

        transformedModel = Instantiate(options.TransformedModel, ModelParent);
        //transformedModel.transform.localPosition = Vector3.zero;
        transformedModel.gameObject.name = "TransformedModel";

        Transformation(false);
        normalModel.ChangeColor(data.color);
        firstTransform = true;
    }

    private void CheckLedging()
    {
        if (CannotInput || state.grounded) return;

        if (controller.StickInput.x == 0) return;

        if (state.ledged && controller.StickInput.y == 0) return;

        var dir = (Vector3.right * controller.StickInput.x).normalized;


        var rayDist = groundRange + groundCheckHeight;
        var ledgeHit = false;
        RaycastHit hit;

        Vector3 transformedFeetPosL = transform.position - Vector3.up * 0.5f + dir * ((1 - groundCheckHeight) * 0.5f);
        transformedFeetPosL.x = CurrentBattleModel.Body.position.x;
        ledgeHit = Physics.Raycast(transformedFeetPosL,
            dir, out hit,
            rayDist, platformLayer);
        if (!ledgeHit)
        {
            Vector3 transformedFeetPosR =
                transform.position + Vector3.up * 0.5f + dir * ((1 - groundCheckHeight) * 0.5f);
            transformedFeetPosR.x = CurrentBattleModel.Body.position.x;
            ledgeHit = Physics.Raycast(transformedFeetPosR,
                dir, out hit,
                rayDist, platformLayer);
        }

        if (ledgeHit)
        {
            if (!state.ledged)
            {
                state.ledgeDirection = dir;
                OnLedgeTouch();
            }

            state.ledgeFrames = ledgeFrames;
        }
        else if (!state.grounded)
        {
            OnAirborne();
        }
    }

    private void Drop()
    {
        if (CannotInput) return;

        if (controller.StickInput.y < -0.5f) state.dropFrames = dropFrames;
    }

    public void TakeHit(HitData data)
    {
        if (state.Invulnerable || state.dead || state.shielded) return;

        state.startup = 0;
        state.active = 0;
        state.recovering = 0;

        gravityMultiplier = 1f;

        var prev = (int)CumulDamage;
        CumulDamage += data.damage;
        OnPercentChanged?.Invoke(prev, (int)CumulDamage);

        state.maxStunDuration = data.maxStunDuration;
        state.stunDuration += data.stunDuration;
        if (state.stunDuration > state.maxStunDuration) state.stunDuration = state.maxStunDuration;

        rb.velocity = Vector3.zero;

        CurrentAnimator.Play("Hit");
        normalModel.ResetHitboxes();
        transformedModel.ResetHitboxes();
        normalModel.ResetFx();
        transformedModel.ResetFx();

        var force = data.force;
        if (!data.fixedForce) force *= CumulDamage * 0.01f;

        rb.AddForce(data.direction * force, ForceMode.VelocityChange); //multiply by percentDamage
        OnTakeDamage?.Invoke(force);

        useVelocityFrames = data.useVelocityDuration;
        hasMoved = false;
    }
}