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

public class Alignment : MonoBehaviour
{
    [System.Serializable]
    private struct AlignmentCollection
    {
        // parent to be rotated, scaled and translated
        [SerializeField] private Transform alignmentParent;

        // Create three gameObjects that will be used as reference points, and place them in the scene, at a position that is easy to locate in the physical environment (eg. corners of a table)
        [SerializeField] private GameObject referencePointA;
        [SerializeField] private GameObject referencePointB;
        [SerializeField] private GameObject referencePointC;

        // Positions of the reference points
        private Vector3 virtualPositionA;
        private Vector3 virtualPositionB;
        private Vector3 virtualPositionC;

        // align this collection based on the given real positions
        public void PerformAlignment(Vector3[] realPositions)
        {
            if (realPositions.Length == 2) this.TwoPointAlignment(realPositions[0], realPositions[1]);
            else this.ThreePointAlignment(realPositions[0], realPositions[1], realPositions[2]);

            this.HideReferencePoints();            
        }

        // hide the reference points
        public void HideReferencePoints()
        {
            this.referencePointA.GetComponent<MeshRenderer>().enabled = false;
            this.referencePointB.GetComponent<MeshRenderer>().enabled = false;
            this.referencePointC.GetComponent<MeshRenderer>().enabled = false;
        }

        private void TwoPointAlignment(Vector3 realPosA, Vector3 realPosB)
        {
            // A little bit of geometry to rotate, translate and rescale the parent of the objects, to make the reference points match. 
            // Note that the rotation should always be done before the translation and rescaling.

            // get virtual positions
            this.virtualPositionA = this.referencePointA.transform.position;
            this.virtualPositionB = this.referencePointB.transform.position;

            // Rotate
            Quaternion rotationOffset = Quaternion.FromToRotation(this.virtualPositionB - this.virtualPositionA, realPosB - realPosA);
            this.alignmentParent.rotation = rotationOffset * this.alignmentParent.rotation;
            this.virtualPositionA = this.referencePointA.transform.position;
            this.virtualPositionB = this.referencePointB.transform.position;

            // Rescale 
            float scaleFactor = (realPosB - realPosA).magnitude / (this.virtualPositionB - this.virtualPositionA).magnitude;
            this.alignmentParent.localScale = this.alignmentParent.localScale * scaleFactor;
            this.virtualPositionA = this.referencePointA.transform.position;
            this.virtualPositionB = this.referencePointB.transform.position;

            // Translate 
            this.alignmentParent.position = this.alignmentParent.position + (realPosA - this.virtualPositionA);
        }

        private void ThreePointAlignment(Vector3 realPosA, Vector3 realPosB, Vector3 realPosC)
        {
            // get virtual positions
            this.virtualPositionA = this.referencePointA.transform.position;
            this.virtualPositionB = this.referencePointB.transform.position;
            this.virtualPositionC = this.referencePointC.transform.position;

            // create two planes
            Plane virtualPlane = new Plane(this.virtualPositionA, this.virtualPositionB, this.virtualPositionC);
            Plane realPlane = new Plane(realPosA, realPosB, realPosC);

            // Rotate
            Quaternion rotationOffset = Quaternion.FromToRotation(virtualPlane.normal, realPlane.normal);
            this.alignmentParent.rotation = rotationOffset * this.alignmentParent.rotation;

            // Rescale
            float scaleFactor = (realPosB - realPosA).magnitude / (this.virtualPositionB - this.virtualPositionA).magnitude;
            this.alignmentParent.localScale = this.alignmentParent.localScale * scaleFactor;

            // Translate
            this.alignmentParent.position += realPosA - this.referencePointA.transform.position;
        }
    }

    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Transform pointer;
    [SerializeField] private Transform[] positionVisualizers;

    [Header("Alignment collections")]
    [SerializeField] private AlignmentCollection[] alignmentCollections;

    [Header("Settings")]
    [SerializeField] private bool createSpatialAnchor;
    [SerializeField] private bool showModelAfterAlignment;
    [SerializeField] private bool threePointAlignment;
    [SerializeField] private bool loadFromParentAnchor;

    [Header("Fine tuning")]
    [SerializeField] private Transform fineTuningParent;
    [SerializeField] private Transform fineTuningReferencePointA;
    [SerializeField] private Transform fineTuningReferencePointB;
    [SerializeField] private Transform fineTuningReferencePointC;

    // Positions where the reference points should be
    private Vector3[] realPositions;

    // References
    private SpatialAnchorManager spatialAnchorManager;
    private AbstractModelManager modelManager;

