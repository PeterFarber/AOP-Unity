using UnityEngine;
using System.IO;
using SimpleJSON;
using static AOP.Helpers;

public class ExtractWorldState : MonoBehaviour
{
    public string filePath = "Assets/extracted_world_state.json";

    public void ExportSceneToJson()
    {
        JSONNode exportWorldState = new JSONObject();
        JSONNode bodies = new JSONArray();

        // Collect all bodies
        foreach (var body in FindObjectsOfType<BodyComponent>())
        {
            JSONNode exportBody = new JSONObject();
            exportBody.Add("customID", body.customID);
            exportBody.Add("position", ConvertVectorToJSONArray(body.position));
            exportBody.Add("rotation", ConvertQuaternionToJSONArray(body.rotation));
            exportBody.Add("size", ConvertVectorToJSONArray(body.size));
            exportBody.Add("center", ConvertVectorToJSONArray(body.center));

            // Add simple properties
            exportBody.Add("radius", body.radius);
            exportBody.Add("height", body.height);
            exportBody.Add("motionType", body.motionType.ToString());
            exportBody.Add("motionQuality", body.motionQuality.ToString());
            exportBody.Add("layer", body.layer.ToString());
            exportBody.Add("shape", body.shape.ToString());
            exportBody.Add("activate", body.activate);
            exportBody.Add("enhancedInternalEdgeRemoval", body.enhancedInternalEdgeRemoval);
            exportBody.Add("allowSleeping", body.allowSleeping);

            // Add velocities and dynamics
            exportBody.Add("linearVelocity", ConvertVectorToJSONArray(body.linearVelocity));
            exportBody.Add("angularVelocity", ConvertVectorToJSONArray(body.angularVelocity));
            exportBody.Add("friction", body.friction);
            exportBody.Add("restitution", body.restitution);
            exportBody.Add("linearDamping", body.linearDamping);
            exportBody.Add("angularDamping", body.angularDamping);
            exportBody.Add("maxLinearVelocity", body.maxLinearVelocity);
            exportBody.Add("maxAngularVelocity", body.maxAngularVelocity);
            exportBody.Add("gravityFactor", body.gravityFactor);

            // Add custom data
            JSONNode data = JSON.Parse(body.data);
            exportBody.Add("data", data);

            bodies.Add(exportBody);
        }

        exportWorldState.Add("bodies", bodies);

        // // Collect all constraints
        JSONNode constraints = new JSONArray();

        foreach (var constraint in FindObjectsOfType<ConstraintBase>())
        {
            constraints.Add(constraint.ExportToJSON());
        }

        exportWorldState.Add("constraints", constraints);

        // Write JSON to file
        File.WriteAllText(filePath, exportWorldState.ToString(4));

        Debug.Log($"World state exported to {filePath}");
    }

}
