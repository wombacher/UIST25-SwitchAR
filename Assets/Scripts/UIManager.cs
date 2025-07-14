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
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Alignment alignment;
    [SerializeField] private AbstractModelManager modelManager;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject[] uiTabs;

    [Header("UI Elements")]
    [SerializeField] private Button spatialAnchorButton;
    [SerializeField] private Button highResModelButton;
    [SerializeField] private Button styleTransferModelButton;
    [SerializeField] private Button posterMaskButton;
    [SerializeField] private Button paintingsButton;
    [SerializeField] private Button modelLockButton;
    [SerializeField] private Button modelUnlockButton;
    [SerializeField] private Button noiseToggleButton;
    [SerializeField] private Button previousGrainButton;
    [SerializeField] private Button nextGrainButton;
    [Space(10)]
    [SerializeField] private Slider grainSlider;
    [SerializeField] private Slider fpsSlider;
    [SerializeField] private Slider counterMovementSlider;
    [Space(10)]
    [SerializeField] private Toggle setupTabRadio;
    [SerializeField] private Toggle modelsTabRadio;
    [SerializeField] private Toggle noiseTabRadio;
    [Space(10)]
    [SerializeField] private Toggle grainToggle;
    [SerializeField] private Toggle fpsToggle;
    [SerializeField] private Toggle grainTextureToggle;
    [SerializeField] private Toggle countermovementToggle;
    [Space(10)]
    [SerializeField] private TMP_Text grainTextureText;
    [SerializeField] private TMP_Text currentModelText;

    [Header("Settings")]
    [SerializeField] private float buttonTextUpdateDelay;
    [SerializeField] private string modelLoadingHint;
    [SerializeField] private string modelLoadedHint;
    [SerializeField] private string modelLoadingFailedHint;

    // references
    private NoiseManager noiseManager;
    private TMP_Text highResModelButtonText;
    private TMP_Text styleTransferModelButtonText;

    // model loading
    private bool highResModelLoading = false;
    private bool styleTransferModelLoading = false;
    private float highResLoadingStartedAt;
    private float styleTransferLoadingStartedAt;

    // button updates
    private float lastButtonTextUpdate;

    private Dictionary<string, string> modelNames = new Dictionary<string, string>()
    {
        { "HCI_Lab_v2", "old low res model"},
        { "HCI_Lab_v2_12x16k", "high res model" },
        { "HCI_Lab_v2_high_gutter", "high gutter model" },
        { "HCI_Lab_with_style_transfer_v1", "style transfer model" }
    };

    private void Start()
    {
        this.lastButtonTextUpdate = Time.time;
        this.currentModelText.text = "";

        // add listeners to the UI elements
        this.AddListeners();

        // get references
        this.noiseManager = this.GetComponent<NoiseManager>();
        this.highResModelButtonText = this.highResModelButton.GetComponentInChildren<TMP_Text>();
        this.styleTransferModelButtonText = this.styleTransferModelButton.GetComponentInChildren<TMP_Text>();
    }

    // add listeners to the UI elements
    private void AddListeners()
    {
        // buttons
        this.spatialAnchorButton.onClick.AddListener(this.LoadSpatialAnchors);
        this.highResModelButton.onClick.AddListener(this.LoadHighResModel);
        this.styleTransferModelButton.onClick.AddListener(this.LoadStyleTransferModel);
        this.posterMaskButton.onClick.AddListener(this.modelManager.TogglePosterMask);
        this.paintingsButton.onClick.AddListener(this.modelManager.TogglePaintings);
        this.modelLockButton.onClick.AddListener(this.LockCurrentModel);
        this.modelUnlockButton.onClick.AddListener(this.UnlockModel);
        this.noiseToggleButton.onClick.AddListener(this.ToggleNoise);
        this.previousGrainButton.onClick.AddListener(this.PreviousGrainTexture);
        this.nextGrainButton.onClick.AddListener(this.NextGrainTexture);

        // sliders
        this.grainSlider.onValueChanged.AddListener(this.SetGrain);
        this.fpsSlider.onValueChanged.AddListener(this.SetFps);
        this.counterMovementSlider.onValueChanged.AddListener(this.SetGrainOffsetMultiplier);

        // radios
        this.setupTabRadio.onValueChanged.AddListener(this.SetSetupTabVisibility);
        this.modelsTabRadio.onValueChanged.AddListener(this.SetModelsTabVisibility);
        this.noiseTabRadio.onValueChanged.AddListener(this.SetNoiseTabVisibility);

        // toggles
        this.grainToggle.onValueChanged.AddListener(this.SetGrainOverride);
        this.fpsToggle.onValueChanged.AddListener(this.SetFpsOverride);
        this.grainTextureToggle.onValueChanged.AddListener(this.SetGrainTextureOverride);
        this.countermovementToggle.onValueChanged.AddListener(this.SetGrainOffsetOverride);
    }

    private void Update()
    {
        if (Time.time - this.lastButtonTextUpdate >= this.buttonTextUpdateDelay)
        {
            this.UpdateButtonTexts();
            this.lastButtonTextUpdate = Time.time;
        }
    }

    // toggle the visibility of the settings tab
    public void ToggleSettingsMenu()
    {
        Utils.ToggleGameObject(this.settingsMenu);
    }

    // update the button texts to indicate actions in progress
    private void UpdateButtonTexts()
    {
        if (this.highResModelLoading)
        {
            // increase number of trailings dots (clamped to 3 max)
            int newDotCount = this.highResModelButtonText.text.Split('.').Length;
            this.highResModelButtonText.text = modelLoadingHint + new string('.', newDotCount % 4);
        }
        if (this.styleTransferModelLoading)
        {
            // increase number of trailings dots (clamped to 3 max)
            int newDotCount = this.styleTransferModelButtonText.text.Split('.').Length;
            this.styleTransferModelButtonText.text = modelLoadingHint + new string('.', newDotCount % 4);
        }
    }

    // load spatial anchors for alignment
    private void LoadSpatialAnchors()
    {
        this.alignment.LoadPositionsFromSpatialAnchors();
        this.spatialAnchorButton.interactable = false;
    }

    // load the high res model from its asset bundle
    private void LoadHighResModel()
    {
        // initiate loading the model
        this.modelManager.LoadHighResModel();
        this.highResLoadingStartedAt = Time.time;

        // change button appearance to indicate ongoing loading
        this.highResModelButton.interactable = false;
        this.highResModelLoading = true;
        this.highResModelButtonText.text = this.modelLoadingHint;
        this.highResModelButtonText.alignment = TextAlignmentOptions.Left;
    }

    // load the style transfer model from its asset bundle
    private void LoadStyleTransferModel()
    {
        // initiate loading the model
        this.modelManager.LoadStyleTransferModel();
        this.styleTransferLoadingStartedAt = Time.time;

        // change button appearance to indicate ongoing loading
        this.styleTransferModelButton.interactable = false;
        this.styleTransferModelLoading = true;
        this.styleTransferModelButtonText.text = this.modelLoadingHint;
        this.styleTransferModelButtonText.alignment = TextAlignmentOptions.Left;
    }

    // update text of the button, once the high res model was loaded
    public void OnHighResModelLoaded()
    {
        // stop automatic text update
        this.highResModelLoading = false;

        // update button text
        this.highResModelButtonText.text = this.modelLoadedHint + $" ({((int)(Time.time - this.highResLoadingStartedAt))}s)";
        this.highResModelButtonText.alignment = TextAlignmentOptions.Center;

        // enable the spatial anchor button
        this.spatialAnchorButton.interactable = true;
    }

    // update text of the button, once the style transfer model was loaded
    public void OnStyleTransferModelLoaded()
    {
        // stop automatic text update
        this.styleTransferModelLoading = false;

        // update button text
        this.styleTransferModelButtonText.text = this.modelLoadedHint + $" ({((int)(Time.time - this.styleTransferLoadingStartedAt))}s)";
        this.styleTransferModelButtonText.alignment = TextAlignmentOptions.Center;

        // enable the spatial anchor button
        this.spatialAnchorButton.interactable = true;
    }

    // update text of the button, once the high res model failed to loaded
    public void OnHighResModelLoadingFailed()
    {
        // stop automatic text update
        this.highResModelLoading = false;

        // update button text
        this.highResModelButtonText.text = this.modelLoadingFailedHint;
        this.highResModelButtonText.alignment = TextAlignmentOptions.Center;

        // enable the spatial anchor button
        this.spatialAnchorButton.interactable = true;
    }

    // update text of the button, once the style transfer model failed to loaded
    public void OnStyleTransferModelLoadingFailed()
    {
        // stop automatic text update
        this.styleTransferModelLoading = false;

        // update button text
        this.styleTransferModelButtonText.text = this.modelLoadingFailedHint;
        this.styleTransferModelButtonText.alignment = TextAlignmentOptions.Center;

        // enable the spatial anchor button
        this.spatialAnchorButton.interactable = true;
    }

    // set the visibility of the setup tab
    private void SetSetupTabVisibility(bool visible)
    {
        this.uiTabs[0].SetActive(visible);
    }

    // set the visibility of the models tab
    private void SetModelsTabVisibility(bool visible)
    {
        this.uiTabs[1].SetActive(visible);
    }

    // set the visibility of the noise tab
    private void SetNoiseTabVisibility(bool visible)
    {
        this.uiTabs[2].SetActive(visible);
    }

    // toggle overall noise
    private void ToggleNoise()
    {
        this.noiseManager.ToggleNoiseVolume();
    }

    // dis-/enable grain setting override
    private void SetGrainOverride(bool value)
    {
        this.noiseManager.SetGrainOverride(value);
    }

    // adjust grain setting
    private void SetGrain(float grain)
    {
        this.noiseManager.SetGrain(grain);
    }

    // dis-/enable fpssetting override
    private void SetFpsOverride(bool value)
    {
        this.noiseManager.SetFpsOverride(value);
    }

    // adjust fps setting
    private void SetFps(float fps)
    {
        this.noiseManager.SetFps(fps);
    }

    // dis-/enable grain texture setting override
    private void SetGrainTextureOverride(bool value)
    {
        this.noiseManager.SetGrainTextureOverride(value);
    }

    // change to next grain texture
    private void NextGrainTexture()
    {
        this.noiseManager.NextGrainTexture();
    }

    // change to previous grain texture
    private void PreviousGrainTexture()
    {
        this.noiseManager.PreviousGrainTexture();
    }

    // set the UI elements on the noise tab to the initial values
    public void InitializeNoiseTab(bool grainOverride, float grain, bool fpsOverride, float fps, bool textureOverride, string texture, bool countermovement, float countermovementMultiplier)
    {
        this.grainToggle.SetIsOnWithoutNotify(grainOverride);
        this.grainSlider.SetValueWithoutNotify(grain);

        this.fpsToggle.SetIsOnWithoutNotify(fpsOverride);
        this.fpsSlider.SetValueWithoutNotify(fps);

        this.grainTextureToggle.SetIsOnWithoutNotify(textureOverride);
        this.UpdateGrainTextureText(texture);

        this.countermovementToggle.SetIsOnWithoutNotify(countermovement);
        this.counterMovementSlider.SetValueWithoutNotify(countermovementMultiplier);
    }

    // display the given grain texture name on the according text field
    public void UpdateGrainTextureText(string textureName)
    {
        this.grainTextureText.text = textureName;
    }

    // dis-/enable grain countermovement
    private void SetGrainOffsetOverride(bool value)
    {
        this.noiseManager.SetGrainOffsetOverride(value);
    }

    // update the multiplier for the grain countermovement
    private void SetGrainOffsetMultiplier(float value)
    {
        this.noiseManager.SetCameraMovementMultiplier(value);
    }

    // set the text indicating the currently displayed model
    public void SetCurrentModelText(string text)
    {
        // remove trailing clone-part
        text = text.Split("(Clone)")[0];

        // lookup text in the model name dictionary and replace it with its value
        if (this.modelNames.ContainsKey(text)) text = this.modelNames[text];

        // apply the text
        this.currentModelText.text = text;
    }

    // lock the current model and update the according button interactabilities
    private void LockCurrentModel()
    {
        this.modelManager.LockCurrentModel();
        this.UpdateModelLockButtons(true);
    }

    // unlock the model and update the according button interactabilities
    private void UnlockModel()
    {
        this.modelManager.UnlockModel();
        this.UpdateModelLockButtons(false);
    }

    // update the interactabilities of the lock-/unlock-buttons
    public void UpdateModelLockButtons(bool locked)
    {
        this.modelLockButton.interactable = !locked;
        this.modelUnlockButton.interactable = locked;
    }
}
