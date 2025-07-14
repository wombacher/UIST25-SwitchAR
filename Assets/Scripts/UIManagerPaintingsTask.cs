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

public class UIManagerPaintingsTask : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform paintingTimerParent;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text paintingTimerText;

    [Header("UI Texts")]
    [SerializeField] private string modelLoadingInstruction;
    [SerializeField] private string[] alignmentInstructions;
    [SerializeField] private string[] paintingsInstructions;
    [SerializeField] private string takeOffInstruction;

    // show loading instruction to the user
    public void ShowLoadingInstruction()
    {
        this.instructionText.text = this.modelLoadingInstruction;
    }

    // show loading instruction with the given index to the user
    public void ShowAlignmentInstruction(int index)
    {
        this.instructionText.text = this.alignmentInstructions[index];
    }

    // show paintings instruction with the given index to the user
    public void ShowPaintingsInstruction(int index)
    {
        this.instructionText.text = this.paintingsInstructions[index];
    }

    // update timer text to show the given timer value
    public void UpdateTimerText(float timer)
    {
        this.timerText.text = $"{Mathf.Max(0, timer):#0.0}";
    }

    // place the painting timer canvas at the position of the given transform (also applies the transform's rotation)
    public void PositionPaintingTimer(Transform transform)
    {
        Transform timerCanvas = this.paintingTimerText.transform.parent;

        // make sure the canvas is a child of the object being manipulated by the RDW
        if (timerCanvas.parent != this.paintingTimerParent) timerCanvas.parent = this.paintingTimerParent;

        // make sure the canvas is active
        timerCanvas.gameObject.SetActive(true);

        // update position and rotation of the canvas
        timerCanvas.position = transform.position;
        timerCanvas.rotation = transform.rotation;
    }

    // update painting timer text to show the given timer value
    public void UpdatePaintingTimerText(float timer)
    {
        this.paintingTimerText.text = $"{Mathf.Max(0, timer):#0.0}";
    }

    // clear the painting timer text
    public void ClearPaintingTimerText()
    {
        this.paintingTimerText.text = "";
    }

    // clear the instructions text
    public void ClearInstructionsText()
    {
        this.instructionText.text = "";
    }

    // set visibility of the instructions text
    public void SetInstructionsVisibility(bool visible)
    {
        this.instructionText.gameObject.SetActive(visible);
    }

    // instruct the user to take off the headset
    public void ShowTakeOffInstruction()
    {
        this.instructionText.text = this.takeOffInstruction;
    }
}
