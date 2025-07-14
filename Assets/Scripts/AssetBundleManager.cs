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
using System.IO;
using System;

public class AssetBundleManager : MonoBehaviour
{
    [Header("Bundle and asset names")]
    [SerializeField] private string highResBundle;
    [SerializeField] private string highResAsset;
    [SerializeField] private string highGutterBundle;
    [SerializeField] private string highGutterAsset;
    [SerializeField] private string styleTransferBundle;
    [SerializeField] private string styleTransferAsset;

    // start a coroutine to load the high res model from its bundle
    public void LoadHighResModel(Action<GameObject> callbackSuccess, Action callbackFailure)
    {
        StartCoroutine(this.OnBundleLoaded(AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, this.highResBundle)), this.highResAsset, callbackSuccess, callbackFailure));
    }

    // start a coroutine to load the high gutter model from its bundle
    public void LoadHighGutterModel(Action<GameObject> callbackSuccess, Action callbackFailure)
    {
        StartCoroutine(this.OnBundleLoaded(AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, this.highGutterBundle)), this.highGutterAsset, callbackSuccess, callbackFailure));
    }

    // start a coroutine to load the style transfer model from its bundle
    public void LoadStyleTransferModel(Action<GameObject> callbackSuccess, Action callbackFailure)
    {
        StartCoroutine(this.OnBundleLoaded(AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, this.styleTransferBundle)), this.styleTransferAsset, callbackSuccess, callbackFailure));
    }

    // load the model asset with the given name out of its bundle, if the bundle was found
    private IEnumerator OnBundleLoaded(AssetBundleCreateRequest request, string assetName, Action<GameObject> callbackSuccess, Action callbackFailure)
    {
        while (!request.isDone) yield return new WaitForEndOfFrame();

        AssetBundle bundle = request.assetBundle;

        if (bundle == null)
        {
            Debug.LogError($"Failed to load asset bundle for asset '{assetName}'!");
            callbackFailure();
            yield break;
        }

        StartCoroutine(this.OnAssetLoaded(bundle.LoadAssetAsync<GameObject>(assetName), bundle, callbackSuccess));
    }

    // hand over the model's prefab to the callback function
    private IEnumerator OnAssetLoaded(AssetBundleRequest request, AssetBundle bundle, Action<GameObject> callback)
    {
        while (!request.isDone) yield return new WaitForEndOfFrame();

        GameObject prefab = (GameObject)request.asset;
        callback(prefab);

        bundle.UnloadAsync(false);
    }
}
