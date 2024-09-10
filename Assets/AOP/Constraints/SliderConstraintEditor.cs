using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SliderConstraint))]
[CanEditMultipleObjects]
public class SliderConstraintEditor : Editor
{
    private SliderConstraint constraint;

    void OnEnable()
    {
        constraint = (SliderConstraint)target;
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
        if (GUILayout.Button("Point1 To Body1", options))
        {
            constraint.point1.transform.position = constraint.body1.transform.position;
            return;
        }
        if (GUILayout.Button("Point1 To Body2", options))
        {
            constraint.point1.transform.position = constraint.body2.transform.position;
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Point2 To Body1", options))
        {
            constraint.point2.transform.position = constraint.body1.transform.position;
            return;
        }
        if (GUILayout.Button("Point2 To Body2", options))
        {
            constraint.point2.transform.position = constraint.body2.transform.position;
            return;
        }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Perp Norm Vec", options))
        {
            Debug.Log(constraint.GetNormalizedPerpendicular(constraint.sliderAxis1));
            return;
        }

        if (GUILayout.Button("Reset", options))
        {
            constraint.Reset();
            return;
        }

        EditorGUILayout.EndVertical();
    }
}
