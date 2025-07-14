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

public class ModelManager : AbstractModelManager
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private List<GameObject> models;
    [SerializeField] private GameObject[] posterMasks;
    [SerializeField] private GameObject[] paintings;
    [SerializeField] private Material fineTuningMaterialTranslation;
    [SerializeField] private Material fineTuningMaterialRotation;

    [Header("Parents for models loaded from asset bundles")]
    [SerializeField] private Transform highResModelParent;
    [SerializeField] private Transform styleTransferModelParent;

    [Header("Settings")]
    [SerializeField] private bool useHighGutterInsteadOfHighRes;

    // References
    private AssetBundleManager assetBundleManager;
    private Material originalModelMaterial;

    private bool posterMaskVisible;
    private int lockedModelIndex = -1;

    private void Start()
    {
        // load references
        this.assetBundleManager = this.GetComponent<AssetBundleManager>();
    }

    // toggle visibility of the meshes/models
    public override void ToggleModelVisibility(bool fade=false)
    {
        if (this.lockedModelIndex != -1) this.ToggleLockedModel();
        else this.CycleModels();
    }

    // check whether a model is currently visible
    public override bool IsModelVisible()
    {
        foreach (GameObject model in this.models) if (model.activeInHierarchy) return true;
        return false;
    }

    // cycle to the next model
    private void CycleModels()
    {
        // determine model to show next
        int indexToActivate = 0;
        for (int i = 0; i < this.models.Count; i++)
        {
            if (this.models[i].activeInHierarchy)
            {
                this.models[i].SetActive(false);
                this.posterMasks[i].SetActive(false);
                if (i + 1 < this.models.Count) indexToActivate = i + 1;
                else indexToActivate = -1; // last model was active, show no model next

                break;
            }
        }

        // activate the next model
        if (indexToActivate >= 0)
        {
            this.models[indexToActivate].SetActive(true);
            this.posterMasks[indexToActivate].SetActive(this.posterMaskVisible);
        }

        this.SetCurrentModelText(indexToActivate);
    }

    // toggle the currently locked model
    private void ToggleLockedModel()
    {
        Utils.ToggleGameObject(this.models[this.lockedModelIndex]);

        // show the model's name if it is active, otherwise show an empty string (index -1)
        this.SetCurrentModelText(this.models[this.lockedModelIndex].activeInHierarchy ? this.lockedModelIndex : -1);
    }

    // update the model name displayed in the UI
    private void SetCurrentModelText(int modelIndex)
    {
        // clear UI text if no model is shown (index -1)
        string text = modelIndex == -1 ? "" : this.models[modelIndex].name;
        if (this.uiManager != null) this.uiManager.SetCurrentModelText(text);
    }

    // load the high resolution model from its asset bundle
    public override void LoadHighResModel()
    {
        if (this.useHighGutterInsteadOfHighRes) this.LoadHighGutterModel();
        else this.assetBundleManager.LoadHighResModel(this.InstantiateHighResModel, () => this.uiManager.OnHighResModelLoadingFailed());
    }

    // load the high gutter model from its asset bundle
    private void LoadHighGutterModel()
    {
        // re-use the callbacks for the high res model, as the high gutter model is supposed to be a replacement for it
        this.assetBundleManager.LoadHighGutterModel(this.InstantiateHighResModel, () => this.uiManager.OnHighResModelLoadingFailed());
    }

    // load the style transfer model from its asset bundle
    public override void LoadStyleTransferModel()
    {
        this.assetBundleManager.LoadStyleTransferModel(this.InstantiateStyleTransferModel, () => this.uiManager.OnStyleTransferModelLoadingFailed());
    }

    // instantiate the given prefab of the high res model and add it to the list of meshes
    private void InstantiateHighResModel(GameObject prefab)
    {
        GameObject model = Instantiate(prefab, this.highResModelParent);
        model.SetActive(false);
        this.models.Add(model);

        // update the high res model button
        this.uiManager.OnHighResModelLoaded();
    }

    // instantiate the given prefab of the style transfer model and add it to the list of meshes
    private void InstantiateStyleTransferModel(GameObject prefab)
    {
        GameObject model = Instantiate(prefab, this.styleTransferModelParent);
        model.SetActive(false);
        this.models.Add(model);

        // update the high res model button
        this.uiManager.OnStyleTransferModelLoaded();
    }

    // (de-)activate the poster mask
    public override void TogglePosterMask()
    {
        this.posterMaskVisible = !this.posterMaskVisible;
        for (int i = 0; i < this.models.Count; i++) if (this.models[i].activeInHierarchy) this.posterMasks[i].SetActive(this.posterMaskVisible);
    }

    // toggle between current model and no model instead of cycling all models
    public override void LockCurrentModel()
    {
        for (int i = 0; i < this.models.Count; i++)
        {
            if (this.models[i].activeInHierarchy)
            {
                this.lockedModelIndex = i;
                break;
            }
        }
    }

    // lock the model with the given index
    public override void LockModel(int index)
    {
        this.lockedModelIndex = index;
    }

    // cycle all models again instead of locking one specific model
    public override void UnlockModel()
    {
        this.lockedModelIndex = -1;
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

    // get the amount of models loaded
    public override int GetModelCount()
    {
        return this.models.Count;
    }

    // show the model with the fine tuning material
    public override void ShowWithFineTuningMaterial()
    {
        if (!this.IsModelVisible()) this.ToggleLockedModel();

        MeshRenderer mr = this.models[this.lockedModelIndex].GetComponentInChildren<MeshRenderer>();
        this.originalModelMaterial = mr.material;
        mr.material = this.fineTuningMaterialTranslation;
    }

    // switch between the two fine tuning materials
    public override void AssignFineTuningMaterial(bool rotationMode)
    {
        MeshRenderer mr = this.models[this.lockedModelIndex].GetComponentInChildren<MeshRenderer>();
        mr.material = rotationMode ? this.fineTuningMaterialRotation : this.fineTuningMaterialTranslation;
    }

    // restore the model's original material
    public override void ApplyOpaqueMaterial()
    {
        MeshRenderer mr = this.models[this.lockedModelIndex].GetComponentInChildren<MeshRenderer>();
        mr.material = this.originalModelMaterial;
    }
}
