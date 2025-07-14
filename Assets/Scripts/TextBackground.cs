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

public class TextBackground : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private TMP_Text text;

    [Header("Settings")]
    [SerializeField] private Vector2 padding;
    [SerializeField] private Vector2 minSize;

    private bool textChanged = false;

    private void Start()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(this.UpdateBackgroundSize);
    }

    private void Update()
    {
        if (this.textChanged)
        {
            this.textChanged = false;
            Vector2 textSize = this.text.GetRenderedValues();
            Vector2 bgSize = new Vector2(textSize.x + this.padding.x * 2, textSize.y + this.padding.y * 2);
            this.backgroundImage.sizeDelta = new Vector2(Mathf.Max(bgSize.x, this.minSize.x), Mathf.Max(bgSize.y, this.minSize.y));
        }
    }

    private void UpdateBackgroundSize(Object obj)
    {
        if (obj != this.text) return;
        this.textChanged = true;
    }
}
