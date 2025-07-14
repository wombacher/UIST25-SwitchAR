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
using System.IO;
using TMPro;
using UnityEngine;

public class DataLogging : MonoBehaviour
{
    [SerializeField] private string logFileName = "userStudyLog";

    private List<Vector3> positions = new List<Vector3>();

    [System.Serializable]
    private struct IterationData
    {
        public List<Vector3> paintingPositions;
        public int iterationIndex;

        public IterationData(List<Vector3> list, int index)
        {
            this.paintingPositions = list;
            this.iterationIndex = index;
        }
    }

    public void AddPosition(Vector3 position)
    {
        this.positions.Add(position);
    }

    public void OnIterationFinished(int iterationIndex)
    {
        this.WriteDataLog(iterationIndex);
        this.positions.Clear();
    }

    public void WriteDataLog(int iterationIndex)
    {
        IterationData data = new IterationData(this.positions, iterationIndex);
        string path = this.GetNextFileName();
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);

        Debug.LogWarning("Wrote data log to the following path: " + path);
    }

    private string GetNextFileName()
    {
        FileInfo[] files = new DirectoryInfo(Application.persistentDataPath).GetFiles();
        int fileCount = 0;
        foreach (FileInfo file in files) if (file.Name.StartsWith(this.logFileName)) fileCount++;

        return Application.persistentDataPath + "/" + this.logFileName + fileCount.ToString() + ".json";
    }
}