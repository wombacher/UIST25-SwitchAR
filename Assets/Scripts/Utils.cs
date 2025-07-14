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

public class Utils
{
    // toggle the active-state of the given game object
    public static void ToggleGameObject(GameObject go)
    {
        go.SetActive(!go.activeInHierarchy);
    }

    // return the given vector, but with the x-value set to 0
    public static Vector3 WithoutX(Vector3 vector)
    {
        vector.x = 0;
        return vector;
    }

    // return the given vector, but with the y-value set to 0
    public static Vector3 WithoutY(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    // return the given vector, while overriding all values, for which the parameter is not null
    public static Vector3 VectorOverride(Vector3 vector, float? x, float? y, float? z)
    {
        if (x.HasValue) vector.x = x.Value;
        if (y.HasValue) vector.y = y.Value;
        if (z.HasValue) vector.z = z.Value;

        return vector;
    }

    // recursively collect all bones contained below the given bone transform, including the bone itself
    public static List<Transform> CollectBoneTransforms(Transform bone)
    {
        List<Transform> bones = new List<Transform>() { bone };
        if (bone.childCount == 0) return bones;

        foreach (Transform child in bone)
        {
            bones.AddRange(CollectBoneTransforms(child));
        }

        return bones;
    }
}