    // State
    private int alignmentPositionsCollected = 0;
    private bool alignmentDone = false;
    private bool fineTuningDone = false;
    private bool newPositions = false;
    private int alignmentAnchorsLeftToCreate;

    // Alignment data for restoring before a new iteration
    private Vector3 alignedParentPosition;
    private Quaternion alignedParentRotation;


    private void Start()
    {
        // load references
        this.spatialAnchorManager = this.GetComponent<SpatialAnchorManager>();
        this.modelManager = this.GetComponent<AbstractModelManager>();

        // initialize array
        this.realPositions = new Vector3[this.threePointAlignment ? 3 : 2];

        // adjust spatial anchor manager to the three-point alignment, if enabled
        if (this.threePointAlignment) this.spatialAnchorManager.InitForThreePointAlignment();
    }

    // get number of alignment positions collected so far
    public int GetAlignmentPositionsCollected()
    {
        return this.alignmentPositionsCollected;
    }

    // get number of alignment positions to be collected
    public int GetAlignmentPositionsToBeCollected()
    {
        return this.realPositions.Length;
    }

    // check if the alignment is already done
    public bool GetAlignmentDone()
    {
        return this.alignmentDone;
    }

    // check if the fine tuning is already done
    public bool GetFineTuningDone() 
    { 
        return this.fineTuningDone; 
    }

    // continue the alignment process, i.e. set a new position of perform the final alignment
    public void NextStep(Vector3? pointerOverride = null)
    {
        // choose between the actual pointer position and a potential override (e.g. when loading alignment from spatial anchors)
        Vector3 pointerPosition = pointerOverride.HasValue ? pointerOverride.Value : this.pointer.position;

        // set new alignment positions
        if (this.alignmentPositionsCollected < this.realPositions.Length)
        {
            // record new alignment position
            this.realPositions[this.alignmentPositionsCollected] = pointerPosition;
            Debug.Log("Alignment position set: " + this.alignmentPositionsCollected);

            // set flag for new positions, if the position was not set with a pointer override
            if (!pointerOverride.HasValue) this.newPositions = true;

            // place a position visualizer
            this.positionVisualizers[this.alignmentPositionsCollected].position = pointerPosition;
            this.positionVisualizers[this.alignmentPositionsCollected].gameObject.SetActive(true);

            this.alignmentPositionsCollected++;
        }
        // perform the alignment
        else if (!this.alignmentDone)
        {
            // execute the alignment
            this.PerformAlignment();
            this.alignmentDone = true;
            this.pointer.gameObject.SetActive(false);
            this.modelManager.ShowWithFineTuningMaterial();
        }
        // end the fine tuning
        else
        {
            // restore the actual model texture and hide the model
            this.fineTuningDone = true;
            this.modelManager.ApplyOpaqueMaterial();

            // show or hide mesh according to the setting
            if (this.showModelAfterAlignment != this.modelManager.IsModelVisible()) this.modelManager.ToggleModelVisibility();

            // only save new spatial anchors, if the alignment positions changed or were newly defined
            if (this.newPositions)
            {
                // initialize counter
                this.alignmentAnchorsLeftToCreate = this.realPositions.Length;

                // create spatial anchors for the reference points after fine tuning
                this.spatialAnchorManager.CreateAlignmentAnchor(0, this.fineTuningReferencePointA.position, this.OnAlignmentAnchorCreated);
                this.spatialAnchorManager.CreateAlignmentAnchor(1, this.fineTuningReferencePointB.position, this.OnAlignmentAnchorCreated);
                if (this.threePointAlignment) this.spatialAnchorManager.CreateAlignmentAnchor(2, this.fineTuningReferencePointC.position, this.OnAlignmentAnchorCreated);
            }

            // create spatial anchor to prevent the world from shifting when the headset is put down and picked back up later
            if (this.createSpatialAnchor) Invoke("CreateSpatialAnchor", 1);

            // store data of the alignment/fine-tuning for restoring between iterations
            this.alignedParentPosition = this.fineTuningParent.position;
            this.alignedParentRotation = this.fineTuningParent.rotation;

            // always create a new parent anchor, in case there was none before
            this.spatialAnchorManager.CreateParentAnchor(this.alignedParentPosition, this.alignedParentRotation, this.fineTuningParent.localScale, this.OnParentAnchorCreated);
        }
    }

    // called when a spatial anchor was created successfully, saves the anchors once all were created
    public void OnAlignmentAnchorCreated()
    {
        // decrease counter
        this.alignmentAnchorsLeftToCreate--;

        // save the alignment anchors
        if (this.alignmentAnchorsLeftToCreate == 0) StartCoroutine(this.spatialAnchorManager.SaveAlignmentAnchors());
    }

