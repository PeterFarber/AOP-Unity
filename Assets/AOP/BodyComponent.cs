using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SimpleJSON;

public class ReadOnlyAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.DisabledGroupScope(true))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}

[System.Serializable]
public class KeyValueEntry
{
    public string name;
    public string value;
}

public enum MotionType { Static, Kinematic, Dynamic }
public enum MotionQuality { Discrete, LinearCast }
public enum Layer { MOVING, NON_MOVING }
public enum Shape { Box, Sphere, Capsule, Cylinder }

[ExecuteInEditMode]
public class BodyComponent : MonoBehaviour
{
    [ReadOnly] public Vector3 position;
    [ReadOnly] public Quaternion rotation;
    [ReadOnly] public Vector3 size;

    [ReadOnly] public Vector3 center;

    [ReadOnly] public float radius;
    [ReadOnly] public float height;

    public MotionType motionType = MotionType.Dynamic;
    public MotionQuality motionQuality = MotionQuality.Discrete;
    public Layer layer = Layer.MOVING;
    public Shape shape = Shape.Box;

    public bool activate = true;
    public bool enhancedInternalEdgeRemoval = false;
    public bool allowSleeping = true;

    public Vector3 linearVelocity = Vector3.zero;
    public Vector3 angularVelocity = Vector3.zero;

    public float friction = 0.2f;
    public float restitution = 0.0f;
    public float linearDamping = 0.05f;
    public float angularDamping = 0.05f;
    public float maxLinearVelocity = 500.0f;
    public float maxAngularVelocity = Mathf.PI * 0.25f * 60.0f;
    public float gravityFactor = 1.0f;

    public List<KeyValueEntry> dataKeys = new List<KeyValueEntry>();
    [TextArea(5, 10)] public string data;

    private void Update()
    {
        Collider collider = GetComponent<Collider>();

        if (gameObject.isStatic)
        {
            SetStaticBodyState();
        }
        else
        {
            SetDynamicBodyState();
        }

        JSONNode colliderOffset = GetColliderOffset(collider);
        UpdateBodyProperties(colliderOffset);

        UpdateDataKeys(colliderOffset);
        data = ConvertDataKeysToJson();
    }

    private void SetStaticBodyState()
    {
        motionType = MotionType.Static;
        layer = Layer.NON_MOVING;
        activate = false;
    }

    private void SetDynamicBodyState()
    {
        if (motionType == MotionType.Static)
        {
            motionType = MotionType.Dynamic;
        }
        layer = Layer.MOVING;
        activate = true;
    }

    private JSONNode GetColliderOffset(Collider collider)
    {
        JSONNode colliderOffset = new JSONObject();

        if (collider is CapsuleCollider capsuleCollider)
        {
            shape = Shape.Capsule;
            // Check if cylinder by mesh name
            if(collider.gameObject.GetComponent<MeshFilter>() != null && collider.gameObject.GetComponent<MeshFilter>().sharedMesh.name.Contains("Cylinder"))
            {
                shape = Shape.Cylinder;
            }
            SetCapsuleProperties(capsuleCollider, colliderOffset);
        }
        else if (collider is BoxCollider boxCollider)
        {
            shape = Shape.Box;
            SetBoxProperties(boxCollider, colliderOffset);
        }
        else if (collider is SphereCollider sphereCollider)
        {
            shape = Shape.Sphere;
            SetSphereProperties(sphereCollider, colliderOffset);
        }

        SetColliderScale(colliderOffset);
        return colliderOffset;
    }

    private void SetCapsuleProperties(CapsuleCollider capsule, JSONNode colliderOffset)
    {
        radius = Mathf.Max(transform.localScale.x, transform.localScale.z) * capsule.radius;
        height = transform.localScale.y * capsule.height * 0.5f;
        AddColliderProperties(colliderOffset, radius, height, capsule.center, capsule.direction, Vector3.zero);
    }

    private void SetBoxProperties(BoxCollider box, JSONNode colliderOffset)
    {
        AddColliderProperties(colliderOffset, -1, -1, box.center, -1, box.size);
    }

