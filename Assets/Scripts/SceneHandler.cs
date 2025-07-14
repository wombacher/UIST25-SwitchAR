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
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{
    [SerializeField] private string incognitoTaskScene;
    [SerializeField] private string withRevealTaskScene;
    [SerializeField] private string afterRevealTaskScene;

    // load the incognito task, where the participant is redirected, but restored to his original position in the end -> no reveal
    public void LoadIncognitoTask()
    {
        SceneManager.LoadScene(this.incognitoTaskScene);
    }

    // load the task with reveal, where the participant is redirected with a reveal in the end ("teleportation in AR")
    public void LoadWithRevealTask()
    {
        SceneManager.LoadScene(this.withRevealTaskScene);
    }

    // load the task after reveal, where the participant already knows of the room model
    public void LoadAfterRevealTask()
    {
        SceneManager.LoadScene(this.afterRevealTaskScene);    }
}
