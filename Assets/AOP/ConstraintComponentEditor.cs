
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Reflection;



[CustomEditor(typeof(ConstraintComponent)), CanEditMultipleObjects]
public class ConstraintComponentEditor : Editor
{
    ConstraintComponent _target;
    public SerializedProperty constraintType_Prop, body1_Prop, body2_Prop, space_Prop,
    coneConstraint_Prop, distanceConstraint_Prop, fixedConstraint_Prop, gearConstraint_Prop, hingeConstraint_Prop, pointConstraint_Prop, pulleyConstraint_Prop, sliderConstraint_Prop;

    void OnEnable()
    {
        // Setup the SerializedProperties
        _target = (ConstraintComponent)target;
        space_Prop = serializedObject.FindProperty("space");
        constraintType_Prop = serializedObject.FindProperty("constraintType");
        body1_Prop = serializedObject.FindProperty("body1");
        body2_Prop = serializedObject.FindProperty("body2");
        coneConstraint_Prop = serializedObject.FindProperty("coneConstraint");
        distanceConstraint_Prop = serializedObject.FindProperty("distanceConstraint");
        fixedConstraint_Prop = serializedObject.FindProperty("fixedConstraint");
        gearConstraint_Prop = serializedObject.FindProperty("gearConstraint");
        hingeConstraint_Prop = serializedObject.FindProperty("hingeConstraint");
        pointConstraint_Prop = serializedObject.FindProperty("pointConstraint");
        pulleyConstraint_Prop = serializedObject.FindProperty("pulleyConstraint");
        sliderConstraint_Prop = serializedObject.FindProperty("sliderConstraint");

    }



    public override void OnInspectorGUI()
    {

        serializedObject.Update();


        EditorGUILayout.PropertyField(constraintType_Prop);

        ConstraintComponent.Type type = (ConstraintComponent.Type)constraintType_Prop.enumValueIndex;
        EditorGUILayout.PropertyField(space_Prop);

        EditorGUILayout.PropertyField(body1_Prop);
        EditorGUILayout.PropertyField(body2_Prop);

        switch (type)
        {

            case ConstraintComponent.Type.Cone:
                EditorGUILayout.PropertyField(coneConstraint_Prop);

                break;

            case ConstraintComponent.Type.Distance:
                EditorGUILayout.PropertyField(distanceConstraint_Prop);
                break;

            case ConstraintComponent.Type.Fixed:
                EditorGUILayout.PropertyField(fixedConstraint_Prop);
                break;

            case ConstraintComponent.Type.Gear:
                EditorGUILayout.PropertyField(gearConstraint_Prop);
                break;

            case ConstraintComponent.Type.Hinge:
                EditorGUILayout.PropertyField(hingeConstraint_Prop);
                break;

            case ConstraintComponent.Type.Point:
                EditorGUILayout.PropertyField(pointConstraint_Prop);
                break;

            case ConstraintComponent.Type.Pulley:
                EditorGUILayout.PropertyField(pulleyConstraint_Prop);
                break;

            case ConstraintComponent.Type.Slider:
                EditorGUILayout.PropertyField(sliderConstraint_Prop);
                break;
        }

        EditorGUILayout.BeginVertical("GroupBox");
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        Transform point1 = _target.transform.Find("Point1");
        Transform point2 = _target.transform.Find("Point2");
        if (point1 && point2 && point1.gameObject.activeSelf && point2.gameObject.activeSelf)
        {

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Point1 To Body1"))
            {
                point1.position = _target.body1.transform.position;
                return;
            }
            if (GUILayout.Button("Point2 To Body1"))
            {
                point2.position = _target.body1.transform.position;
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Point1 To Body2"))
            {
                point1.position = _target.body2.transform.position;
                return;
            }
            if (GUILayout.Button("Point2 To Body2"))
            {
                point2.position = _target.body2.transform.position;
                return;
            }
            EditorGUILayout.EndHorizontal();
        }

        Transform bodyPoint1 = _target.transform.Find("BodyPoint1");
        Transform bodyPoint2 = _target.transform.Find("BodyPoint2");
        if (bodyPoint1 && bodyPoint2 && bodyPoint1.gameObject.activeSelf && bodyPoint2.gameObject.activeSelf)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("BodyPoint1 To Body1"))
            {

                bodyPoint1.position = _target.body2.transform.position;

                return;
            }
            if (GUILayout.Button("BodyPoint2 To Body1"))
            {

                bodyPoint2.position = _target.body1.transform.position;
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("BodyPoint1 To Body2"))
            {

                bodyPoint1.position = _target.body2.transform.position;

                return;
            }
            if (GUILayout.Button("BodyPoint2 To Body2"))
            {

                bodyPoint2.position = _target.body2.transform.position;

                return;
            }
            EditorGUILayout.EndHorizontal();
        }
        Transform fixedPoint1 = _target.transform.Find("FixedPoint1");
        Transform fixedPoint2 = _target.transform.Find("FixedPoint2");
        if (fixedPoint1 && fixedPoint2 && fixedPoint1.gameObject.activeSelf && fixedPoint2.gameObject.activeSelf)
        {
            EditorGUILayout.BeginHorizontal();


            if (GUILayout.Button("FixedPoint1 To Body1"))
            {

                fixedPoint1.position = _target.body1.transform.position;

                return;
            }
            if (GUILayout.Button("FixedPoint2 To Body1"))
            {

                fixedPoint2.position = _target.body1.transform.position;

                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("FixedPoint1 To Body2"))
            {

                fixedPoint1.position = _target.body2.transform.position;

                return;
            }
            if (GUILayout.Button("FixedPoint2 To Body2"))
            {

                fixedPoint2.position = _target.body2.transform.position;

                return;
            }
            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.EndVertical();


        serializedObject.ApplyModifiedProperties();
    }

    private static object GetFieldValue(string fieldName, object obj, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, bindings);
        if (field != null)
        {
            return field.GetValue(obj);
        }
        return default(object);
    }
    /// <summary>
    /// Gets the object the property represents.
    /// </summary>
    /// <param name="prop"></param>
    /// <returns></returns>
    public static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');
        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("["));
                var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue_Imp(obj, elementName, index);
            }
            else
            {
                obj = GetValue_Imp(obj, element);
            }
        }
        return obj;
    }

    private static object GetValue_Imp(object source, string name)
    {
        if (source == null)
            return null;
        var type = source.GetType();

        while (type != null)
        {
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
                return f.GetValue(source);

            var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p != null)
                return p.GetValue(source, null);

            type = type.BaseType;
        }
        return null;
    }

    private static object GetValue_Imp(object source, string name, int index)
    {
        var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
        if (enumerable == null) return null;
        var enm = enumerable.GetEnumerator();
        //while (index-- >= 0)
        //    enm.MoveNext();
        //return enm.Current;

        for (int i = 0; i <= index; i++)
        {
            if (!enm.MoveNext()) return null;
        }
        return enm.Current;
    }
}