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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRSpatialAnchor;

public class SpatialAnchorManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject alignmentAnchorPrefab;

    // references
    private Alignment alignment;

    // alignment anchors
    private OVRSpatialAnchor[] alignmentAnchors = new OVRSpatialAnchor[] { null, null };
    private OVRSpatialAnchor parentAnchor; // TODO: complete the single parent anchor implementation
    private Guid[] alignmentAnchorUuids = new Guid[] { Guid.Empty, Guid.Empty };
    private Guid parentAnchorUuid;
    private string[] playerPrefsAlignmentAnchorKeys = new string[] { "alignmentAnchor0", "alignmentAnchor1" };
    private string playerPrefsParentAnchorKey = "parentAnchor";
    private string playerPrefsParentScaleKey = "parentScale";
    
    private bool alignmentAnchorsLoadedFromPlayerPrefs = false;

    private void Start()
    {
        this.alignment = this.GetComponent<Alignment>();
    }

    public void InitForThreePointAlignment()
    {
        this.alignmentAnchors = new OVRSpatialAnchor[] { null, null, null };
        this.alignmentAnchorUuids = new Guid[] { Guid.Empty, Guid.Empty, Guid.Empty };
        this.playerPrefsAlignmentAnchorKeys = new string[] { "alignmentAnchor0", "alignmentAnchor1", "alignmentAnchor2" };
    }

    // create a new spatial anchor for the given alignment position
    public void CreateAlignmentAnchor(int index, Vector3 position, Action callback=null)
    {
        // create new game object for the anchor, position it as required and add the spatial anchor component
        GameObject anchor = new GameObject("alignment anchor " + index);
        anchor.transform.position = position;
        this.alignmentAnchors[index] = anchor.AddComponent<OVRSpatialAnchor>();

        // store the new anchor's uuid once it has been initialized
        this.alignmentAnchorUuids[index] = Guid.Empty;
        StartCoroutine(anchorCreated(this.alignmentAnchors[index], index, callback));
    }

    // create a new spatial anchor for the given position and rotation of the model parent
    public void CreateParentAnchor(Vector3 position, Quaternion rotation, Vector3 scale, Action callback=null)
    {
        // create anchor
        GameObject anchor = new GameObject("parent anchor");
        anchor.transform.position = position;
        anchor.transform.rotation = rotation;
        this.parentAnchor = anchor.AddComponent<OVRSpatialAnchor>();

        // write the scale to the player prefs separately
        PlayerPrefs.SetString(this.playerPrefsParentScaleKey, JsonUtility.ToJson(scale));

        // store the new anchor's uuid once it has been initialized
        this.parentAnchorUuid = Guid.Empty;
        StartCoroutine(anchorCreated(this.parentAnchor, -1, callback));
    }

    // store the anchor's uuid for later use, once the anchor has been initialized
    private IEnumerator anchorCreated(OVRSpatialAnchor anchor, int index, Action callback)
    {
        // keep checking for a valid and localized spatial anchor state
        while (!anchor.Created || !anchor.Localized) yield return new WaitForEndOfFrame();

        if (index < 0) this.parentAnchorUuid = anchor.Uuid;
        else this.alignmentAnchorUuids[index] = anchor.Uuid;

        // reset flag, as a new anchor was created
        this.alignmentAnchorsLoadedFromPlayerPrefs = false;

        // execute callback, if there is one
        if (callback != null) callback();
    }

    // write the alignment anchors to the headset's memory
    public IEnumerator SaveAlignmentAnchors()
    {
        // only save anchors, if they were not loaded from the player prefs
        if (this.alignmentAnchorsLoadedFromPlayerPrefs) yield break;

        // wait until all anchors have been initialized
        while (!this.AllAlignmentAnchorsInitialized()) yield return new WaitForEndOfFrame();

        // choose local storage (on the headset) only, not the cloud
        SaveOptions saveOptions = new SaveOptions();
        saveOptions.Storage = OVRSpace.StorageLocation.Local;
        
        // save the anchors
        for (int i = 0; i < this.alignmentAnchors.Length; i++)
        {
            this.alignmentAnchors[i].SaveAsync(saveOptions);  // TODO MAYBE: check for success here, only write uuids to the player prefs, if saving was successful
        }

        this.WriteAlignmentAnchorUuidsToPlayerPrefs();        
    }

    // check, if all alignment anchors have been initialized
    private bool AllAlignmentAnchorsInitialized()
    {
        foreach (Guid guid in this.alignmentAnchorUuids) if (guid == Guid.Empty) return false;
        return true;
    }

    // check, if all alignment anchors have been loaded
    private bool AllAlignmentAnchorsLoaded()
    {
        foreach (OVRSpatialAnchor anchor in this.alignmentAnchors) if (anchor == null) return false;
        return true;
    }

    // write the parent anchor to the headset's memory
    public IEnumerator SaveParentAnchor()
    {
        // only save anchors, if they were not loaded from the player prefs
        if (this.alignmentAnchorsLoadedFromPlayerPrefs) yield break;

        // wait until the anchor has been initialized
        while (this.parentAnchorUuid == Guid.Empty) yield return new WaitForEndOfFrame();

        // choose local storage (on the headset) only, not the cloud
        SaveOptions saveOptions = new SaveOptions();
        saveOptions.Storage = OVRSpace.StorageLocation.Local;

        // save the anchor
        this.parentAnchor.SaveAsync(saveOptions);  // TODO MAYBE: check for success here, only write uuids to the player prefs, if saving was successful

        this.WriteParentAnchorUuidToPlayerPrefs();
    }

    // write the uuids of the saved anchors to the player prefs, so the anchors can be re-used next time
    private void WriteAlignmentAnchorUuidsToPlayerPrefs()
    {
        for (int i = 0; i < this.alignmentAnchorUuids.Length; i++)
        {
            // delete previous anchor, if there was one
            String uuid = PlayerPrefs.GetString(this.playerPrefsAlignmentAnchorKeys[i], null);
            if (uuid != null) this.DeleteAnchor(uuid);

            // save uuid of the new anchor
            PlayerPrefs.SetString(this.playerPrefsAlignmentAnchorKeys[i], this.alignmentAnchorUuids[i].ToString());
        }
    }

    // write the uuid of the saved anchor to the player prefs, so the anchor can be re-used next time
    private void WriteParentAnchorUuidToPlayerPrefs()
    {
        // delete previous anchor, if there was one
        String uuid = PlayerPrefs.GetString(this.playerPrefsParentAnchorKey, null);
        if (uuid != null) this.DeleteAnchor(uuid);

        // save uuid of the new anchor
        PlayerPrefs.SetString(this.playerPrefsParentAnchorKey, this.parentAnchorUuid.ToString());
    }

    // load the alignment anchors, based on the uuids that were written to the player prefs
    public void LoadAlignmentAnchorsFromPlayerPrefs()
    {
        for (int i = 0; i < this.alignmentAnchorUuids.Length; i++)
        {
            // read uuid from the player prefs
            Guid uuid;
            Guid.TryParse(PlayerPrefs.GetString(this.playerPrefsAlignmentAnchorKeys[i]), out uuid);
            this.alignmentAnchorUuids[i] = uuid;            
        }

        // choose local storage and look only for the uuids we read from the player prefs
        LoadOptions loadOptions = new LoadOptions();
        loadOptions.StorageLocation = OVRSpace.StorageLocation.Local;
        loadOptions.Uuids = this.alignmentAnchorUuids;

        // start coroutine to load all saved anchors
        StartCoroutine(this.HandleUnboundAlignmentAnchors(OVRSpatialAnchor.LoadUnboundAnchorsAsync(loadOptions)));
    }

    // load the parent anchor, based on the uuid that was written to the player prefs
    public void LoadParentAnchorFromPlayerPrefs()
    {
        // read uuid from the player prefs
        Guid uuid;
        Guid.TryParse(PlayerPrefs.GetString(this.playerPrefsParentAnchorKey), out uuid);
        this.parentAnchorUuid = uuid;

        // choose local storage and look only for the uuids we read from the player prefs
        LoadOptions loadOptions = new LoadOptions();
        loadOptions.StorageLocation = OVRSpace.StorageLocation.Local;
        loadOptions.Uuids = new List<Guid>() { this.parentAnchorUuid };

        // start coroutine to load all saved anchors
        StartCoroutine(this.HandleUnboundParentAnchor(OVRSpatialAnchor.LoadUnboundAnchorsAsync(loadOptions)));
    }

    // localize and bind the unbound anchors from the load task
    private IEnumerator HandleUnboundAlignmentAnchors(OVRTask<UnboundAnchor[]> loadTask)
    {
        // wait until the task is complete
        while (!loadTask.IsCompleted) yield return new WaitForEndOfFrame();

        UnboundAnchor[] unboundAnchors = loadTask.GetResult();
        foreach (UnboundAnchor unboundAnchor in unboundAnchors)
        {
            // start coroutine to bind the anchor
            StartCoroutine(this.HandleLocalizedAlignmentAnchors(unboundAnchor.LocalizeAsync(), unboundAnchor));
        }
    }

    // localize and bind the unbound anchor from the load task
    private IEnumerator HandleUnboundParentAnchor(OVRTask<UnboundAnchor[]> loadTask)
    {
        // wait until the task is complete
        while (!loadTask.IsCompleted) yield return new WaitForEndOfFrame();

        UnboundAnchor[] unboundAnchors = loadTask.GetResult();
        // start coroutine to bind the anchor
        StartCoroutine(this.HandleLocalizedParentAnchor(unboundAnchors[0].LocalizeAsync(), unboundAnchors[0]));
    }

    // bind the localized anchors from the localization task
    private IEnumerator HandleLocalizedAlignmentAnchors(OVRTask<bool> localizationTask, UnboundAnchor unboundAnchor)
    {
        // wait until the task is complete
        while (!localizationTask.IsCompleted) yield return new WaitForEndOfFrame();

        bool localized = localizationTask.GetResult();
        if (localized)
        {
            // bind the anchor to a new game object
            OVRSpatialAnchor anchor = Instantiate(this.alignmentAnchorPrefab, unboundAnchor.Pose.position, unboundAnchor.Pose.rotation, this.transform).GetComponent<OVRSpatialAnchor>();
            unboundAnchor.BindTo(anchor);

            // store the new anchor
            this.alignmentAnchors[Array.IndexOf(this.alignmentAnchorUuids, anchor.Uuid)] = anchor;
            this.OnAlignmentAnchorBound();
        }
        else Debug.LogError("Failed to localize spatial anchor with uuid " + unboundAnchor.Uuid);
    }

    // bind the localized anchor from the localization task
    private IEnumerator HandleLocalizedParentAnchor(OVRTask<bool> localizationTask, UnboundAnchor unboundAnchor)
    {
        // wait until the task is complete
        while (!localizationTask.IsCompleted) yield return new WaitForEndOfFrame();

        bool localized = localizationTask.GetResult();
        if (localized)
        {
            // bind the anchor to a new game object
            OVRSpatialAnchor anchor = Instantiate(this.alignmentAnchorPrefab, unboundAnchor.Pose.position, unboundAnchor.Pose.rotation, this.transform).GetComponent<OVRSpatialAnchor>();
            unboundAnchor.BindTo(anchor);

            // store the new anchor
            this.parentAnchor = anchor;
            this.OnParentAnchorBound();
        }
        else Debug.LogError("Failed to localize parent anchor with uuid " + unboundAnchor.Uuid);
    }

    // code that should be executed whenever an alignment anchor has been bound
    private void OnAlignmentAnchorBound()
    {
        // only execute, when all anchors have been loaded
        if (!this.AllAlignmentAnchorsLoaded()) return;

        // set alignment positions based on the anchors' positions
        for (int i = 0; i < this.alignmentAnchors.Length; i++) this.alignment.NextStep(this.alignmentAnchors[i].transform.position);

        // set flag to remember, that the anchors were loaded from the player prefs
        this.alignmentAnchorsLoadedFromPlayerPrefs = true;
    }

    // code that should be executed whenever the parent anchor has been bound
    private void OnParentAnchorBound()
    {
        // set flag to remember, that the anchors were loaded from the player prefs
        this.alignmentAnchorsLoadedFromPlayerPrefs = true;

        // apply the parent anchor's position and rotation to the model parent and skip alignment 
        this.alignment.PerformParentAnchorAlignment(this.parentAnchor.transform, JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString(this.playerPrefsParentScaleKey, JsonUtility.ToJson(Vector3.one))));
    }

    // delete the spatial anchor with the given uuid from the device's storage
    private void DeleteAnchor(String uuidString)
    {
        Guid uuid;
        Guid.TryParse(uuidString, out uuid);

        // choose local storage and look only for the uuid of the anchor to be deleted
        LoadOptions loadOptions = new LoadOptions();
        loadOptions.StorageLocation = OVRSpace.StorageLocation.Local;
        loadOptions.Uuids = new Guid[] { uuid };

        // start coroutine to load and then delete the anchor
        StartCoroutine(this.DeleteUnboundAnchor(OVRSpatialAnchor.LoadUnboundAnchorsAsync(loadOptions)));
    }

    // bind and delete the unbound anchor
    private IEnumerator DeleteUnboundAnchor(OVRTask<UnboundAnchor[]> loadTask)
    {
        // wait until the task is complete
        while (!loadTask.IsCompleted) yield return new WaitForEndOfFrame();

        UnboundAnchor[] unboundAnchors = loadTask.GetResult();
        foreach (UnboundAnchor unboundAnchor in unboundAnchors)
        {
            // bind the anchor to a new game object
            OVRSpatialAnchor anchor = Instantiate(this.alignmentAnchorPrefab, unboundAnchor.Pose.position, unboundAnchor.Pose.rotation, this.transform).GetComponent<OVRSpatialAnchor>();
            unboundAnchor.BindTo(anchor);

            // erase the anchor
            StartCoroutine(this.CleanUpAfterAnchorDelete(anchor.EraseAsync(), anchor.gameObject));
        }
    }

    // clean up the dummy game object that was required for deleting an anchor
    private IEnumerator CleanUpAfterAnchorDelete(OVRTask<bool> eraseTask, GameObject dummy)
    {
        // wait until the task is complete
        while (!eraseTask.IsCompleted) yield return new WaitForEndOfFrame();

        Destroy(dummy);
    }
}
