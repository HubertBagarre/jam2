using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Character : MonoBehaviour
{
    [ContextMenu("Transform (true)")]
    private void TransformT()
    {
        Transformation(true);
    }

    [ContextMenu("Transform (false)")]
    private void TransformF()
    {
        Transformation(false);
    }

    private void Transformation(bool transformed)
    {
        Debug.Log($"Transformation : {transformed}");


        if (CurrentFrameData) frameDataDict = CurrentFrameData.MakeDictionary();

        frameDataDict.TryGetValue("Transformation", out var frameData);

        OnActive += SwitchModel;

        TransformedAnimator.CrossFade(frameData.AnimationName, 0.1f);
        NormalAnimator.CrossFade(frameData.AnimationName, 0.1f);

        state.startup = frameData.Startup;
        state.active = frameData.Active;
        state.recovering = frameData.Recovery;
        if (transformed) state.transformedFrames = transformationFrames + state.totalFrames;

        return;

        void SwitchModel(int i)
        {
            if (i != 1) return;
            normalModel.Show(!transformed);
            transformedModel.Show(transformed);
            state.isTransformed = transformed;
        }
    }

    public void Respawn()
    {
        InitStats();
        state.dead = false;
        state.invulFrames = (int)(respawnInvulSeconds * 60);
    }

    public void Shield()
    {
        Debug.Log("Shield");
        if (CannotInput || state.totalActiveFrames > 0) return;
        frameDataDict.TryGetValue("Shield", out var frameData);

        PlayAnimation(frameData, 0.05f);
        OnStartup += GainShield;
        OnActive += LostShield;
    }

    private void GainShield(int i)
    {
        if (i != 1 || state.shieldFrames > 0) return;
        state.shieldFrames = ShieldFrames;
        Debug.Log("GainShield");
    }

    private void LostShield(int i)
    {
        if (i != 1 || state.shieldFrames == 0) return;
        state.shieldFrames = 0;
        Debug.Log("LostShield");
    }

    public void Dash()
    {
        if (CannotInput || state.totalActiveFrames > 0) return;
        frameDataDict.TryGetValue("Dash", out var frameData);
        Vector2 dir = controller.StickInput;
        rb.velocity = Vector3.zero;
        endedPositionDashRatio = new Vector3(dir.x, dir.y, 0) * dashForce;
        endedPositionDashRatio /= frameData.Active;

        PlayAnimation(frameData, 0.05f);

        OnActive += DashOnDirection;
    }

    public void DashOnDirection(int i)
    {
        if (i <= 0) return;
        if (!Physics.Raycast(transform.position, endedPositionDashRatio, out var hit, 1f))
            rb.MovePosition(transform.position + endedPositionDashRatio);
    }


    public void Jump()
    {
        if (CannotInput) return;

        var grounded = state.grounded || (state.ledged && !ledgeJumpIsAirJump);

        if (!grounded && airJumpsLeft <= 0) return;

        if (!grounded) airJumpsLeft--;

        cachedVelocity = rb.velocity;
        cachedVelocity.y = 0;
        rb.velocity = cachedVelocity;

        var dir = Vector3.up;
        if (state.ledged)
        {
            state.jumpFrames = ledgeJumpFrames;
            dir = Vector3.up + (-state.ledgeDirection).normalized;
        }

        rb.AddForce(dir.normalized * jumpForce, ForceMode.Impulse);
    }

    public void Attack(bool heavy = false)
    {
        if (CannotInput) return;
        
        var frameData = AttackUp(heavy);

        if (controller.StickInput != Vector2.zero)
        {
            var dot = Vector2.Dot(controller.StickInput, Vector2.up);
            var up = Mathf.Abs(1f - dot);
            var down = Mathf.Abs(-1f - dot);
            var side = Mathf.Abs(0 - dot);

            if (up < down && up < side) frameData = AttackUp(heavy);
            else if (down < up && down < side) frameData = AttackDown(heavy);
            else
            {
                frameData = AttackSide(heavy);
            }

            FrameDataSo.FrameData AttackDown(bool heavyAttack)
            {
                return frameDataDict[
                    state.grounded
                        ? (heavyAttack ? "DownHeavy" : "DownLight")
                        : (heavyAttack ? "GroundPound" : "DownAir")];
            }

            FrameDataSo.FrameData AttackSide(bool heavyAttack)
            {
                if (state.grounded)
                {
                    return frameDataDict[(heavyAttack ? "SideHeavy" : "SideLight")];
                }

                if (heavy)
                {
                    if (up < down) return AttackUp(true);
                    return AttackDown(true);
                }

                return frameDataDict["SideAir"];
            }
        }

        rb.velocity = new Vector3(
            (frameData.StopVelocityX) ? 0 : rb.velocity.x,
            (frameData.StopVelocityY) ? 0 : rb.velocity.y,
            rb.velocity.z);

        PlayAnimation(frameData);

        lastAttackChargeUltimate = (heavy ? chargeUltimateHeavy : chargeUltimateLight);

        OnActive += checkAttack;


        return;

        FrameDataSo.FrameData AttackUp(bool heavyAttack)
        {
            return frameDataDict[
                state.grounded ? (heavyAttack ? "UpHeavy" : "UpLight") : (heavyAttack ? "Recovery" : "UpAir")];
        }
    }

    private void checkAttack(int i)
    {
        if (i != 1) return;
        if (CurrentBattleModel.HitThisFrame()) GainUltimate(lastAttackChargeUltimate, true);
    }

    private void PlayAnimation(FrameDataSo.FrameData frameData, float transitionDuration = 0.1f)
    {
        if (frameData == null) return;

        var str = frameData.AnimationName;
        Debug.Log($"Playing {str} data on {CurrentBattleModel}");

        CurrentAnimator.CrossFade(frameData.AnimationName, transitionDuration);

        OnStartup = null;
        OnActive = null;
        OnRecovering = null;

        state.startup = frameData.Startup;
        state.active = frameData.Active;
        state.recovering = frameData.Recovery;
    }

    private void DecreaseTransformedFrames()
    {
        if (!state.shouldBeTransformed && state.isTransformed && !CannotInput)
        {
            Transformation(false);
        }

        if (!state.shouldBeTransformed) return;
        state.transformedFrames--;

        //TODO decrease cumul ultimate
        CumulUltimate = state.transformedFrames / (float)transformationFrames;
        
        Debug.Log($"DecreaseTransformedFrames : {state.transformedFrames} / {transformationFrames} = {CumulUltimate}");
        
        OnTransformationChargeUpdated?.Invoke(CumulUltimate);
    }

    private void HandleAnimations()
    {
        NormalAnimator.SetBool(animCanInput, !CannotInput);
        NormalAnimator.SetBool(animCanInput, !CannotInput);
        NormalAnimator.SetBool(animIsGrounded, state.grounded);
        NormalAnimator.SetBool(animIsLedged, state.ledged);
        NormalAnimator.SetBool(animIsDropping, state.dropping);
        NormalAnimator.SetFloat(animVelocityX, Velocity.x);
        NormalAnimator.SetFloat(animMagnitudeX, Mathf.Abs(Velocity.x));
        NormalAnimator.SetFloat(animVelocityY, Velocity.y);

        TransformedAnimator.SetBool(animCanInput, !CannotInput);
        TransformedAnimator.SetBool(animCanInput, !CannotInput);
        TransformedAnimator.SetBool(animIsGrounded, state.grounded);
        TransformedAnimator.SetBool(animIsLedged, state.ledged);
        TransformedAnimator.SetBool(animIsDropping, state.dropping);
        TransformedAnimator.SetFloat(animVelocityX, Velocity.x);
        TransformedAnimator.SetFloat(animMagnitudeX, Mathf.Abs(Velocity.x));
        TransformedAnimator.SetFloat(animVelocityY, Velocity.y);
    }

    public float GainUltimate(float percent, bool isMine = false)
    {
        if (state.shouldBeTransformed) return CumulUltimate;

        CumulUltimate += percent;
        if (isMine) OnGainUltimate?.Invoke(this, percent);
        OnTransformationChargeUpdated?.Invoke(CumulUltimate);

        if (CumulUltimate >= 1)
        {
            Transformation(true);
        }

        return CumulUltimate;
    }
}