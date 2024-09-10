using UnityEngine;
using System;
using SimpleJSON;
using System.Collections.Generic;
using Unity.VisualScripting;

[Serializable]
public class SpringSettings
{
    public enum Mode { FrequencyAndDamping, StiffnessAndDamping };
    public Mode mode;
    public float stiffness;
    public float frequency;
    public float damping;
}

[Serializable]
public class MotorSettings
{
    public float frequency;
    public float damping;
}
public enum Axis { X, Y, Z }
public enum Space { LocalToBodyCOM, WorldSpace }


[ExecuteInEditMode]
public class ConstraintComponent : MonoBehaviour
{

    private GameObject point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2;


    private Type previousConstraintType;

    public enum Type { Cone, Distance, Fixed, Gear, Hinge, Point, Pulley, Slider }

    public Space space = Space.WorldSpace;

    public Type constraintType;
    public int id;
    public BodyComponent body1;
    public BodyComponent body2;

    public ConeConstraint coneConstraint;
    public DistanceConstraint distanceConstraint;
    public FixedConstraint fixedConstraint;
    public GearConstraint gearConstraint;
    public HingeConstraint hingeConstraint;
    public PointConstraint pointConstraint;
    public PulleyConstraint pulleyConstraint;
    public SliderConstraint sliderConstraint;

    private void OnDrawGizmos()
    {

        GetConstraint()?.DrawGizmos();

    }

    void Reset()
    {

        constraintType = Type.Fixed;
        // If the constraint is not initialized, initialize it
        coneConstraint ??= new ConeConstraint();
        distanceConstraint ??= new DistanceConstraint();
        fixedConstraint ??= new FixedConstraint();
        gearConstraint ??= new GearConstraint();
        hingeConstraint ??= new HingeConstraint();
        pointConstraint ??= new PointConstraint();
        pulleyConstraint ??= new PulleyConstraint();
        sliderConstraint ??= new SliderConstraint();

        // Remove all points
        Transform[] objs = gameObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in objs)
        {
            if(child != transform)
                DestroyImmediate(child.gameObject);
        }
        


        point1 = CreatePoint("Point1", transform, new Vector3(-1, 0, 0));
        point2 = CreatePoint("Point2", transform, new Vector3(1, 0, 0));
        bodyPoint1 = CreatePoint("BodyPoint1", transform, new Vector3(-1, 0, 0));
        bodyPoint2 = CreatePoint("BodyPoint2", transform, new Vector3(1, 0, 0));
        fixedPoint1 = CreatePoint("FixedPoint1", transform, new Vector3(-1, 0, 0));
        fixedPoint2 = CreatePoint("FixedPoint2", transform, new Vector3(1, 0, 0));
        GetConstraint().InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);


    }

    private void Update()
    {

        if (previousConstraintType != constraintType && GetConstraint() != null)
        {
            previousConstraintType = constraintType;
            GetConstraint().InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);

        }

    }

    private ConstraintBase GetConstraint()
    {
        switch (constraintType)
        {
            case Type.Cone:
                return coneConstraint;
            case Type.Distance:
                return distanceConstraint;
            case Type.Fixed:
                return fixedConstraint;
            case Type.Gear:
                return gearConstraint;
            case Type.Hinge:
                return hingeConstraint;
            case Type.Point:
                return pointConstraint;
            case Type.Pulley:
                return pulleyConstraint;
            case Type.Slider:
                return sliderConstraint;
            default:
                return null;
        }
    }

    private GameObject CreatePoint(string name, Transform parent, Vector3 localPosition)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.localPosition = localPosition;
        return obj;
    }
    public JSONNode ExportToJson()
    {
        JSONNode exportConstraint = new JSONObject();
        exportConstraint.Add("body1ID", body1.GetInstanceID());
        exportConstraint.Add("body2ID", body2.GetInstanceID());
        exportConstraint.Add("type", constraintType.ToString());

        // Add simple properties
        exportConstraint.Add("space", space.ToString());

        JSONNode properties = GetConstraint().ExportToJSON();

        foreach (KeyValuePair<string, JSONNode> property in properties)
        {
            exportConstraint.Add(property.Key, property.Value);
        }

        return exportConstraint;
    }
}

