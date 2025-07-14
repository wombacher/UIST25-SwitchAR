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

public class ChangingPainting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer frame;
    [SerializeField] private SpriteRenderer placeholder;
    [SerializeField] private SpriteRenderer[] sprites;
    [SerializeField] private Transform timerTransform;

    [Header("Materials")]
    [SerializeField] private Material frameBright;
    [SerializeField] private Material frameDark;
    [SerializeField] private Material spriteBright;
    [SerializeField] private Material spriteDark;

    [Header("Settings")]
    [SerializeField] private float interpolationDuration = 1f;
    [SerializeField] private int initialSpriteIndex;

    private bool interpolating;
    private int nextSpriteIndex;

    private void Start()
    {
        // show dark materials and the placeholder
        this.ShowDark();
        this.ShowPlaceholder();
    }

    private void Update()
    {
        // blend between bright and dark
        if (this.interpolating)
        {
            float lerpVal = Mathf.PingPong(Time.time, this.interpolationDuration) / this.interpolationDuration;
            this.frame.material.Lerp(this.frameDark, this.frameBright, lerpVal);
        }
    }

    // get the transform used as an anchor to position the timer next to the painting
    public Transform GetTimerTransform()
    {
        return this.timerTransform;
    }

    // show the painting in its bright state
    private void ShowBright()
    {
        this.interpolating = false;
        this.frame.material = this.frameBright;
        foreach (SpriteRenderer sr in this.sprites) sr.material = this.spriteBright;
    }

    // show the painting in its dark state
    private void ShowDark()
    {
        this.interpolating = false;
        this.frame.material = this.frameDark;
        foreach (SpriteRenderer sr in this.sprites) sr.material = this.spriteDark;
    }

    // hide painting by activating the placeholder and showing dark materials
    public void HidePainting()
    {
        this.ShowPlaceholder();
        this.ShowDark();
    }

    // show the placeholder instead of the actual painting
    private void ShowPlaceholder()
    {
        foreach (SpriteRenderer sr in this.sprites) sr.enabled = false;
        this.placeholder.enabled = true;
    }

    // hide the placeholder
    private void HidePlaceholder()
    {
        this.placeholder.enabled = false;
    }

    // start interpolating between the two materials
    public void StartMaterialInterpolation()
    {
        this.interpolating = true;
    }

    // show the next sprite
    public void ShowNextPainting()
    {
        // go back to first sprite if the last one was reached
        if (this.nextSpriteIndex >= this.sprites.Length) this.nextSpriteIndex = 0;

        // disable previous sprite in case it is still active
        this.sprites[this.nextSpriteIndex == 0 ? this.sprites.Length - 1 : this.nextSpriteIndex - 1].enabled = false;

        // activate the new sprite
        this.sprites[this.nextSpriteIndex].enabled = true;

        // hide the placeholder and show bright materials
        this.HidePlaceholder();
        this.ShowBright();

        // increment the index
        this.nextSpriteIndex++;
    }

    // reset the index to start at the first painting again
    public void ResetIndex()
    {
        this.nextSpriteIndex = 0;
    }
}
