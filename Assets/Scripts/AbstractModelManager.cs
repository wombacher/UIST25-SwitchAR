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

public abstract class AbstractModelManager: MonoBehaviour
{
    public abstract void ToggleModelVisibility(bool fade=false);
    public abstract void LoadHighResModel();
    public abstract void LoadStyleTransferModel();
    public abstract void LockModel(int index);
    public abstract void LockCurrentModel();
    public abstract void UnlockModel();
    public abstract void TogglePosterMask();
    public abstract void TogglePaintings();
    public abstract void SetPaintingsActive(bool active);
    public abstract bool IsModelVisible();
    public abstract int GetModelCount();
    public abstract void AssignFineTuningMaterial(bool rotationMode);
    public abstract void ShowWithFineTuningMaterial();
    public abstract void ApplyOpaqueMaterial();
}