    // called when the parent anchor was created successfully, saves the anchor
    public void OnParentAnchorCreated()
    {
        StartCoroutine(this.spatialAnchorManager.SaveParentAnchor());
    }

    // execute the actual alignment
    private void PerformAlignment()
    {
        // perform the alignment in each of the alignment collections
        foreach (AlignmentCollection ac in this.alignmentCollections) ac.PerformAlignment(this.realPositions);

        // hide the position visualizers
        foreach (Transform visualizer in this.positionVisualizers) visualizer.gameObject.SetActive(false);

        // activate the button for locking the current model
        if (this.uiManager != null) this.uiManager.UpdateModelLockButtons(false);
    }

    // align the model based on the single parent anchor
    public void PerformParentAnchorAlignment(Transform parentAnchor, Vector3 scale)
    {
        // apply position, rotation and scale
        this.fineTuningParent.position = parentAnchor.position;
        this.fineTuningParent.rotation = parentAnchor.rotation;
        this.fineTuningParent.localScale = scale;

        // set values to skip the default alignment based on alignment points
        this.alignmentPositionsCollected = this.realPositions.Length;
        this.alignmentDone = true;

        // prepare for the fine-tuning
        this.pointer.gameObject.SetActive(false);
        this.modelManager.ShowWithFineTuningMaterial();

        // hide position visualizers and reference points
        foreach (Transform visualizer in this.positionVisualizers) visualizer.gameObject.SetActive(false);
        foreach (AlignmentCollection ac in this.alignmentCollections) ac.HideReferencePoints();
    }

    // fine tune the alignment
    public void FineTuneTranslation(Vector2 leftJoystick, Vector2 rightJoystick)
    {
        // set flag to make sure the new positions after fine tuning are saved with new spatial anchors
        this.newPositions = true;

        // map joystick inputs to a translation vector
        float x = leftJoystick.x;
        float y = rightJoystick.y;
        float z = leftJoystick.y;

        // apply the translation
        this.fineTuningParent.Translate(x, y, z, Space.World);
    }

    // fine tune the alignment
    public void FineTuneRotation(Vector2 leftJoystick, Vector2 rightJoystick)
    {
        // set flag to make sure the new positions after fine tuning are saved with new spatial anchors
        this.newPositions = true;

        // map joystick inputs to a rotation vector
        float x = leftJoystick.y;
        float y = rightJoystick.x;
        float z = leftJoystick.x;

        // apply the rotation (only rotates around a single axis by choosing the one with the highest absolute input)
        if (Mathf.Abs(x) >= Mathf.Abs(y) && Mathf.Abs(x) >= Mathf.Abs(z)) this.fineTuningParent.Rotate(x, 0, 0, Space.Self);
        else if (Mathf.Abs(y) >= Mathf.Abs(z)) this.fineTuningParent.Rotate(0, y, 0, Space.Self);
        else this.fineTuningParent.Rotate(0, 0, z, Space.Self);
    }

    // reset the up-axis of the model to the global up-axis
    public void FineTuneResetUpAxis()
    {
        Quaternion rotationOffset = Quaternion.FromToRotation(this.fineTuningParent.up, Vector3.up);
        this.fineTuningParent.rotation = rotationOffset * this.fineTuningParent.rotation;
    }

    // create a spatial anchor attached to this game object
    private void CreateSpatialAnchor()
    {
        this.gameObject.AddComponent<OVRSpatialAnchor>();
    }

    // load the spatial anchors from the player prefs and apply their positions as alignment positions
    public void LoadPositionsFromSpatialAnchors()
    {
        if (this.loadFromParentAnchor) this.spatialAnchorManager.LoadParentAnchorFromPlayerPrefs();
        else this.spatialAnchorManager.LoadAlignmentAnchorsFromPlayerPrefs();
    }

    // change setting of whether to show the model after alignment
    public void SetShowModelAfterAlignment(bool value)
    {
        this.showModelAfterAlignment = value;
    }

    // prepare alignment for the next iteration
    public void StartNewIteration()
    {
        // restore the aligned position and rotation (before RDW was applied)
        this.fineTuningParent.position = this.alignedParentPosition;
        this.fineTuningParent.rotation = this.alignedParentRotation;

        // allow fine tuning again to check the alignment between iterations
        this.fineTuningDone = false;
        // TODO: maybe introduce additional flag to prevent anchor saving after the first fine tuning
    }
}
