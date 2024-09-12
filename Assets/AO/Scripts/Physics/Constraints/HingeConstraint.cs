using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class HingeConstraint : ConstraintBase
{
    [HideInInspector] public GameObject point1;
    [HideInInspector] public GameObject point2;

    public Axis hingeAxis1;
    public Axis normalAxis1;
    public Axis hingeAxis2;
    public Axis normalAxis2;

    [Range(-180.0f, 0.0f)]
    public float limitsMin;
    [Range(0.0f, 180.0f)]
    public float limitsMax;

    public SpringSettings springSettings;
    public float maxFrictionTorque;
    public MotorSettings motorSettings;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("hingeAxis1", AOP.Helpers.ConvertAxisToJsonArray(hingeAxis1));
        exportConstraint.Add("normalAxis1", AOP.Helpers.ConvertAxisToJsonArray(normalAxis1));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("hingeAxis2", AOP.Helpers.ConvertAxisToJsonArray(hingeAxis2));
        exportConstraint.Add("normalAxis2", AOP.Helpers.ConvertAxisToJsonArray(normalAxis2));
        exportConstraint.Add("limitsMin", limitsMin);
        exportConstraint.Add("limitsMax", limitsMax);
        exportConstraint.Add("springSettings", AOP.Helpers.ConvertSpringSettingsToJson(springSettings));
        exportConstraint.Add("maxFrictionTorque", maxFrictionTorque);
        exportConstraint.Add("motorSettings", AOP.Helpers.ConvertMotorSettingsToJson(motorSettings));

        return exportConstraint;
    }

    public override void Initialize()
    {
        constraintType = ConstraintType.Hinge;
        CreatePoint(ref point1, "Point 1", transform);
        CreatePoint(ref point2, "Point 2", transform);
        hingeAxis1 = Axis.Y;
        normalAxis1 = Axis.X;
        hingeAxis2 = Axis.Y;
        normalAxis2 = Axis.X;
        limitsMin = -180.0f;
        limitsMax = 180.0f;
        maxFrictionTorque = 0.0f;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.green, "Point 1");
        DrawGizmoSphere(point2, Color.red, "Point 2");
    }
}
