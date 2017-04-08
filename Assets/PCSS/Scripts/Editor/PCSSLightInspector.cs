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

        //if (script.Blocker_GradientBias > 0 && QualitySettings.shadowCascades > 1)
        //{
        //    EditorGUILayout.HelpBox("'Blocker Gradient Bias' > 0 seems to cause issues when using shadow cascades. Use at your own risk!", MessageType.Warning);
        //}

        if (script.Blocker_GradientBias < Mathf.Epsilon && QualitySettings.shadowCascades == 1)
        {
            EditorGUILayout.HelpBox("A 'Blocker Gradient Bias' of 0 seems to cause issues when not using shadow cascades. Any non-zero value should fix this.", MessageType.Error);
        }
    }
}
