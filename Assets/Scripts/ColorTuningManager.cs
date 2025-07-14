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
using UnityEngine.UI;

// controller for the scene allowing you to fine tune the model's color
public class ColorTuningManager : GameManager
{
    [Header("References")]
    [SerializeField] private MeshRenderer modelMR;
    [SerializeField] private TMP_Text valueTextRed;
    [SerializeField] private TMP_Text valueTextGreen;
    [SerializeField] private TMP_Text valueTextBlue;
    [SerializeField] private Slider sliderRed;
    [SerializeField] private Slider sliderGreen;
    [SerializeField] private Slider sliderBlue;
    [SerializeField] private Image previewImage;

    private UIManagerPaintingsTask uiManagerPaintingsTask;
    private int state = 0;
    private bool loadingSpatialAnchors;
    private Material modelMaterial;

    /*
        states:
            - 0: waiting for the model to load
            - 1: alignment
            - 2: fine tuning the colors
    */
    private static int STATE_MODEL_LOADING = 0;
    private static int STATE_ALIGNMENT = 1;
    private static int STATE_FINE_TUNING = 2;

    private new void Start()
    {
        base.Start();
        
        // get references
        this.uiManagerPaintingsTask = this.GetComponent<UIManagerPaintingsTask>();
        this.modelMaterial = this.modelMR.material;

        Invoke("Init", this.initDelay);
    }

    private new void Update()
    {
        // handle state and input
        this.UpdateState();
        this.HandleInput();
    }

    // initialize the task by loading the room model etc.
    private void Init()
    {
        this.ClearPaintingTimer();

        // configure model manager to only toggle the style transfer model on and off (instead of cycling all models)
        this.modelManager.LockModel(1);
    }

    // trigger the according actions, whenever a button is pressed
    private void HandleInput()
    {
        if (this.state == STATE_ALIGNMENT) // alignment
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
        else if (this.state == STATE_FINE_TUNING) // fine tuning the colors
        {
            // button Y to toggle the model
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(OVRInput.Button.Four, OVRInput.Controller.Touch))
            {
                this.modelManager.ToggleModelVisibility();
            }
        }
    }

    // update the current state of the task
    private void UpdateState()
    {
        if (this.state == STATE_MODEL_LOADING) // waiting for the model to load
        {
            // if the style transfer model was loaded, update the instruction text to tell the user to perform the alignment
            if (this.modelManager.GetModelCount() == 2) // TODO: still needs to be adjusted to the three-point alignment (like the ChangingPaintingsTaskManager)
            {
                this.uiManagerPaintingsTask.ShowAlignmentInstruction(0);
                this.state++;
            }
        }
        else if (this.state == STATE_ALIGNMENT) // alignment
        {
            if (this.loadingSpatialAnchors && this.alignment.GetAlignmentPositionsCollected() == 2)
            {
                // allow button input again, once the spatial anchors have been loaded
                this.loadingSpatialAnchors = false;

                // show the final alignment instruction
                this.uiManagerPaintingsTask.ShowAlignmentInstruction(2);
            }

            if (this.alignment.GetAlignmentDone() && this.alignment.GetFineTuningDone())
            {
                state++;
            }
        }
    }

    // clear the painting timer text
    private void ClearPaintingTimer()
    {
        this.uiManagerPaintingsTask.ClearPaintingTimerText();
    }

    // set the model's color to the given RGB-values
    private void SetModelColor(float r, float g, float b)
    {
        Color color = new Color(r, g, b);
        this.previewImage.color = color;
        this.modelMaterial.color = color;
        this.modelMR.material = this.modelMaterial;
    }

    // reset the model's color
    public void ResetModelColor()
    {
        this.sliderRed.value = this.sliderRed.maxValue;
        this.sliderGreen.value = this.sliderGreen.maxValue;
        this.sliderBlue.value = this.sliderBlue.maxValue;
    }

    // handle updated value of a color slider
    public void OnSliderChanged()
    {
        this.SetModelColor(this.sliderRed.value / 255f, this.sliderGreen.value / 255f, this.sliderBlue.value / 255f);
        this.valueTextRed.text = ((int)this.sliderRed.value).ToString();
        this.valueTextGreen.text = ((int)this.sliderGreen.value).ToString();
        this.valueTextBlue.text = ((int)this.sliderBlue.value).ToString();
    }

    // increase the red color by one step
    public void IncreaseRed()
    {
        this.sliderRed.value += 1;
    }

    // decrease the red color by one step
    public void DecreaseRed()
    {
        this.sliderRed.value -= 1;
    }

    // increase the green color by one step
    public void IncreaseGreen()
    {
        this.sliderGreen.value += 1;
    }

    // decrease the green color by one step
    public void DecreaseGreen()
    {
        this.sliderGreen.value -= 1;
    }

    // increase the blue color by one step
    public void IncreaseBlue()
    {
        this.sliderBlue.value += 1;
    }

    // decrease the blue color by one step
    public void DecreaseBlue()
    {
        this.sliderBlue.value -= 1;
    }
}
