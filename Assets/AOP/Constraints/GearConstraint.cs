using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class GearConstraint : ConstraintBase
{
    [HideInInspector] public GameObject point1;
    [HideInInspector] public GameObject point2;

    public float numTeeth1;
    public Axis hingeAxis1;
    public float numTeeth2;
    public Axis hingeAxis2;
    public float ratio;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("numTeeth1", numTeeth1);
        exportConstraint.Add("hingeAxis1", AOP.Helpers.ConvertAxisToJsonArray(hingeAxis1));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("numTeeth2", numTeeth2);
        exportConstraint.Add("hingeAxis2", AOP.Helpers.ConvertAxisToJsonArray(hingeAxis2));
        exportConstraint.Add("ratio", ratio);

        return exportConstraint;
    }

    public override void Initialize()
    {
        constraintType = ConstraintType.Gear;
        CreatePoint(ref point1, "Point 1", transform);
        CreatePoint(ref point2, "Point 2", transform);
        hingeAxis1 = Axis.X;
        hingeAxis2 = Axis.X;
        numTeeth1 = 1;
        numTeeth2 = 1;
        ratio = 1.0f;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.yellow, "Point 1");
        DrawGizmoSphere(point2, Color.blue, "Point 2");
    }
}
