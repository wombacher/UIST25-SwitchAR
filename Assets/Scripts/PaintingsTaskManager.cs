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
using UnityEngine;

// manages the task where the user has to look at three paintings, with a hard-coded sequence
/*
    states:
        - 0: waiting for the model to load
        - 1: alignment
        - 2: walking towards the first painting
        - 3: looking at the first painting
        - 4: walking towards the second painting
        - 5: looking at the second painting
        - 6: walking towards the third painting
        - 7: looking at the third and final painting
        - 8: after the third and final painting was hidden
*/
public class PaintingsTaskManager : GameManager
{
    [SerializeField] private Painting[] paintings;

    [Header("Task Settings")]
    [SerializeField] private float paintingTimer = 20;

    private UIManagerPaintingsTask uiManagerPaintingsTask;
    private UIAnimator uiAnimator;
    private int state = 0;
    private bool timerRunning;
    private float currentTimer;
    private bool loadingSpatialAnchors;

    private Dictionary<int, int> stateToPaintingMapping = new Dictionary<int, int> { { 2, 0 }, { 3, 0 }, { 4, 1 }, { 5, 1 }, { 6, 2 }, { 7, 2 } };
    private Dictionary<int, bool> stateToRDWDirectionMapping = new Dictionary<int, bool> { { 3, false }, { 5, true } };

    private new void Start()
    {
        base.Start();

        // get references
        this.uiManagerPaintingsTask = this.GetComponent<UIManagerPaintingsTask>();
        this.uiAnimator = this.GetComponent<UIAnimator>();

        Invoke("Init", this.initDelay);
    }

    private new void Update()
    {
        if (this.timerRunning) this.UpdateTimer();
        this.UpdateState();
        this.HandleInput();
    }

    // initialize the task by loading the room model etc.
    private void Init()
    {
        // load the style transfer model and show the according instruction for the user to wait
        this.modelManager.LoadStyleTransferModel();
        this.uiManagerPaintingsTask.ShowLoadingInstruction();
        this.ClearTimer();

        // don't show the model immediately after the alignment
        this.alignment.SetShowModelAfterAlignment(false);

        // configure model manager to only toggle the style transfer model on and off (instead of cycling all models)
        this.modelManager.LockModel(1);
    }

    // trigger the according actions, whenever a button is pressed
    private void HandleInput()
    {
        if (this.state == 1) // alignment
        {
            // don't allow further input while the spatial anchors are loading
            if (this.loadingSpatialAnchors) return;

            // button A to continue the alignment progress
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["alignmentContinue"], OVRInput.Controller.Touch))
            {
                this.alignment.NextStep();
                this.UpdateLastInteractionTime();

                // show the next alignment instruction
                this.uiManagerPaintingsTask.ShowAlignmentInstruction(this.alignment.GetAlignmentPositionsCollected());
            }
            // button B to load the alignment from spatial anchors, if no anchor was set yet
            if (this.alignment.GetAlignmentPositionsCollected() == 0 && Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["alignmentLoad"], OVRInput.Controller.Touch))
            {
                this.alignment.LoadPositionsFromSpatialAnchors();
                this.loadingSpatialAnchors = true;
                this.UpdateLastInteractionTime();

                // instruct player to wait
                this.uiManagerPaintingsTask.ShowLoadingInstruction();
            }
        }
        else if (this.state == 2 || this.state == 4 || this.state == 6) // walking towards a painting
        {
            // button X to continue the paintings task (looking at the painting)
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["paintingsTaskContinue"], OVRInput.Controller.Touch))
            {
                this.UpdateLastInteractionTime();

                // stop the redirected walking
                this.rdwManager.StopCurvatureRDW();

                // make the painting visible to the user
                this.paintings[this.stateToPaintingMapping[this.state]].HidePlaceholder();
                this.paintings[this.stateToPaintingMapping[this.state]].ShowBright();
                this.uiManagerPaintingsTask.ClearInstructionsText();
                this.uiAnimator.SetTarget(null);

                // activate the room model at the first painting
                if (this.state == 2) this.modelManager.ToggleModelVisibility();

                // start the timer
                this.currentTimer = this.paintingTimer;
                this.timerRunning = true;

                // go to the next state
                this.state++;
            }
        }
        else if (this.state == 8) // after the final painting was hidden
        {
            // button X to continue the paintings task (deactivate the room model to resolve the RDW, i.e. resolve the illusion)
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["paintingsTaskContinue"], OVRInput.Controller.Touch))
            {
                this.modelManager.ToggleModelVisibility();
                this.modelManager.TogglePaintings();
            }
        }
    }

    // update the current state of the task
    private void UpdateState()
    {
        if (this.state == 0) // waiting for the model to load
        {
            // if the style transfer model was loaded, update the instruction text to tell the user to perform the alignment
            if (this.modelManager.GetModelCount() == 2)
            {
                this.uiManagerPaintingsTask.ShowAlignmentInstruction(0);
                this.state++;
            }
        }
        else if (this.state == 1) // alignment
        {
            if (this.loadingSpatialAnchors && this.alignment.GetAlignmentPositionsCollected() == 2)
            {
                // allow button input again, once the spatial anchors have been loaded
                this.loadingSpatialAnchors = false;

                // show the final alignment instruction
                this.uiManagerPaintingsTask.ShowAlignmentInstruction(2);
            }

            if (this.alignment.GetAlignmentDone())
            {
                // hint the user to go to the first painting
                this.DirectUserToPainting(paintings[0]);

                state++;
            }
        }
        else if (this.state == 3 || this.state == 5) // user currently looking at the first or second painting
        {
            if (this.currentTimer <= 0)
            {
                this.ClearTimer();

                // hide the painting again
                this.paintings[this.stateToPaintingMapping[this.state]].ShowPlaceholder();
                this.paintings[this.stateToPaintingMapping[this.state]].ShowDark();

                // hint the user to go to the next painting
                this.DirectUserToPainting(this.paintings[this.stateToPaintingMapping[this.state + 1]]);

                // start the redirected walking
                this.rdwManager.SetCurvatureDirection(this.stateToRDWDirectionMapping[this.state]);
                this.rdwManager.SetRotationDirection(this.stateToRDWDirectionMapping[this.state]);
                this.rdwManager.StartRotationRDW();

                state++;
            }
        }
        else if (this.state == 7) // user currently looking at the final painting
        {
            if (this.currentTimer <= 0)
            {
                this.ClearTimer();

                // hide the painting again
                this.paintings[this.stateToPaintingMapping[this.state]].ShowPlaceholder();
                this.paintings[this.stateToPaintingMapping[this.state]].ShowDark();

                // hide the directions UI
                this.uiAnimator.SetTarget(null);

                state++;
            }
        }
    }

    // hint user to go to the given painting
    private void DirectUserToPainting(Painting painting)
    {
        this.uiManagerPaintingsTask.ShowPaintingsInstruction(0);
        painting.StartMaterialInterpolation();
        this.uiAnimator.SetTarget(painting.transform);
        this.uiAnimator.Show();
    }


    // update the timer
    private void UpdateTimer()
    {
        this.currentTimer -= Time.deltaTime;
        this.uiManagerPaintingsTask.UpdatePaintingTimerText(this.currentTimer);
    }

    // clear the timer text
    private void ClearTimer()
    {
        this.timerRunning = false;
        this.uiManagerPaintingsTask.ClearPaintingTimerText();
    }
}
