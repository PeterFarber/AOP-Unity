using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class DistanceConstraint : ConstraintBase
{
    [HideInInspector] public GameObject point1;
    [HideInInspector] public GameObject point2;

    [Range(-1000.0f, 1000.0f)]
    public float minDistance;
    [Range(-1000.0f, 1000.0f)]

    public float maxDistance;

    public SpringSettings springSettings;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("minDistance", minDistance);
        exportConstraint.Add("maxDistance", maxDistance);
        exportConstraint.Add("springSettings", AOP.Helpers.ConvertSpringSettingsToJson(springSettings));

        return exportConstraint;
    }

    public override void Initialize()
    {
        constraintType = ConstraintType.Distance;
        CreatePoint(ref point1, "Point 1", transform);
        CreatePoint(ref point2, "Point 2", transform);
        minDistance = -1.0f;
        maxDistance = -1.0f;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.green, "Point 1");
        DrawGizmoSphere(point2, Color.red, "Point 2");
    }
}