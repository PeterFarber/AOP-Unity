using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class PulleyConstraint : ConstraintBase
{
    [HideInInspector] public GameObject bodyPoint1;
    [HideInInspector] public GameObject fixedPoint1;
    [HideInInspector] public GameObject bodyPoint2;
    [HideInInspector] public GameObject fixedPoint2;

    [Range(0.0f, 10.0f)]
    public float ratio;
    public float minLength;
    public float maxLength;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();
        exportConstraint.Add("bodyPoint1", AOP.Helpers.ConvertVectorToJSONArray(bodyPoint1.transform.position));
        exportConstraint.Add("fixedPoint1", AOP.Helpers.ConvertVectorToJSONArray(fixedPoint1.transform.position));
        exportConstraint.Add("bodyPoint2", AOP.Helpers.ConvertVectorToJSONArray(bodyPoint2.transform.position));
        exportConstraint.Add("fixedPoint2", AOP.Helpers.ConvertVectorToJSONArray(fixedPoint2.transform.position));
        exportConstraint.Add("ratio", ratio);
        exportConstraint.Add("minLength", minLength);
        exportConstraint.Add("maxLength", maxLength);

        return exportConstraint;
    }

    public override void Initialize()
    {
        constraintType = ConstraintType.Pulley;
        CreatePoint(ref bodyPoint1, "Body Point 1", transform);
        CreatePoint(ref fixedPoint1, "Fixed Point 1", transform);
        CreatePoint(ref bodyPoint2, "Body Point 2", transform);
        CreatePoint(ref fixedPoint2, "Fixed Point 2", transform);
        ratio = 1.0f;
        minLength = 0.0f;
        maxLength = -1.0f;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(bodyPoint1, Color.yellow, "Body Point 1");
        DrawGizmoSphere(bodyPoint2, Color.blue, "Body Point 2");
        DrawGizmoSphere(fixedPoint1, Color.cyan, "Fixed Point 1");
        DrawGizmoSphere(fixedPoint2, Color.magenta, "Fixed Point 2");
    }
}
