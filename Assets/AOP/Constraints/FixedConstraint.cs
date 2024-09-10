using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class FixedConstraint : ConstraintBase
{
    [HideInInspector] public GameObject point1;
    [HideInInspector] public GameObject point2;

    public bool autoDetectPoint;
    public Axis axisX1;
    public Axis axisY1;
    public Axis axisX2;
    public Axis axisY2;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("axisX1", AOP.Helpers.ConvertAxisToJsonArray(axisX1));
        exportConstraint.Add("axisY1", AOP.Helpers.ConvertAxisToJsonArray(axisY1));
        exportConstraint.Add("axisX2", AOP.Helpers.ConvertAxisToJsonArray(axisX2));
        exportConstraint.Add("axisY2", AOP.Helpers.ConvertAxisToJsonArray(axisY2));
        exportConstraint.Add("autoDetectPoint", autoDetectPoint);

        return exportConstraint;
    }

    public override void Initialize()
    {
        constraintType = ConstraintType.Fixed;
        CreatePoint(ref point1, "Point 1", transform);
        CreatePoint(ref point2, "Point 2", transform);
        autoDetectPoint = false;
        axisX1 = Axis.X;
        axisY1 = Axis.Y;
        axisX2 = Axis.X;
        axisY2 = Axis.Y;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.cyan, "Point 1");
        DrawGizmoSphere(point2, Color.magenta, "Point 2");
    }
}
