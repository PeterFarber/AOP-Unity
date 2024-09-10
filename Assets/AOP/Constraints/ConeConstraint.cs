using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class ConeConstraint : ConstraintBase
{
    [HideInInspector] public GameObject point1;
    public Axis twistAxis1;
    [HideInInspector] public GameObject point2;
    public Axis twistAxis2;

    [Range(0, 180.0f)]
    public float halfConeAngle;


    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();

        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("twistAxis1", AOP.Helpers.ConvertAxisToJsonArray(twistAxis1));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("twistAxis2", AOP.Helpers.ConvertAxisToJsonArray(twistAxis2));
        exportConstraint.Add("halfConeAngle", halfConeAngle);

        return exportConstraint;
    }

    public override void Initialize() 
    {
        constraintType = ConstraintType.Cone;
        CreatePoint(ref point1, "Point 1", transform);
        CreatePoint(ref point2, "Point 2", transform);
        twistAxis1 = Axis.Y;
        twistAxis2 = Axis.Y;
        halfConeAngle = 0.0f;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.yellow, "Point 1");
        DrawGizmoSphere(point2, Color.blue, "Point 2");
    }
}
