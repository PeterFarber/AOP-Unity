using UnityEngine;
using SimpleJSON;
using System;

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

public enum ConstraintType { Cone, Distance, Fixed, Gear, Hinge, Point, Pulley, Slider, None }


[Serializable]
public abstract class ConstraintBase : MonoBehaviour
{    // Points used in constraints

    [ReadOnly] public int id;
    public BodyComponent body1;
    public BodyComponent body2;

    public Space space;
    
    [ReadOnly] public ConstraintType constraintType = ConstraintType.None;

    public abstract void DrawGizmos();
    public virtual JSONNode ExportToJSON(){
        JSONNode exportConstraint = new JSONObject();
        exportConstraint.Add("id", id);
        exportConstraint.Add("body1ID", body1.customID);
        exportConstraint.Add("body2ID", body2.customID);
        exportConstraint.Add("space", space.ToString());
        exportConstraint.Add("type", constraintType.ToString());
        return exportConstraint;
    }

    public void OnDrawGizmos(){
        DrawGizmos();
    }

    public abstract void Initialize();

    public void Reset()
    {
        // Destory all child points
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        id = GetInstanceID();
        body1 = null;
        body2 = this.GetComponent<BodyComponent>();;
        space = Space.WorldSpace;
        constraintType = ConstraintType.None;
        Initialize();
    }

    protected void DrawGizmoSphere(GameObject point, Color color, string label)
    {
        if (point?.gameObject.activeSelf == true)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(point.transform.position, 0.33f);
            DrawString(label, point.transform.position, Color.white, new Vector2(0f, 5f));
        }
    }

    protected void CreatePoint(ref GameObject point, string name, Transform parent)
    {
        if (point == null)
        {
            point = new GameObject(name);
            point.transform.parent = parent;
            point.transform.localPosition = Vector3.zero;
            point.transform.localRotation = Quaternion.identity;
            point.transform.localScale = Vector3.one;
        }
    }

    protected void DrawString(string text, Vector3 worldPosition, Color textColor, Vector2 anchor, float textSize = 15f)
    {
#if UNITY_EDITOR
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        if (!view) return;

        Vector3 screenPosition = view.camera.WorldToScreenPoint(worldPosition);
        if (screenPosition.y < 0 || screenPosition.y > view.camera.pixelHeight || screenPosition.x < 0 || screenPosition.x > view.camera.pixelWidth || screenPosition.z < 0) return;

        var pixelRatio = UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.right).x - UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.zero).x;
        UnityEditor.Handles.BeginGUI();
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)textSize,
            normal = new GUIStyleState() { textColor = textColor }
        };
        Vector2 size = style.CalcSize(new GUIContent(text)) * pixelRatio;
        var alignedPosition = ((Vector2)screenPosition + size * ((anchor + Vector2.left + Vector2.up) / 2f)) * (Vector2.right + Vector2.down) + Vector2.up * view.camera.pixelHeight;

        GUI.Label(new Rect(alignedPosition / pixelRatio, size / pixelRatio), text, style);
        UnityEditor.Handles.EndGUI();
#endif
    }


}