    private void SetSphereProperties(SphereCollider sphere, JSONNode colliderOffset)
    {
        radius = sphere.radius;
        AddColliderProperties(colliderOffset, radius, -1, sphere.center, -1, Vector3.zero);
    }

    private void SetColliderScale(JSONNode colliderOffset)
    {
        JSONObject scale = new JSONObject
        {
            ["x"] = transform.localScale.x,
            ["y"] = transform.localScale.y,
            ["z"] = transform.localScale.z
        };
        colliderOffset.Add("scale", scale);
    }

    private void AddColliderProperties(JSONNode colliderOffset, float radius, float height, Vector3 center, int axis, Vector3 size)
    {
        colliderOffset.Add("radius", radius);
        colliderOffset.Add("height", height);
        colliderOffset.Add("shape", shape.ToString());

        JSONObject sizeJson = new JSONObject();
        sizeJson.Add("x", size.x);
        sizeJson.Add("y", size.y);
        sizeJson.Add("z", size.z);
        colliderOffset.Add("size", sizeJson);

        JSONObject centerJson = new JSONObject();
        centerJson.Add("x", center.x);
        centerJson.Add("y", center.y);
        centerJson.Add("z", center.z);
        colliderOffset.Add("center", centerJson);

        colliderOffset.Add("axis", axis);
    }

    private void UpdateBodyProperties(JSONNode colliderOffset)
    {
        Vector3 offsetCenter = colliderOffset["center"].ReadVector3();
        Vector3 offsetSize = colliderOffset["size"].ReadVector3();

        position = transform.position;
        center = Vector3.Scale(offsetCenter, transform.localScale);
        rotation = transform.rotation;
        size = Vector3.Scale(transform.localScale, offsetSize);
    }

    private void UpdateDataKeys(JSONNode colliderOffset)
    {
        UpdateDataKey("collider", colliderOffset.ToString(4));
        UpdatePrefabOrPrimitive();
    }

    private void UpdatePrefabOrPrimitive()
    {
        string prefabName = PrefabUtility.GetCorrespondingObjectFromSource(gameObject)?.name;

        if (!string.IsNullOrEmpty(prefabName))
        {
            UpdateDataKey("prefab", prefabName);
            RemoveDataKey("primitive");
        }
        else
        {
            UpdateDataKey("primitive", shape.ToString());
            // Add Materials to data
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials.Length > 0)
                {
                    string materialPaths = "[";
                    foreach (Material material in materials)
                    {
                        string materialPath = AssetDatabase.GetAssetPath(material);
                        materialPath = materialPath.Replace("Assets/Resources/", "");
                        materialPath = materialPath.Replace(".mat", "");
                        materialPaths += "\"" + materialPath + "\",";
                    }
                    materialPaths = materialPaths.TrimEnd(',');
                    materialPaths += "]";
                    UpdateDataKey("materials", materialPaths);
                }
                else
                {
                    RemoveDataKey("materials");
                }
            }
            RemoveDataKey("prefab");
        }
    }

    private void UpdateDataKey(string keyName, string value)
    {
        var key = dataKeys.Find(k => k.name == keyName);
        if (key != null)
        {
            key.value = value;
        }
        else
        {
            dataKeys.Add(new KeyValueEntry { name = keyName, value = value });
        }
    }

    private void RemoveDataKey(string keyName)
    {
        dataKeys.RemoveAll(k => k.name == keyName);
    }

    private string ConvertDataKeysToJson()
    {
        data = "{";
        foreach (var key in dataKeys)
        {
            data += "\"" + key.name + "\":" + key.value + ",";
        }
        data = data.TrimEnd(',');
        data += "}";
        JSONNode dataJson = JSONNode.Parse(data);
        data = dataJson.ToString(2);
        return data;
    }
}

public static class JSONNodeExtensions
{
    public static Vector3 ReadVector3(this JSONNode node)
    {
        return new Vector3(node["x"].AsFloat, node["y"].AsFloat, node["z"].AsFloat);
    }
}
