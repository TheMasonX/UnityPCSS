using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PoissonTools))]
public class PoissonToolsEditor : Editor
{
    public static bool autoIntelliSort = true;

    public override void OnInspectorGUI ()
    {
        var script = target as PoissonTools;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if(EditorGUI.EndChangeCheck())
        {
            if (script.count > script.data.Length)
                script.count = script.data.Length;

            if (autoIntelliSort)
                script.IntelliSort();

            Redraw();
        }

        GUILayout.Space(30f);

        float spaceBetweenButtons = 5f;


        if (GUILayout.Button("IntelliSort"))
        {
            Undo.RecordObject(script, "Poisson Tools - IntelliSort");
            script.IntelliSort();
            script.Redraw();
        }

        GUILayout.Space(spaceBetweenButtons);

        if (GUILayout.Button("Shuffle"))
        {
            Undo.RecordObject(script, "Poisson Tools - Shuffled Data");
            script.ShuffleData();
            script.Redraw();
        }

        GUILayout.Space(spaceBetweenButtons);

        if (GUILayout.Button("Interleave"))
        {
            Undo.RecordObject(script, "Poisson Tools - Interleaved Data");
            script.Interleave();
            script.Redraw();
        }

        GUILayout.Space(25f);

        if (GUILayout.Button("Save As Backup"))
        {
            Undo.RecordObject(script, "Poisson Tools - Saved New Backup");
            script.Backup(true);
            script.Redraw();
        }

        GUILayout.Space(spaceBetweenButtons);

        if (GUILayout.Button("Revert to Backup"))
        {
            Undo.RecordObject(script, "Poisson Tools - Reverted To Backup");
            script.Revert();
            script.Redraw();
        }

        GUILayout.Space(25f);

        if (GUILayout.Button("Force Redraw"))
        {
            script.Redraw();
        }

        GUILayout.Space(spaceBetweenButtons);

        if (GUILayout.Button("Print Data"))
        {
            script.PrintArray();
        }

        GUILayout.Space(10f);
        autoIntelliSort = EditorGUILayout.Toggle("Update IntelliSort", autoIntelliSort);
    }

    public void OnSceneGUI ()
    {
        var script = target as PoissonTools;

        for (int i = 0; i < script.data.Length; i++)
        {
            var v2 = script.data[i];
            var v3 = new Vector3(v2.x * 10f, 0, v2.y * 10f) + script.transform.position;

            Handles.color = (i < script.count) ? Color.red : Color.white;
            v3 = Handles.FreeMoveHandle(v3, Quaternion.identity, .2f, Vector3.zero, Handles.DotHandleCap) - script.transform.position;
            v2.x = Mathf.Clamp(v3.x / 10f, -.999f, .999f);
            v2.y = Mathf.Clamp(v3.z / 10f, -.999f, .999f);
            script.data[i] = v2;
        }
    }

    public void OnEnable ()
    {
        Undo.undoRedoPerformed += Redraw;
    }

    public void OnDisable ()
    {
        Undo.undoRedoPerformed -= Redraw;
    }

    public void Redraw ()
    {
        var script = target as PoissonTools;
        script.Redraw();
    }
}