[Serializable]
public abstract class ConstraintBase
{


    public abstract void DrawGizmos();

    public abstract JSONNode ExportToJSON();

    public virtual void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {

        point1.SetActive(false);
        point2.SetActive(false);
        bodyPoint1.SetActive(false);
        bodyPoint2.SetActive(false);
        fixedPoint1.SetActive(false);
        fixedPoint2.SetActive(false);
    }


    protected void DrawGizmoSphere(Transform point, Color color, string label)
    {
        if (point?.gameObject.activeSelf == true)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(point.position, 0.33f);
            DrawString(label, point.position, Color.white, new Vector2(0f, 5f));
        }
    }


    protected void DrawString(string text, Vector3 worldPosition, Color textColor, Vector2 anchor, float textSize = 15f)
    {
#if UNITY_EDITOR
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        if (!view)
            return;
        Vector3 screenPosition = view.camera.WorldToScreenPoint(worldPosition);
        if (screenPosition.y < 0 || screenPosition.y > view.camera.pixelHeight || screenPosition.x < 0 || screenPosition.x > view.camera.pixelWidth || screenPosition.z < 0)
            return;
        var pixelRatio = UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.right).x - UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.zero).x;
        UnityEditor.Handles.BeginGUI();
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)textSize,
            normal = new GUIStyleState() { textColor = textColor }
        };
        Vector2 size = style.CalcSize(new GUIContent(text)) * pixelRatio;
        var alignedPosition =
            ((Vector2)screenPosition +
            size * ((anchor + Vector2.left + Vector2.up) / 2f)) * (Vector2.right + Vector2.down) +
            Vector2.up * view.camera.pixelHeight;
        GUI.Label(new Rect(alignedPosition / pixelRatio, size / pixelRatio), text, style);
        UnityEditor.Handles.EndGUI();
#endif
    }
}

// Constraint classes with separated points

[Serializable]
public class ConeConstraint : ConstraintBase
{
    public Transform point1;
    public Axis twistAxis1 = Axis.Y;
    public Transform point2;
    public Axis twistAxis2 = Axis.Y;
    public float halfConeAngle = 0.0f;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("twistAxis1", AOP.Helpers.ConvertAxisToJsonArray(twistAxis1));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("twistAxis2", AOP.Helpers.ConvertAxisToJsonArray(twistAxis2));
        exportConstraint.Add("halfConeAngle", halfConeAngle);

        return exportConstraint;
    }

    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);
        point1.SetActive(true);
        point2.SetActive(true);
        if (point1 != null)
            this.point1 = point1.transform;
        if (point2 != null)
            this.point2 = point2.transform;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.yellow, "Point 1");
        DrawGizmoSphere(point2, Color.blue, "Point 2");
    }


}

[Serializable]
public class DistanceConstraint : ConstraintBase
{
    public Transform point1;
    public Transform point2;
    public float minDistance = -1.0f;
    public float maxDistance = -1.0f;

    public SpringSettings springSettings = new SpringSettings();

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("minDistance", minDistance);
        exportConstraint.Add("maxDistance", maxDistance);
        exportConstraint.Add("springSettings", AOP.Helpers.ConvertSpringSettingsToJson(springSettings));

        return exportConstraint;
    }
    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);
        point1.SetActive(true);
        point2.SetActive(true);
        if (point1 != null)
            this.point1 = point1.transform;
        if (point2 != null)
            this.point2 = point2.transform;

    }
    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.green, "Point 1");
        DrawGizmoSphere(point2, Color.red, "Point 2");
    }
}

[Serializable]
public class FixedConstraint : ConstraintBase
{
    public bool autoDetectPoint = false;

    public Transform point1;

    public Axis axisX1 = Axis.Y;
    public Axis axisY1 = Axis.X;

    public Transform point2;

