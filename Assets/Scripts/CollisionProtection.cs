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

public class CollisionProtection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StudyManager taskManager;
    [SerializeField] private AbstractModelManager modelManager;
    [SerializeField] private Alignment alignment;
    [SerializeField] private Transform[] obstacles;
    [SerializeField] private Transform obstacleParent;

    [Header("Settings")]
    [SerializeField] private bool active;
    [SerializeField] private string obstacleTag;
    [Range(0, 1)][SerializeField] private float vibrationIntensity;

    private bool parentChanged;
    private bool collisionDetected;

    private void Update()
    {
        if (!this.parentChanged && this.alignment.GetAlignmentDone() && this.alignment.GetFineTuningDone())
        {
            // change parent to prevent obstacles from being manipulated by the RDW
            foreach (Transform obstacle in this.obstacles) obstacle.parent = this.obstacleParent;
            this.parentChanged = true;
        }

        if (this.collisionDetected) OVRInput.SetControllerVibration(1, this.vibrationIntensity, OVRInput.Controller.All);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (this.active && other.tag == this.obstacleTag) this.OnImpendingCollision();
    }

    // if it is currently active, deactivate the model so the user can see the impending collision
    private void OnImpendingCollision()
    {
        // don't check for collisions during alignment and fine tuning
        if (!this.taskManager.GetCollisionDetectionWanted()) return;

        // no need to do something if there is no model being displayed anyway
        if (!this.modelManager.IsModelVisible()) return;

        // deactivate virtual content, so the user can see the real world again
        this.modelManager.ToggleModelVisibility();
        this.modelManager.SetPaintingsActive(false);

        // give haptic feedback in the Update method
        this.collisionDetected = true;

        // notify the task manager
        this.taskManager.EnterCollisionDeadlockState();
    }

    // restore the state from before the collision
    public void Recover()
    {
        // reset the flag (disables haptic feedback)
        this.collisionDetected = false;

        // restore painting and model visibility
        this.modelManager.SetPaintingsActive(true);
        if (!this.modelManager.IsModelVisible()) this.modelManager.ToggleModelVisibility();
    }

    // stop the haptic feedback
    public void StopHapticFeedback()
    {
        this.collisionDetected = false;
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.All);
    }
}
