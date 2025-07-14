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

public class Painting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer frame;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private SpriteRenderer placeholder;

    [Header("Materials")]
    [SerializeField] private Material frameBright;
    [SerializeField] private Material frameDark;
    [SerializeField] private Material spriteBright;
    [SerializeField] private Material spriteDark;

    [Header("Settings")]
    [SerializeField] private float interpolationDuration = 1f;
    [SerializeField] private bool darkAsDefault;
    [SerializeField] private bool placeholderAsDefault;

    private bool interpolating;

    private void Start()
    {
        // show either dark or bright materials
        if (this.darkAsDefault) this.ShowDark();
        else this.ShowBright();

        // show either the placeholder or the actual sprite
        if (this.placeholderAsDefault) this.ShowPlaceholder();
        else this.HidePlaceholder();
    }

    private void Update()
    {
        // blend between bright and dark
        if (this.interpolating)
        {
            float lerpVal = Mathf.PingPong(Time.time, this.interpolationDuration) / this.interpolationDuration;
            this.frame.material.Lerp(this.darkAsDefault ? this.frameDark : this.frameBright, this.darkAsDefault ? this.frameBright : this.frameDark, lerpVal);
            this.sprite.material.Lerp(this.darkAsDefault ? this.spriteDark : this.spriteBright, this.darkAsDefault ? this.spriteBright : this.spriteDark, lerpVal);
        }
    }

    // show the painting in its bright state
    public void ShowBright()
    {
        this.interpolating = false;
        this.frame.material = this.frameBright;
        this.sprite.material = this.spriteBright;
    }

    // show the painting in its dark state
    public void ShowDark()
    {
        this.interpolating = false;
        this.frame.material = this.frameDark;
        this.sprite.material = this.spriteDark;
    }

    // show the placeholder instead of the actual painting
    public void ShowPlaceholder()
    {
        this.sprite.enabled = false;
        this.placeholder.enabled = true;
    }

    // hide the placeholder
    public void HidePlaceholder()
    {
        this.sprite.enabled = true;
        this.placeholder.enabled = false;
    }

    // start interpolating between the two materials
    public void StartMaterialInterpolation()
    {
        this.interpolating = true;
    }
}
