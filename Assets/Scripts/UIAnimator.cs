/*
MIT License

Copyright (c) 2025 Jonas Wombacher

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimator : MonoBehaviour
{
    private enum ScalingAnimationState { none, shrinking, growing}
    private enum TranslatingAnimationState { none, negative, positive}

    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("UI Elements")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private RectTransform arrow;
    [SerializeField] private GameObject lookTargetCanvas;
    [SerializeField] private Transform lookTargetRotationParent;
    [SerializeField] private Slider progressSlider;

    [Header("Settings")]
    [SerializeField] private float cursorScaleDelta;
    [SerializeField] private float cursorAnimationDuration;
    [Space(10)]
    [SerializeField] private float arrowTranslationDistance;
    [SerializeField] private float arrowAnimationDuration;
    [Space(10)]
    [SerializeField] private float lookAngleThresholdWide;
    [SerializeField] private float lookAngleThresholdNarrow;
    [Space(10)]
    [SerializeField] private float lookTargetRotationSpeed;
    [SerializeField] private float lookTargetOffset;
    [Space(10)]
    [SerializeField] private float lookAtAngleThreshold;
    [SerializeField] private float lookAtTimeToReach;
    [SerializeField] private float lookAtMaxDistance;

    private UIManagerPaintingsTask uiManagerPaintingsTask;
    private StudyManager taskManager;

    // cursor animation
    private ScalingAnimationState cursorAnimationState;
    private float cursorScaleDeltaPerSecond;

    // arrow animation
    private TranslatingAnimationState arrowAnimationState;
    private Vector2 initialArrowOffset;
    private Vector2 currentArrowOffset;
    private float arrowTranslationPerSecond;
    private float currentArrowDistance;
    private Transform currentTarget;

    // progress slider animation
    private float lookProgressTimer;

    private void Start()
    {
        // get references
        this.uiManagerPaintingsTask = this.GetComponent<UIManagerPaintingsTask>();
        this.taskManager = this.GetComponent<StudyManager>();

        // the scale delta needs to be achieved in a quarter of the duration (animation consists of growing, shrinking back to normal, shrinking, growing back to normal)
        this.cursorScaleDeltaPerSecond = this.cursorScaleDelta / (this.cursorAnimationDuration / 4);

        // the transform distance needs to be achieved in a quarter of the duration (animation consists of positive, negative back to normal, negative, positive back to normal)
        this.arrowTranslationPerSecond = this.arrowTranslationDistance/ (this.arrowAnimationDuration/ 4);

        // initialize offsets to the positions assigned in the editor
        this.initialArrowOffset = this.arrow.localPosition;
        this.currentArrowOffset = this.initialArrowOffset;

        this.cursorAnimationState = ScalingAnimationState.growing;
        this.arrowAnimationState = TranslatingAnimationState.positive;

    }

    private void Update()
    {
        // hide cursor and arrow when there is no target to direct the user towards
        if (this.currentTarget == null)
        {
            this.Hide();
            return;
        }

        // calculate angles and thresholds
        float angleH = this.GetHorizontalLookAngleAbs();
        float angleV = this.GetVerticalLookAngleAbs();
        bool wideThresholdReached = angleH > this.lookAngleThresholdWide;
        bool narrowThresholdReached = angleH > this.lookAngleThresholdNarrow;
        bool lookingAtTarget = angleH < this.lookAtAngleThreshold && angleV < this.lookAtAngleThreshold;

        // update the animations
        this.UpdateArrowOffset();
        this.CursorAnimation(narrowThresholdReached);
        this.ArrowAnimation(wideThresholdReached);
        this.LookTargetIndicatorAnimation(wideThresholdReached);
        this.ProgressSliderAnimation(lookingAtTarget, narrowThresholdReached);
    }

    // update the cursor animation
    private void CursorAnimation(bool narrowAngleThresholdReached)
    {
        if (this.cursorAnimationState == ScalingAnimationState.none) return;

        if (this.cursorAnimationState == ScalingAnimationState.growing)
        {
            // switch to shrinking
            if (this.cursor.localScale.x >= 1 + this.cursorScaleDelta) this.cursorAnimationState = ScalingAnimationState.shrinking;
            // increase the scale
            else this.cursor.localScale += new Vector3(this.cursorScaleDeltaPerSecond, this.cursorScaleDeltaPerSecond, 0) * Time.deltaTime;
        }

        if (this.cursorAnimationState == ScalingAnimationState.shrinking)
        {
            // switch to growing
            if (this.cursor.localScale.x <= 1 - this.cursorScaleDelta) this.cursorAnimationState = ScalingAnimationState.growing;
            // increase the scale
            else this.cursor.localScale -= new Vector3(this.cursorScaleDeltaPerSecond, this.cursorScaleDeltaPerSecond, 0) * Time.deltaTime;
        }

        // only show cursor, if the user is not looking directly at the painting
        this.cursor.gameObject.SetActive(narrowAngleThresholdReached);
    }

    // update the direction arrow animation
    private void ArrowAnimation(bool wideAngleThresholdReached)
    {
        if (this.arrowAnimationState == TranslatingAnimationState.none) return;

        // hide the arrow, if the player is looking at the picture (angle lower than the threshold)
        if (!wideAngleThresholdReached)
        {
            this.arrow.gameObject.SetActive(false);
            return;
        }

        if (this.arrowAnimationState == TranslatingAnimationState.positive)
        {
            // switch to negative
            if (this.currentArrowDistance >= this.arrowTranslationDistance) this.arrowAnimationState = TranslatingAnimationState.negative;
            // increase the current distance
            else this.currentArrowDistance += this.arrowTranslationPerSecond * Time.deltaTime;
        }

        if (this.arrowAnimationState == TranslatingAnimationState.negative)
        {
            // switch to positive
            if (this.currentArrowDistance <= -this.arrowTranslationDistance) this.arrowAnimationState = TranslatingAnimationState.positive;
            // decrease the current distance
            else this.currentArrowDistance -= this.arrowTranslationPerSecond * Time.deltaTime;
        }

        // show arrow and apply translation
        this.arrow.gameObject.SetActive(true);
        this.arrow.localPosition = this.currentArrowOffset + this.currentArrowOffset.normalized * this.currentArrowDistance;

        // rotate arrow to point in the direction of the translation
        float angle = Vector2.Angle(Vector2.right, this.currentArrowOffset.normalized);
        if (this.currentArrowOffset.y < 0) angle = 360 - angle;
        this.arrow.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // update the animation of the look target indicator
    private void LookTargetIndicatorAnimation(bool wideAngleThresholdReached)
    {
        // place the indicator in front of the target painting
        this.lookTargetCanvas.transform.position = this.currentTarget.position + this.currentTarget.forward * this.lookTargetOffset;
        this.lookTargetCanvas.transform.forward = this.currentTarget.forward;

        // animate the indicator, if the player's look cursor is close enough
        if (!wideAngleThresholdReached) this.lookTargetRotationParent.transform.Rotate(0, 0, this.lookTargetRotationSpeed * Time.deltaTime, Space.Self);
    }

    // update the animation of the progress slider
    private void ProgressSliderAnimation(bool lookingAtTarget, bool narrowAngleThresholdReached)
    {
        // calculate how long the user has been looking at the target
        if (lookingAtTarget && this.GetTargetDistance() <= this.lookAtMaxDistance) this.lookProgressTimer += Time.deltaTime;
        else this.lookProgressTimer = 0;

        // update slider value
        this.progressSlider.value = this.lookProgressTimer / this.lookAtTimeToReach;

        // start the iteration, once the user is close enough to the painting and looking at it
        if (this.lookProgressTimer > 0 && this.taskManager != null && this.taskManager.GetCurrentlyWaitingToStart()) this.taskManager.StartCurrentIteration(); 

        // only show slider, if the user is looking somewhat close enough at the painting
        this.progressSlider.gameObject.SetActive(!narrowAngleThresholdReached);

        // notify task manager if the user has been looking at the target long enough
        if (this.taskManager != null && this.lookProgressTimer >= this.lookAtTimeToReach) this.taskManager.OnPaintingLookedAt();
    }

    // get absolute value of the angle between the looking direction and the target, discarding the y-axis
    private float GetHorizontalLookAngleAbs()
    {
        Vector3 targetDir = Utils.WithoutY(this.currentTarget.position - this.cam.transform.position);
        Vector3 lookDir = Utils.WithoutY(this.cam.transform.forward);

        return Vector3.Angle(targetDir, lookDir);
    }

    // get absolute value of the angle between the looking direction and the target, discarding the x-axis
    private float GetVerticalLookAngleAbs()
    {
        Vector3 targetDir = Utils.WithoutX(this.currentTarget.position - this.cam.transform.position);
        Vector3 lookDir = Utils.WithoutX(this.cam.transform.forward);

        return Vector3.Angle(targetDir, lookDir);
    }

    // get distance between the user and the target
    private float GetTargetDistance()
    {
        return (Utils.WithoutY(this.currentTarget.position) - Utils.WithoutY(this.cam.transform.position)).magnitude;
    }

    // update the arrow offset based on the direction the user has to look into to get to the painting
    private void UpdateArrowOffset()
    {
        /*
        // Get the position of the target in screen space
        Vector3 targetScreenPos = this.cam.WorldToScreenPoint(this.currentTarget.position);
        if (targetScreenPos.z < 0) targetScreenPos *= -1;

        // calculate new offset
        Vector3 dir = (targetScreenPos - this.cam.WorldToScreenPoint(this.cursor.position)).normalized;
        this.currentArrowOffset = dir * this.initialArrowOffset.magnitude;
        */

        Vector3 relativePos = (this.currentTarget.position - this.cursor.transform.position).normalized;
        float angle = Vector3.SignedAngle(relativePos, this.cam.transform.forward, Vector3.up);
        this.currentArrowOffset = Quaternion.Euler(0, 0, angle) * this.initialArrowOffset;
    }

    // update the target, in whose direction the arrow should be guiding the user
    public void SetTarget(Transform target)
    {
        this.currentTarget = target;
    }

    // show cursor and arrow
    public void Show()
    {
        // hide instructions text
        this.uiManagerPaintingsTask.SetInstructionsVisibility(false);

        // start animations
        this.cursorAnimationState = ScalingAnimationState.growing;
        this.arrowAnimationState = TranslatingAnimationState.positive;

        // update the animations once before showing the images (makes sure the images already fit the new target)
        this.Update();

        // show images
        this.cursor.gameObject.SetActive(true);
        this.arrow.gameObject.SetActive(true);
        this.lookTargetCanvas.SetActive(true);
    }

    // hide cursor and arrow
    public void Hide()
    {
        // show instructions text again
        this.uiManagerPaintingsTask.SetInstructionsVisibility(true);

        // hide images
        this.cursor.gameObject.SetActive(false);
        this.arrow.gameObject.SetActive(false);
        this.lookTargetCanvas.SetActive(false);

        // stop animations
        this.cursorAnimationState = ScalingAnimationState.none;
        this.arrowAnimationState = TranslatingAnimationState.none;
    }
}
