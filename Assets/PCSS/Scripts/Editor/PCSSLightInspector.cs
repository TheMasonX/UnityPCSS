using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PCSSLight))]
public class PCSSLightInspector : Editor
{
    public override void OnInspectorGUI ()
    {
        PCSSLight script = target as PCSSLight;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if(EditorGUI.EndChangeCheck())
        {
            script.UpdateShaderValues();
        }
    }
}
