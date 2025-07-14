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

public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] protected float interactionCooldown = 1;
    [SerializeField] protected float initDelay = 1;

    [Header("References")]
    [SerializeField] protected Alignment alignment;
    [SerializeField] protected AbstractModelManager modelManager;

    protected UIManager uiManager;
    protected RDWManager rdwManager;
    protected float lastInteraction;

    protected Dictionary<string, OVRInput.Button> buttonMapping = new Dictionary<string, OVRInput.Button>(){
        { "alignmentContinue", OVRInput.Button.Two },
        { "alignmentLoad", OVRInput.Button.Start },
        { "fineTuningModelView", OVRInput.Button.Four },
        { "fineTuningModelUpReset", OVRInput.Button.Three },
        { "fineTuningModeSwitch", OVRInput.Button.Start },
        { "toggleSettingsMenu", OVRInput.Button.Start },
        { "toggleModelVisibility", OVRInput.Button.Two },
        { "startRDW", OVRInput.Button.Three },
        { "stopRDW", OVRInput.Button.Four },
        { "paintingsTaskContinue", OVRInput.Button.Four },
        { "nextIteration", OVRInput.Button.Start },
        { "collisionRecover", OVRInput.Button.Start }
    };

    protected void Start()
    {
        // get references
        this.uiManager = this.GetComponent<UIManager>();
        this.rdwManager = this.GetComponent<RDWManager>();
    }

    protected void Update()
    {
        this.HandleInput();
    }

    // trigger the according actions, whenever a button is pressed
    private void HandleInput()
    {
        // start/option button to toggle the settings menu
        if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["toggleSettingsMenu"], OVRInput.Controller.Touch))
        {
            this.uiManager.ToggleSettingsMenu();
            this.UpdateLastInteractionTime();
        }

        if (!this.alignment.GetAlignmentDone())
        {
            // button A to continue the alignment progress
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["alignmentContinue"], OVRInput.Controller.Touch))
            {
                this.alignment.NextStep();
                this.UpdateLastInteractionTime();
            }
        }
        else
        {
            // button B to toggle the model visibility
            if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["toggleModelVisibility"], OVRInput.Controller.Touch))
            {
                this.modelManager.ToggleModelVisibility();
                this.UpdateLastInteractionTime();
            }
            // button X to start RWD
            else if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["startRDW"], OVRInput.Controller.Touch))
            {
                this.rdwManager.SetCurvatureDirection(true);
                this.rdwManager.StartCurvatureRDW();
                this.UpdateLastInteractionTime();
            }
            // button Y to stop RWD
            else if (Time.time - this.lastInteraction > this.interactionCooldown && OVRInput.GetUp(this.buttonMapping["stopRDW"], OVRInput.Controller.Touch))
            {
                this.rdwManager.StopCurvatureRDW();
                this.UpdateLastInteractionTime();
            }
        }
    }

    // update time of last interaction
    public void UpdateLastInteractionTime()
    {
        this.lastInteraction = Time.time;
    }
}
