using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class PointConstraint : ConstraintBase
{
    [HideInInspector] public GameObject point1;
    [HideInInspector] public GameObject point2;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));

        return exportConstraint;
    }

    public override void Initialize()
    {
        constraintType = ConstraintType.Point;
        CreatePoint(ref point1, "Point 1", transform);
        CreatePoint(ref point2, "Point 2", transform);
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.cyan, "Point 1");
        DrawGizmoSphere(point2, Color.magenta, "Point 2");
    }
}