    public Axis axisX2 = Axis.Y;
    public Axis axisY2 = Axis.X;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

        exportConstraint.Add("autoDetectPoint", autoDetectPoint);
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("axisX1", AOP.Helpers.ConvertAxisToJsonArray(axisX1));
        exportConstraint.Add("axisY1", AOP.Helpers.ConvertAxisToJsonArray(axisY1));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("axisX2", AOP.Helpers.ConvertAxisToJsonArray(axisX2));
        exportConstraint.Add("axisY2", AOP.Helpers.ConvertAxisToJsonArray(axisY2));

        return exportConstraint;
    }

    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);

        point1.SetActive(true);
        point2.SetActive(true);
        if (point1 != null)
            this.point1 = point1.transform;
        if (point2 != null)
            this.point2 = point2.transform;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.cyan, "Point 1");
        DrawGizmoSphere(point2, Color.magenta, "Point 2");
    }
}

[Serializable]
public class GearConstraint : ConstraintBase
{
    public Transform point1;
    public float numTeeth1;

    public Axis hingeAxis1 = Axis.X;

    public Transform point2;
    public float numTeeth2;

    public Axis hingeAxis2 = Axis.X;

    public float ratio;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("numTeeth1", numTeeth1);
        exportConstraint.Add("hingeAxis1", AOP.Helpers.ConvertAxisToJsonArray(hingeAxis1));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("numTeeth2", numTeeth2);
        exportConstraint.Add("hingeAxis2", AOP.Helpers.ConvertAxisToJsonArray(hingeAxis2));
        exportConstraint.Add("ratio", ratio);

        return exportConstraint;
    }

    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);

        point1.SetActive(true);
        point2.SetActive(true);
        if (point1 != null)
            this.point1 = point1.transform;
        if (point2 != null)
            this.point2 = point2.transform;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.yellow, "Point 1");
        DrawGizmoSphere(point2, Color.blue, "Point 2");
    }
}

[Serializable]
public class HingeConstraint : ConstraintBase
{
    public Transform point1;
    public Axis hingeAxis1 = Axis.Y;
    public Axis normalAxis1 = Axis.X;

    public Transform point2;
    public Axis hingeAxis2 = Axis.Y;
    public Axis normalAxis2 = Axis.X;

    public float limitsMin = -180.0f;
    public float limitsMax = 180.0f;

    public SpringSettings springSettings = new SpringSettings();

    public float maxFrictionTorque = 0.0f;

    public MotorSettings motorSettings = new MotorSettings();

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

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

    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);

        point1.SetActive(true);
        point2.SetActive(true);
        if (point1 != null)
            this.point1 = point1.transform;
        if (point2 != null)
            this.point2 = point2.transform;
    }
    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.green, "Point 1");
        DrawGizmoSphere(point2, Color.red, "Point 2");
    }
}

[Serializable]
public class PointConstraint : ConstraintBase
{
    public Transform point1;
    public Transform point2;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));

        return exportConstraint;
    }

    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);

        point1.SetActive(true);
        point2.SetActive(true);
        if (point1 != null)
            this.point1 = point1.transform;
        if (point2 != null)
            this.point2 = point2.transform;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.cyan, "Point 1");
        DrawGizmoSphere(point2, Color.magenta, "Point 2");
    }
}

[Serializable]
public class PulleyConstraint : ConstraintBase
{
    public Transform bodyPoint1;
    public Transform fixedPoint1;

    public Transform bodyPoint2;
    public Transform fixedPoint2;

    public float ratio = 1.0f;

    public float minLength = 0.0f;
    public float maxLength = 0.0f;

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

        exportConstraint.Add("bodyPoint1", AOP.Helpers.ConvertVectorToJSONArray(bodyPoint1.transform.position));
        exportConstraint.Add("fixedPoint1", AOP.Helpers.ConvertVectorToJSONArray(fixedPoint1.transform.position));
        exportConstraint.Add("bodyPoint2", AOP.Helpers.ConvertVectorToJSONArray(bodyPoint2.transform.position));
        exportConstraint.Add("fixedPoint2", AOP.Helpers.ConvertVectorToJSONArray(fixedPoint2.transform.position));
        exportConstraint.Add("ratio", ratio);
        exportConstraint.Add("minLength", minLength);
        exportConstraint.Add("maxLength", maxLength);

