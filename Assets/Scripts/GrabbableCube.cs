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

using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GrabbableCube : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float respawnDeltaY;
    [SerializeField] private float respawnDelay;
    
    /// <summary>
    /// If the transform has an associated rigidbody, make it kinematic during this
    /// number of frames after a respawn, in order to avoid ghost collisions.
    /// </summary>
    [SerializeField]
    [Tooltip("If the transform has an associated rigidbody, make it kinematic during this number of frames after a respawn, in order to avoid ghost collisions.")]
    private int _sleepFrames = 0;

    /// <summary>
    /// UnityEvent triggered when a respawn occurs.
    /// </summary>
    [SerializeField]
    [Tooltip("UnityEvent triggered when a respawn occurs.")]
    private UnityEvent _whenRespawned = new UnityEvent();

    public UnityEvent WhenRespawned => _whenRespawned;

    // cached starting transform
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    private TwoGrabFreeTransformer[] freeTransformers;
    private Rigidbody rigidBody;
    private int _sleepCountDown;
    private bool initialized;
    private bool locked;
   
    public void Init()
    {
        this.initialPosition = this.transform.position;
        this.initialRotation = this.transform.rotation;
        this.initialScale = this.transform.localScale;
        this.freeTransformers = this.GetComponents<TwoGrabFreeTransformer>();
        this.rigidBody = this.GetComponent<Rigidbody>();
        this.initialized = true;
        this.locked = false;
    }

    private void Update()
    {
        if (this.locked) return;
        if (this.initialized && this.initialPosition.y - this.transform.position.y > this.respawnDeltaY) Invoke("Respawn", this.respawnDelay);
    }

    protected virtual void FixedUpdate()
    {
        if (this.locked) return;

        if (_sleepCountDown > 0)
        {
            if (--_sleepCountDown == 0)
            {
                rigidBody.isKinematic = false;
            }
        }
    }

    public void Respawn()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;

        if (rigidBody)
        {
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;

            if (!rigidBody.isKinematic && _sleepFrames > 0)
            {
                _sleepCountDown = _sleepFrames;
                rigidBody.isKinematic = true;
            }
        }

        foreach (var freeTransformer in freeTransformers)
        {
            freeTransformer.MarkAsBaseScale();
        }

        _whenRespawned.Invoke();
    }

    // prevent the cube from being moved, also disables the respawning
    public void LockInPlace()
    {
        this.locked = true;
        this.rigidBody.isKinematic = true;
        this.GetComponent<TouchHandGrabInteractable>().enabled = false;
        this.GetComponent<Grabbable>().enabled = false;
    }
}