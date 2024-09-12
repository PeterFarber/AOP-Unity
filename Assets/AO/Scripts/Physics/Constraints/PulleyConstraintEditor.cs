using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PulleyConstraint))]
[CanEditMultipleObjects]
public class PulleyConstraintEditor : Editor
{
    private PulleyConstraint constraint;

    void OnEnable()
    {
        constraint = (PulleyConstraint)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayoutOption[] options = { GUILayout.Height(32) };

        if (constraint.body1 == null || constraint.body2 == null)
        {
            EditorGUILayout.HelpBox("Please assign Body1 and Body2", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("GroupBox");
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("BodyPoint1 To Body1", options))
        {
            constraint.bodyPoint1.transform.position = constraint.body1.transform.position;
            return;
        }

        if (GUILayout.Button("BodyPoint1 To Body2", options))
        {
            constraint.bodyPoint1.transform.position = constraint.body2.transform.position;
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("BodyPoint2 To Body1", options))
        {
            constraint.bodyPoint2.transform.position = constraint.body1.transform.position;
            return;
        }
        if (GUILayout.Button("BodyPoint2 To Body2", options))
        {
            constraint.bodyPoint2.transform.position = constraint.body2.transform.position;
            return;
        }
        EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("FixedPoint1 To Body1", options))
        {
            constraint.fixedPoint1.transform.position = constraint.body1.transform.position;
            return;
        }
        
        if (GUILayout.Button("FixedPoint1 To Body2", options))
        {
            constraint.fixedPoint1.transform.position = constraint.body2.transform.position;
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("FixedPoint2 To Body1", options))
        {
            constraint.fixedPoint2.transform.position = constraint.body1.transform.position;
            return;
        }
        if (GUILayout.Button("FixedPoint2 To Body2", options))
        {
            constraint.fixedPoint2.transform.position = constraint.body2.transform.position;
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reset", options))
        {
            constraint.Reset();
            return;
        }

        EditorGUILayout.EndVertical();
    }
}
