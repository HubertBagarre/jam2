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
        
        OnActiveEnd += SwitchModel;
        
        TransformedAnimator.CrossFade(frameData.AnimationName, 0.1f);
        NormalAnimator.CrossFade(frameData.AnimationName, 0.1f);
        
        state.startup = frameData.Startup;
        state.active = frameData.Active;
        state.recovering = frameData.Recovery;
        if (transformed) state.transformedFrames = transformationFrames + state.totalFrames;
        
        return;
        
        void SwitchModel()
        {
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
        if (CannotInput || OnCooldownShield) return;
        state.shieldFrames = ShieldFrames;
        frameDataDict.TryGetValue("Shield", out var frameData);
        cooldownShield = cooldownFrameReloadShield + ShieldFrames;
        
        PlayAnimation(frameData,0.05f);

        cooldownShield += state.startup + state.active + state.recovering;
    }

    public void Dash()
    {
        if (CannotInput || OnCooldownDash) return;
        state.dashFrames = DashFrames;
        frameDataDict.TryGetValue("Dash", out var frameData);
        cooldownDash = cooldownFrameReloadDash + DashFrames;
        Vector2 dir = controller.StickInput;
        endedPositionDashRatio = new Vector3(dir.x, dir.y, 0) * dashForce;

        endedPositionDashRatio /= DashFrames;

        PlayAnimation(frameData,0.05f);

        cooldownDash += state.startup + state.active + state.recovering;
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
            dir = Vector3.up + (Vector3.right * -controller.StickInput.x).normalized;
        }

        rb.AddForce(dir.normalized * jumpForce, ForceMode.Impulse);
    }

    public void Attack(bool heavy = false)
    {
        if (CannotInput) return;

        //rb.velocity = Vector3.zero; //TODO do better

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
        
        return;

        FrameDataSo.FrameData AttackUp(bool heavyAttack)
        {
            return frameDataDict[
                state.grounded ? (heavyAttack ? "UpHeavy" : "UpLight") : (heavyAttack ? "Recovery" : "UpAir")];
        }
    }

    private void PlayAnimation(FrameDataSo.FrameData frameData,float transitionDuration = 0.1f)
    {
        if (frameData == null) return;
        
        var str = frameData.AnimationName;
        Debug.Log($"Playing {str} data on {CurrentBattleModel}");
        
        CurrentAnimator.CrossFade(frameData.AnimationName, transitionDuration);

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
        OnTransformationChargeUpdated?.Invoke(CumulUltimate);
        
        //TODO decrease cumul ultimate
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