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
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

// manages the study experiment
public class StudyManager : GameManager
{
    [System.Serializable]
    private struct IterationData
    {
        public float[] rotationGains;
        public bool hideModelAtFinalPainting;

        public IterationData(float[] gains, bool hideModel)
        {
            this.rotationGains = gains;
            this.hideModelAtFinalPainting = hideModel;
        }
    }
    
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private ChangingPainting[] paintings;
    [SerializeField] private CollisionProtection collisionProtection;
    [SerializeField] private Trail trail;
    [SerializeField] private GrabbableCube[] grabbableCubes;

    [Header("Task Settings")]
    [SerializeField] private float paintingTimer = 20;
    [SerializeField] private IterationData[] iterationData = { 
        new IterationData(new float[] { 0.05f, -0.1f, 0.1f, -0.1f, 0, 0.1f, -0.1f, 0.1f, -0.05f, 0 }, true),    // first iteration, no teleport
        new IterationData(new float[] { 0.05f, -0.1f, 0.1f, -0.1f, -0.05f }, false),                            // second iteration, with teleport
        new IterationData(new float[] { 0.05f, -0.1f, 0.1f, -0.1f, 0, 0.1f, -0.1f, 0.1f, -0.05f, 0 }, true)     // third iteration, no teleport
    };
    [SerializeField] private float fineTuningMinThreshold = 0.1f;
    [SerializeField] private float fineTuningTranslationSpeed;
    [SerializeField] private float fineTuningRotationSpeed;
    [SerializeField] private bool adjustPaintingsHeight;

    private UIManagerPaintingsTask uiManagerPaintingsTask;
    private UIAnimator uiAnimator;
    private int state = 0;
    private int iteration = 0;
    private int paintingIndex = 0;
    private int paintingsLookedAt = 0;
    private float currentTimer = 0;
    private float currentPaintingTimer;
    private bool paintingTimerRunning;
    private bool loadingSpatialAnchors;
    private bool fineTuningRotationMode;
    private DataLogging dataLogging;
    private int preCollisionState;

    /*
        states:
            - 0: waiting for the model to load
            - 1: alignment
            - 2: waiting for button input to start the timer and continue
            - 3: walking towards a painting
            - 4: looking at a painting (not the final one)
            - 5: walking towards the final painting
            - 6: looking at the final painting
            - 7: after the final painting was hidden
            - -1: deadlock state, entered in case of an impending collision
    */
    private static int STATE_ALIGNMENT = 0;
    private static int STATE_WAITING_FOR_START = 1;
    private static int STATE_WALKING = 2;
    private static int STATE_LOOKING = 3;
    private static int STATE_WALKING_FINAL = 4;
    private static int STATE_LOOKING_FINAL = 5;
    private static int STATE_DONE = 6;
    private static int STATE_COLLISION_DEADLOCK = -1;

    private new void Start()
    {
        base.Start();

        // get references
        this.uiManagerPaintingsTask = this.GetComponent<UIManagerPaintingsTask>();
        this.uiAnimator = this.GetComponent<UIAnimator>();
        this.dataLogging = this.GetComponent<DataLogging>();

        Invoke("Init", this.initDelay);
    }

    private new void Update()
    {
        // update timers
        if (this.state > STATE_WAITING_FOR_START) this.UpdateTimer();
        if (this.paintingTimerRunning) this.UpdatePaintingTimer();

        // handle state and input
        this.UpdateState();
        this.HandleInput();
    }

    // initialize the task
    private void Init()
    {
        // clear the painting timer
        this.ClearPaintingTimer();

        // show the first alignment instruction
        this.uiManagerPaintingsTask.ShowAlignmentInstruction(0);
    }

