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

public class CustomAvatarManager : MonoBehaviour
{
    private int layer;
    private int childCount;

    private void Start()
    {
        this.layer = this.gameObject.layer;
    }

    private void Update()
    {
        if (this.transform.childCount != this.childCount)
        {
            this.ApplyLayerToChildren();
            this.childCount = this.transform.childCount;
        }
    }

    // apply the configured layer to all children
    private void ApplyLayerToChildren()
    {
        for (int i = 0; i < this.transform.childCount; i++)
        {
            Transform child = this.transform.GetChild(i);
            child.gameObject.layer = this.layer;

            // also apply the layer to the grandchildren
            int grandchildCount = child.childCount;
            for (int j = 0; j < grandchildCount; j++) child.GetChild(j).gameObject.layer = this.layer;
        }
    }
}
