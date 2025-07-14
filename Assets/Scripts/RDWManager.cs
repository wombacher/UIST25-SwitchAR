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

public class RDWManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform modelParent;
    [SerializeField] private Transform centerEyeAnchor;

    [Header("Settings")]
    [SerializeField] private float curvatureGain;
    [SerializeField] private float rotationGain;
    [SerializeField] private float rotationStopOffset;
    [SerializeField] private bool debugOutput;

    private Vector3 rdwOrigin;

    // curvature-based RDW
    private bool rdwCurvatureActive;
    private bool redirectLeft;
    private float currentAngle;

    // rotation-based RDW
    private bool rdwRotationActive;
    private bool rotateLeft;
    private Vector3 lastForward;

    private void FixedUpdate()
    {
        if (this.rdwCurvatureActive) this.UpdateCurvatureRDW();
        else if (this.rdwRotationActive) this.UpdateRotationRDW();        
    }

    // update for the curvature-based RDW
    private void UpdateCurvatureRDW()
    {
        // apply rotation based on the distance from the origin
        float newAngle = (this.centerEyeAnchor.position - this.rdwOrigin).magnitude * this.curvatureGain;
        if (this.redirectLeft) newAngle *= -1;
        this.RotateModel(newAngle - this.currentAngle);
        this.currentAngle = newAngle;

        if (this.debugOutput)
        {
            Debug.Log($"Distance: {(this.centerEyeAnchor.position - this.rdwOrigin).magnitude}");
            Debug.Log($"New angle: {newAngle}");
        }
    }

    // update for the rotation-based RDW
    private void UpdateRotationRDW()
    {
        // amplify the user's rotation
        Vector3 currentForward = Utils.WithoutY(this.centerEyeAnchor.forward);
        float angle = Vector3.Angle(currentForward, this.lastForward);
        if (this.rotateLeft) angle *= -1;
        this.RotateModel(angle * this.rotationGain);

        // update the stored forward axis
        this.lastForward = currentForward;

        // check, whether the rotation should be stopped (based on the distance the user moved)
        float distance = Utils.WithoutY(this.centerEyeAnchor.position - this.rdwOrigin).magnitude;
        if (this.rotationStopOffset > 0 && distance >= this.rotationStopOffset) this.StopRotationRDW();
    }

    // start curvature-based RDW with the player's current position as the origin
    public void StartCurvatureRDW()
    {
        this.rdwOrigin = this.centerEyeAnchor.position;
        this.currentAngle = 0;
        this.rdwCurvatureActive = true;
    }

    // set which direction the next curvature RDW should use
    public void SetCurvatureDirection(bool dir)
    {
        this.redirectLeft = dir;
    }

    // set which direction the next rotation RDW should use
    public void SetRotationDirection(bool dir)
    {
        this.rotateLeft = dir;
    }

    // flip the direction for the next rotation RDW
    public void FlipRotationDirection()
    {
        this.rotateLeft = !this.rotateLeft;
    }

    // stop the curvature-based RDW
    public void StopCurvatureRDW()
    {
        this.rdwCurvatureActive = false;
    }

    // start rotation-based RDW
    public void StartRotationRDW()
    {
        this.rdwRotationActive = true;

        // initialize with the current forward axis and position
        this.lastForward = Utils.WithoutY(this.centerEyeAnchor.forward);
        this.rdwOrigin = this.centerEyeAnchor.position;
    }

    // start rotation-based RDW with the given rotation gain
    public void StartRotationRDW(float gain)
    {
        this.rdwRotationActive = true;

        // set the given gain, rotateLeft should be false so the sign isn't flipped
        this.rotationGain = gain;
        this.rotateLeft = false;

        // initialize with the current forward axis and position
        this.lastForward = Utils.WithoutY(this.centerEyeAnchor.forward);
        this.rdwOrigin = this.centerEyeAnchor.position;
    }

    // stop the rotation-based RDW
    public void StopRotationRDW()
    {
        this.rdwRotationActive = false;
    }

    // rotate the model around the up-axis
    private void RotateModel(float angle)
    {
        this.modelParent.RotateAround(this.centerEyeAnchor.position, Vector3.up, angle);
    }

    // switch from rotation- to curvature-RDW, once the user has turned around
    public void SwitchFromRotationToCurvature() // TODO MAYBE: call this method? Only if we want to use curvature after the rotation
    {
        if (!this.rdwRotationActive) return;

        this.StopRotationRDW();
        this.StartCurvatureRDW();
    }
}
