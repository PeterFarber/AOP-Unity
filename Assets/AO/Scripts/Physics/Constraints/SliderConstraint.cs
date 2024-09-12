using UnityEngine;
using SimpleJSON;
using System;

[Serializable]
public class SliderConstraint : ConstraintBase
{
    [HideInInspector] public GameObject point1;
    [HideInInspector] public GameObject point2;

    public Vector3 sliderAxis1;
    public Vector3 normalAxis1;
    public Vector3 sliderAxis2;
    public Vector3 normalAxis2;

    [Range(float.MinValue, 0)]

    public float limitsMin;

    [Range(0, float.MaxValue)]

    public float limitsMax;

    public SpringSettings springSettings;
    public float maxFrictionForce;
    public MotorSettings motorSettings;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = base.ExportToJSON();
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("sliderAxis1", AOP.Helpers.ConvertVectorToJSONArray(sliderAxis1.normalized));
        exportConstraint.Add("normalAxis1", AOP.Helpers.ConvertVectorToJSONArray(normalAxis1.normalized));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("sliderAxis2", AOP.Helpers.ConvertVectorToJSONArray(sliderAxis2.normalized));
        exportConstraint.Add("normalAxis2", AOP.Helpers.ConvertVectorToJSONArray(normalAxis2.normalized));
        exportConstraint.Add("limitsMax", limitsMax);
        exportConstraint.Add("limitsMin", limitsMin);
        exportConstraint.Add("springSettings", AOP.Helpers.ConvertSpringSettingsToJson(springSettings));
        exportConstraint.Add("maxFrictionForce", maxFrictionForce);
        exportConstraint.Add("motorSettings", AOP.Helpers.ConvertMotorSettingsToJson(motorSettings));

        return exportConstraint;
    }

    public override void Initialize()
    {
        constraintType = ConstraintType.Slider;
        CreatePoint(ref point1, "Point 1", transform);
        CreatePoint(ref point2, "Point 2", transform);
        sliderAxis1 = new Vector3(1, 0, 0);
        normalAxis1 = new Vector3(0, 1, 0);
        sliderAxis2 = new Vector3(1, 0, 0);
        normalAxis2 = new Vector3(0, 1, 0);
        limitsMin = float.MinValue;
        limitsMax = float.MaxValue;
        maxFrictionForce = 0.0f;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.green, "Point 1");
        DrawGizmoSphere(point2, Color.red, "Point 2");
    }
    public Vector3 GetNormalizedPerpendicular(Vector3 vec)
    {
        if (Mathf.Abs(vec.x) > Mathf.Abs(vec.y))
        {
            float len = Mathf.Sqrt(vec.x * vec.x + vec.z * vec.z);
            return new Vector3(vec.z, 0.0f, -vec.x) / len;
        }
        else
        {
            float len = Mathf.Sqrt(vec.y * vec.y + vec.z * vec.z);
            return new Vector3(0.0f, vec.z, -vec.y) / len;
        }
    }

}
