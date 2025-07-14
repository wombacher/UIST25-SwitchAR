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

public class SingleModelManager : AbstractModelManager
{
    [System.Serializable]
    private struct MaterialOverride
    {
        public MeshRenderer meshRenderer;
        public Material opaqueMaterial;
        public Material transparentMaterial;
        public Color color;

        public MaterialOverride(MeshRenderer mr, Material opaque, Material transparent, Color color)
        {
            this.meshRenderer = mr;
            this.opaqueMaterial = opaque;
            this.transparentMaterial = transparent;
            this.color = color;
        }

        public void UpdateMaterialAlpha(float alpha)
        {
            this.color.a = alpha;
            this.meshRenderer.material.color = this.color;
        }
    }

    private enum FadingState { none, fadingIn, fadingOut }

    [Header("References")]
    [SerializeField] private GameObject model;
    [SerializeField] private Transform modelParent;
    [SerializeField] private GameObject[] paintings;
    [SerializeField] private Material fineTuningMaterialTranslation;
    [SerializeField] private Material fineTuningMaterialRotation;

    [Header("Materials")]
    [SerializeField] private Material opaqueModelMaterial;
    [SerializeField] private Material transparentModelMaterial;

    [Header("Settings")]
    [SerializeField] private float modelFadeDuration;
    [SerializeField] private MaterialOverride[] materialOverrides;

    // References
    private MeshRenderer[] modelMeshRenderers;

    // Fading
    private FadingState fadingState = FadingState.none;
    private float fadeDurationRemaining;

    private void Start()
    {
        // get references
        this.modelMeshRenderers = this.model.GetComponentsInChildren<MeshRenderer>();
    }

    private void Update()
    {
        if (this.fadingState == FadingState.none) return;

        // update the alpha based on the remaining time
        this.UpdateModelMaterialAlpha();
        this.fadeDurationRemaining -= Time.deltaTime;

        // deactivate the model if it finished fading out, change it to the opaque material if it finished fading in
        if (this.fadeDurationRemaining <= 0)
        {
            if (this.fadingState == FadingState.fadingIn) this.ApplyOpaqueMaterial();
            else this.model.SetActive(false);

            // reset the fading state
            this.fadingState = FadingState.none;
        }
    }

    // update the alpha of the model's material based on the remaining fading duration
    private void UpdateModelMaterialAlpha()
    {
        Color color = this.modelMeshRenderers[0].material.color;
        float fraction = (this.modelFadeDuration - this.fadeDurationRemaining) / this.modelFadeDuration;
        color.a = this.fadingState == FadingState.fadingIn ? fraction : 1f - fraction;
        foreach (MeshRenderer mr in this.modelMeshRenderers) mr.material.color = color;
        foreach (MaterialOverride mo in this.materialOverrides) mo.UpdateMaterialAlpha(color.a);
    }


    // toggle visibility of the model
    public override void ToggleModelVisibility(bool fade=false)
    {
        if (fade)
        {
            this.fadingState = this.model.activeInHierarchy ? FadingState.fadingOut : FadingState.fadingIn;
            this.fadeDurationRemaining = this.modelFadeDuration;
            this.model.SetActive(true);
            this.ApplyTransparentMaterial();
        }
        else
        {
            Utils.ToggleGameObject(this.model);
            if (this.model.activeInHierarchy) this.ApplyOpaqueMaterial();
        }
    }

    // check whether the model is currently visible
    public override bool IsModelVisible()
    {
        return this.model.activeInHierarchy;
    }

    // (de-)activate the paintings
    public override void TogglePaintings()
    {
        foreach (GameObject painting in this.paintings) Utils.ToggleGameObject(painting);
    }

    // (de-)activate the paintings
    public override void SetPaintingsActive(bool active)
    {
        foreach (GameObject painting in this.paintings) painting.SetActive(active);
    }

    // show the model with the fine tuning material
    public override void ShowWithFineTuningMaterial()
    {
        if (!this.IsModelVisible()) this.ToggleModelVisibility();
        foreach (MeshRenderer mr in this.modelMeshRenderers) mr.material = this.fineTuningMaterialTranslation;
    }

    // switch between the two fine tuning materials
    public override void AssignFineTuningMaterial(bool rotationMode)
    {
        foreach (MeshRenderer mr in this.modelMeshRenderers) mr.material = rotationMode ? this.fineTuningMaterialRotation : this.fineTuningMaterialTranslation;
    }

    // restore the model's original opaque material
    public override void ApplyOpaqueMaterial()
    {
        foreach (MeshRenderer mr in this.modelMeshRenderers) mr.material = this.opaqueModelMaterial;
        foreach (MaterialOverride mo in this.materialOverrides) mo.meshRenderer.material = mo.opaqueMaterial;
    }

    // apply the models' transparent materials
    private void ApplyTransparentMaterial()
    {
        foreach (MeshRenderer mr in this.modelMeshRenderers) mr.material = this.transparentModelMaterial;
        foreach (MaterialOverride mo in this.materialOverrides) mo.meshRenderer.material = mo.transparentMaterial;
    }

    public override void LoadStyleTransferModel() { }

    public override void LockModel(int index) { }

    public override int GetModelCount()
    {
        return 1;
    }

    public override void LoadHighResModel() { }

    public override void LockCurrentModel() { }

    public override void UnlockModel() { }

    public override void TogglePosterMask() { }
}