    // trigger the according actions, whenever a button is pressed
    private void HandleInput()
    {
        if (this.state == STATE_ALIGNMENT) // alignment
        {
            // don't allow further input while the spatial anchors are loading
            if (this.loadingSpatialAnchors) return;

            // button B to continue the alignment progress
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["alignmentContinue"], OVRInput.Controller.Touch))
            {
                this.alignment.NextStep();
                this.UpdateLastInteractionTime();

                // show the next alignment instruction
                this.uiManagerPaintingsTask.ShowAlignmentInstruction(this.alignment.GetAlignmentPositionsCollected());
            }
            // button Start to load the alignment from spatial anchors, if no anchor was set yet
            if (this.alignment.GetAlignmentPositionsCollected() == 0 && Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["alignmentLoad"], OVRInput.Controller.Touch))
            {
                this.alignment.LoadPositionsFromSpatialAnchors();
                this.loadingSpatialAnchors = true;
                this.UpdateLastInteractionTime();

                // instruct player to wait
                this.uiManagerPaintingsTask.ShowLoadingInstruction();
            }
            // input for the fine tuning
            if (this.alignment.GetAlignmentDone()) this.HandleFineTuningInput();
        }
        else if (this.state == STATE_WAITING_FOR_START) // waiting for button input to start the timer and continue 
        {
            // button Y to continue the paintings task (start the timer)
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["paintingsTaskContinue"], OVRInput.Controller.Touch))
            {
                this.StartCurrentIteration();
            }
        }
        else if (this.state == STATE_WALKING || this.state == STATE_WALKING_FINAL) // walking towards a painting
        {
            // button Y to continue the paintings task (looking at the painting)
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["paintingsTaskContinue"], OVRInput.Controller.Touch))
            {
                this.UpdateLastInteractionTime();
                this.ShowNextPainting();
            }
        }
        else if (this.state == STATE_DONE) // after the final painting was hidden
        {
            // button Y to continue the paintings task (deactivate the room model to resolve the RDW, i.e. resolve the illusion)
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["paintingsTaskContinue"], OVRInput.Controller.Touch))
            {
                this.modelManager.ToggleModelVisibility();
                this.modelManager.TogglePaintings();
                this.trail.Toggle();
            }
            // start button to go to the next iteration, if it wasn't the last one already
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["nextIteration"], OVRInput.Controller.Touch))
            {
                if (this.iteration + 1 < this.iterationData.Length) this.NextIteration();
            }
        }
        else if (this.state == STATE_COLLISION_DEADLOCK) // deadlock state, entered in case of an impending collision
        {
            // button Y to toggle room model to show user how the RDW almost caused a collision
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["paintingsTaskContinue"], OVRInput.Controller.Touch))
            {
                this.modelManager.ToggleModelVisibility();
                this.modelManager.TogglePaintings();

                // stop the controller vibration, as we know it was noticed already (a button on the controller was pressed)
                this.collisionProtection.StopHapticFeedback();
            }
            // start button to recover from the collision and allow the study to continue
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["collisionRecover"], OVRInput.Controller.Touch))
            {
                // restore the original state
                this.state = this.preCollisionState;

                // stop the controller vibration and restore painting and model visibility
                this.collisionProtection.Recover();
            }
        }
    }

    // handle input during the fine tuning
    private void HandleFineTuningInput()
    {
        // start button to switch between translation and rotation fine tuning
        if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["fineTuningModeSwitch"], OVRInput.Controller.Touch))
        {
            this.fineTuningRotationMode = !this.fineTuningRotationMode;
            if (!this.modelManager.IsModelVisible()) this.modelManager.ToggleModelVisibility();
            this.modelManager.AssignFineTuningMaterial(this.fineTuningRotationMode);
        }

        // button Y to show/hide the model with its actual texture during fine tuning
        if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["fineTuningModelView"], OVRInput.Controller.Touch))
        {
            this.modelManager.ApplyOpaqueMaterial();
            this.modelManager.ToggleModelVisibility();
        }

        // button X to reset the model's up-axis
        if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["fineTuningModelUpReset"], OVRInput.Controller.Touch))
        {
            this.alignment.FineTuneResetUpAxis();
        }

        // fine tuning input with the joysticks
        Vector2 leftJoystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.Touch);
        Vector2 rightJoystick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick, OVRInput.Controller.Touch);

        // THIS CHECK HAS TO BE AFTER THE OTHER FINE TUNING BUTTON INPUT, BECAUSE OF THE RETURN
        if (leftJoystick.magnitude < this.fineTuningMinThreshold && rightJoystick.magnitude < this.fineTuningMinThreshold) return;

        // make sure the model is visible with the correct fine tuning material
        if (!this.modelManager.IsModelVisible()) this.modelManager.ToggleModelVisibility();
        this.modelManager.AssignFineTuningMaterial(this.fineTuningRotationMode);

        // apply the input to either the rotation or translation fine tuning
        if (this.fineTuningRotationMode) this.alignment.FineTuneRotation(leftJoystick * this.fineTuningRotationSpeed * Time.deltaTime, rightJoystick * this.fineTuningRotationSpeed * Time.deltaTime);
        else this.alignment.FineTuneTranslation(leftJoystick * this.fineTuningTranslationSpeed * Time.deltaTime, rightJoystick * this.fineTuningTranslationSpeed * Time.deltaTime);
    }

    // update the current state of the task
    private void UpdateState()
    {
        if (this.state == STATE_ALIGNMENT) // alignment
        {
            if (this.loadingSpatialAnchors && this.alignment.GetAlignmentPositionsCollected() == this.alignment.GetAlignmentPositionsToBeCollected())
            {
                // allow button input again, once the spatial anchors have been loaded
                this.loadingSpatialAnchors = false;

                // show the final alignment instruction
                this.uiManagerPaintingsTask.ShowAlignmentInstruction(this.alignment.GetAlignmentPositionsToBeCollected());
            }

            if (this.alignment.GetAlignmentDone() && this.alignment.GetFineTuningDone())
            {
                // initialize the grabbable cubes
                foreach (GrabbableCube gc in this.grabbableCubes) gc.Init();

                // hint the user to go to the first painting
                this.DirectUserToPainting(paintings[0]);

                state++;
            }
        }
        else if (this.state == STATE_LOOKING) // user looking at a painting (not the final one)
        {
            if (this.currentPaintingTimer <= 0)
            {
                this.ClearPaintingTimer();

                // hide the painting again
                this.paintings[this.paintingIndex].HidePainting();

                // hint the user to go to the next painting
                this.paintingsLookedAt++;
                this.paintingIndex = (this.paintingIndex + 1) % this.paintings.Length;
                this.DirectUserToPainting(this.paintings[this.paintingIndex]);

                // start the redirected walking
                this.rdwManager.StartRotationRDW(this.iterationData[this.iteration].rotationGains[paintingsLookedAt - 1]);

                // go to next state if the next painting is the last one
                if (this.paintingsLookedAt == this.iterationData[this.iteration].rotationGains.Length) state++;
                // go back to the previous state, as long as there are more paintings to look at
                else state--;
            }
        }
        else if (this.state == STATE_LOOKING_FINAL) // user looking at the final painting
        {
            if (this.currentPaintingTimer <= 0)
            {
                this.ClearPaintingTimer();

                // hide the painting again
                this.paintings[this.paintingIndex].HidePainting();

                // hide the directions UI
                this.uiAnimator.SetTarget(null);

                // show instruction to take off the headset
                this.uiManagerPaintingsTask.ShowTakeOffInstruction();

                // log the data from the finished iteration
                this.dataLogging.OnIterationFinished(this.iteration);

                state++;
            }
        }
    }

    // start the current iteration
    public void StartCurrentIteration()
    {
        // only allowed, if we are currently waiting for the iteration to start
        if (this.state != STATE_WAITING_FOR_START) return;

        this.state++;
        this.trail.AddPosition();

        // lock the grabbable cubes 
        foreach (GrabbableCube gc in this.grabbableCubes) gc.LockInPlace();

        // adjust the height of the paintings
        if (this.adjustPaintingsHeight) this.AdjustPaintingsHeight();
    }

    // check, if we are currently waiting for the current iteration to start
    public bool GetCurrentlyWaitingToStart()
    {
        return this.state == STATE_WAITING_FOR_START;
    }

    // hint user to go to the given painting
    private void DirectUserToPainting(ChangingPainting painting)
    {
        this.uiManagerPaintingsTask.ShowPaintingsInstruction(0);
        this.uiManagerPaintingsTask.PositionPaintingTimer(painting.GetTimerTransform());
        painting.StartMaterialInterpolation();
        this.uiAnimator.SetTarget(painting.transform);
        this.uiAnimator.Show();
    }


    // update the timer
    private void UpdateTimer(bool reset=false)
    {
        if (reset) this.currentTimer = 0;
        else this.currentTimer += Time.deltaTime;

        this.uiManagerPaintingsTask.UpdateTimerText(this.currentTimer);
    }

    // update the painting timer
    private void UpdatePaintingTimer()
    {
        this.currentPaintingTimer -= Time.deltaTime;
        this.uiManagerPaintingsTask.UpdatePaintingTimerText(this.currentPaintingTimer);
    }

    // clear the painting timer text
    private void ClearPaintingTimer()
    {
        this.paintingTimerRunning = false;
        this.uiManagerPaintingsTask.ClearPaintingTimerText();
    }

    // adjust the height of the paintings to the user's height
    private void AdjustPaintingsHeight()
    {
        float paintingsHeight = this.centerEyeAnchor.position.y;
        foreach (ChangingPainting painting in this.paintings) painting.transform.position = Utils.VectorOverride(painting.transform.position, null, paintingsHeight, null);

        // position the paintings timer again, so it matches the new height of the painting
        this.uiManagerPaintingsTask.PositionPaintingTimer(this.paintings[0].GetTimerTransform());
    }

    // called when the user has been looking at the painting for long enough
    public void OnPaintingLookedAt()
    {
        if (this.state == STATE_WALKING || this.state == STATE_WALKING_FINAL) this.ShowNextPainting();
    }

    // show the next painting and start its timer
    private void ShowNextPainting()
    {
        // make the painting visible to the user
        this.paintings[this.paintingIndex].ShowNextPainting();
        this.uiManagerPaintingsTask.ClearInstructionsText();
        this.uiAnimator.SetTarget(null);

        // store the painting's position for data logging
        this.dataLogging.AddPosition(this.paintings[this.paintingIndex].transform.position);

        // activate the room model at the first painting
        if (!this.modelManager.IsModelVisible()) this.modelManager.ToggleModelVisibility(true);

        // hide the room model at the final painting
        if (this.iterationData[this.iteration].hideModelAtFinalPainting && this.state == STATE_WALKING_FINAL) this.modelManager.ToggleModelVisibility(true);

        // start the timer
        this.currentPaintingTimer = this.paintingTimer;
        this.paintingTimerRunning = true;

        // add the current user position to the trail
        this.trail.AddPosition();

        // go to the next state
        this.state++;
    }

    // go to the deadlock state when an impending collision was detected
    public void EnterCollisionDeadlockState()
    {
        this.preCollisionState = this.state;
        this.state = STATE_COLLISION_DEADLOCK;
    }

    // go to the next iteration
    private void NextIteration()
    {
        // increase the iteration index
        this.iteration++;

        // reset and hide the trail
        this.trail.ClearAndHide();

        // reset the timers
        this.ClearPaintingTimer();
        this.UpdateTimer(true);

        // reset the paintings
        this.paintingIndex = 0;
        this.paintingsLookedAt = 0;
        foreach (ChangingPainting cp in this.paintings) cp.ResetIndex();
        this.modelManager.SetPaintingsActive(true);

        // reset the model to its true position and reenable fine tuning
        this.alignment.StartNewIteration();

        this.state = STATE_ALIGNMENT;
    }

    // check whether the collision detection should currently be active
    public bool GetCollisionDetectionWanted()
    {
        return this.state == STATE_LOOKING || this.state == STATE_LOOKING_FINAL || this.state == STATE_WALKING || this.state == STATE_WALKING_FINAL || this.state == STATE_DONE;
    }
}
