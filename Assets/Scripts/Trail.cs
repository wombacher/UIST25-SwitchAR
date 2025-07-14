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

public class Trail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform centerEyeAnchor;

    private LineRenderer lineRenderer;
    private Color visibleColor;
    private Color hiddenColor;
    private List<Vector3> positions;

    private void Start()
    {
        // get references
        this.lineRenderer = this.GetComponent<LineRenderer>();
        this.visibleColor = this.lineRenderer.material.color;
        this.hiddenColor = this.lineRenderer.material.color;
        this.hiddenColor.a = 0;
        this.positions = new List<Vector3>();

        this.Hide();
    }

    // hide the trail
    private void Hide()
    {
        this.lineRenderer.material.color = this.hiddenColor;
    }

    // show the trail
    private void Show()
    {
        this.lineRenderer.material.color = this.visibleColor;
    }

    // toggle the trail
    public void Toggle()
    {
        if (this.lineRenderer.material.color.a == 0) this.Show();
        else this.Hide();
    }

    // add the current position to the trail
    public void AddPosition()
    {
        this.positions.Add(Utils.WithoutY(this.centerEyeAnchor.position));
        this.lineRenderer.positionCount = this.positions.Count;
        this.lineRenderer.SetPositions(this.positions.ToArray());
    }

    // clear all positions and hide the trail
    public void ClearAndHide()
    {
        this.positions.Clear();
        this.lineRenderer.positionCount = 0;
        this.lineRenderer.SetPositions(new Vector3[] { });
        this.Hide();
    }
}
