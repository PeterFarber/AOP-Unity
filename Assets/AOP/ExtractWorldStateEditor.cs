using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;


[CustomEditor(typeof(ExtractWorldState))]
public class ExtractWorldStateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ExtractWorldState extractWorldState = (ExtractWorldState)target;
        if(GUILayout.Button("Extract World State"))
        {
            extractWorldState.ExportSceneToJson();
        }
    }
}
