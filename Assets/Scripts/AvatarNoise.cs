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
using System.Linq;
using UnityEngine;

public class AvatarNoise : MonoBehaviour
{
    private struct NoiseAvatar
    {
        private Transform[] bones;
        private Vector2 perlinOrigin;
        private float maxOffset;
        private bool negativeOffset;

        public NoiseAvatar(Transform avatar, Vector2 perlinCoordinates, float maxOffset, bool negativeOffset)
        {
            this.bones = Utils.CollectBoneTransforms(avatar).ToArray();
            this.perlinOrigin = perlinCoordinates;
            this.maxOffset = maxOffset;
            this.negativeOffset = negativeOffset;
        }

        // set this avatar's bones' positions and rotations to the ones of the tracked avatar's bones, but with an added perlin noise offset
        public void UpdateBoneTransforms(Vector3[] trackedAvatarBonePositions, Quaternion[] trackedAvatarBoneRotations, float newPerlinOffset, Vector3 cameraRight)
        {
            float noise = Mathf.PerlinNoise(this.perlinOrigin.x + newPerlinOffset, this.perlinOrigin.y) * (this.negativeOffset ? -1 : 1);
            for (int i = 0; i < this.bones.Length; i++)
            {
                this.bones[i].rotation = trackedAvatarBoneRotations[i];
                this.bones[i].position = trackedAvatarBonePositions[i] + cameraRight * noise * this.maxOffset;
            }
        }
    }

    [Header("References")]
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private Transform trackedAvatar;
    [SerializeField] private Transform[] noiseAvatarTransforms;

    [Header("Settings")]
    [SerializeField] private float maxOffset = 1;
    [SerializeField] private float perlinStepSize = 1;
    [SerializeField] private float perlinOriginOffset = 100;
    [SerializeField] private bool initiallyActive;

    private Transform[] trackedAvatarBones;
    private List<NoiseAvatar> noiseAvatars = new List<NoiseAvatar>();
    private float totalPerlinOffset;
    private bool active;

    private void Start()
    {
        // collect bones of the tracked avatar
        this.trackedAvatarBones = Utils.CollectBoneTransforms(this.trackedAvatar).ToArray();

        // collect bones of the noise avatars and initialize their perlin origin offset
        for (int i = 0; i < this.noiseAvatarTransforms.Length; i++)
        {
            this.noiseAvatars.Add(new NoiseAvatar(this.noiseAvatarTransforms[i], Vector2.zero + Vector2.up * this.perlinOriginOffset * i, this.maxOffset, i % 2 == 1));
        }

        // apply setting for initial visibility
        this.active = this.initiallyActive;
        foreach (Transform transform in this.noiseAvatarTransforms) transform.gameObject.SetActive(this.active);
    }

    private void Update()
    {
        // toggle visibility of the noise avatars
        if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick))
        {
            this.active = !this.active;
            foreach (Transform transform in this.noiseAvatarTransforms) transform.gameObject.SetActive(this.active);
        }

        if (this.active) this.UpdateNoiseAvatars();
        
    }

    private void UpdateNoiseAvatars()
    {
        // collect all of the tracked avatar's bones' positions and rotations
        Vector3[] trackedBonePositions = this.trackedAvatarBones.Select(transform => transform.position).ToArray();
        Quaternion[] trackedBoneRotations = this.trackedAvatarBones.Select(transform => transform.rotation).ToArray();

        // update the noise avatars' bones' positions and rotations
        Vector3 cameraRight = this.centerEyeAnchor.right;
        foreach (NoiseAvatar noiseAvatar in this.noiseAvatars)
        {
            this.totalPerlinOffset += Time.deltaTime * this.perlinStepSize;
            noiseAvatar.UpdateBoneTransforms(trackedBonePositions, trackedBoneRotations, this.totalPerlinOffset, cameraRight);
        }
    }
}