        return exportConstraint;
    }

    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);

        bodyPoint1.SetActive(true);
        bodyPoint2.SetActive(true);
        fixedPoint1.SetActive(true);
        fixedPoint2.SetActive(true);
        if (bodyPoint1 != null)
            this.bodyPoint1 = bodyPoint1.transform;
        if (bodyPoint2 != null)
            this.bodyPoint2 = bodyPoint2.transform;
        if (fixedPoint1 != null)
            this.fixedPoint1 = fixedPoint1.transform;
        if (fixedPoint2 != null)
            this.fixedPoint2 = fixedPoint2.transform;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(bodyPoint1, Color.yellow, "Body Point 1");
        DrawGizmoSphere(bodyPoint2, Color.blue, "Body Point 2");
        DrawGizmoSphere(fixedPoint1, Color.cyan, "Fixed Point 1");
        DrawGizmoSphere(fixedPoint2, Color.magenta, "Fixed Point 2");
    }
}

[Serializable]
public class SliderConstraint : ConstraintBase
{
    public bool autoDetectPoint = false;

    public Transform point1;
    public Axis sliderAxis1 = Axis.X;
    public Axis normalAxis1 = Axis.Y;

    public Transform point2;
    public Axis sliderAxis2 = Axis.X;
    public Axis normalAxis2 = Axis.Y;

    public float limitsMax = float.MinValue;
    public float limitsMin = float.MaxValue;

    public SpringSettings springSettings = new SpringSettings();

    public float maxFrictionForce = 0.0f;

    public MotorSettings motorSettings = new MotorSettings();

    public override JSONNode ExportToJSON()
    {
        JSONNode exportConstraint = new JSONObject();

        exportConstraint.Add("autoDetectPoint", autoDetectPoint);
        exportConstraint.Add("point1", AOP.Helpers.ConvertVectorToJSONArray(point1.transform.position));
        exportConstraint.Add("sliderAxis1", AOP.Helpers.ConvertAxisToJsonArray(sliderAxis1));
        exportConstraint.Add("normalAxis1", AOP.Helpers.ConvertAxisToJsonArray(normalAxis1));
        exportConstraint.Add("point2", AOP.Helpers.ConvertVectorToJSONArray(point2.transform.position));
        exportConstraint.Add("sliderAxis2", AOP.Helpers.ConvertAxisToJsonArray(sliderAxis2));
        exportConstraint.Add("normalAxis2", AOP.Helpers.ConvertAxisToJsonArray(normalAxis2));
        exportConstraint.Add("limitsMax", limitsMax);
        exportConstraint.Add("limitsMin", limitsMin);
        exportConstraint.Add("springSettings", AOP.Helpers.ConvertSpringSettingsToJson(springSettings));
        exportConstraint.Add("maxFrictionForce", maxFrictionForce);
        exportConstraint.Add("motorSettings", AOP.Helpers.ConvertMotorSettingsToJson(motorSettings));

        return exportConstraint;
    }

    public override void InitializePoints(GameObject point1, GameObject point2, GameObject bodyPoint1 = null, GameObject bodyPoint2 = null, GameObject fixedPoint1 = null, GameObject fixedPoint2 = null)
    {
        base.InitializePoints(point1, point2, bodyPoint1, bodyPoint2, fixedPoint1, fixedPoint2);

        point1.SetActive(true);
        point2.SetActive(true);
        if (point1 != null)
            this.point1 = point1.transform;
        if (point2 != null)
            this.point2 = point2.transform;
    }

    public override void DrawGizmos()
    {
        DrawGizmoSphere(point1, Color.green, "Point 1");
        DrawGizmoSphere(point2, Color.red, "Point 2");
    }
}
