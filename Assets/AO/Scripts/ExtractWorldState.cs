using System.IO;
using SimpleJSON;
using UnityEngine;
using static AOP.Helpers;

public static class AOSceneExporter
{
    public static string ExportSceneToJson(string worldName, string worldDescription, string imageURL)
    {
        JSONNode exportWorldState = new JSONObject();
        JSONNode bodies = new JSONArray();

        exportWorldState["name"] = worldName;
        exportWorldState["description"] = worldDescription;
        exportWorldState["imageTXID"] = imageURL;

        foreach (var body in Object.FindObjectsOfType<BodyComponent>())
        {
            JSONNode exportBody = new JSONObject();
            exportBody.Add("customID", body.customID);
            exportBody.Add("position", ConvertVectorToJSONArray(body.position));
            exportBody.Add("rotation", ConvertQuaternionToJSONArray(body.rotation));
            exportBody.Add("size", ConvertVectorToJSONArray(body.size));
            exportBody.Add("center", ConvertVectorToJSONArray(body.center));

            exportBody.Add("radius", body.radius);
            exportBody.Add("height", body.height);
            exportBody.Add("motionType", body.motionType.ToString());
            exportBody.Add("motionQuality", body.motionQuality.ToString());
            exportBody.Add("layer", body.layer.ToString());
            exportBody.Add("shape", body.shape.ToString());
            exportBody.Add("activate", body.activate);
            exportBody.Add("enhancedInternalEdgeRemoval", body.enhancedInternalEdgeRemoval);
            exportBody.Add("allowSleeping", body.allowSleeping);

            exportBody.Add("linearVelocity", ConvertVectorToJSONArray(body.linearVelocity));
            exportBody.Add("angularVelocity", ConvertVectorToJSONArray(body.angularVelocity));
            exportBody.Add("friction", body.friction);
            exportBody.Add("restitution", body.restitution);
            exportBody.Add("linearDamping", body.linearDamping);
            exportBody.Add("angularDamping", body.angularDamping);
            exportBody.Add("maxLinearVelocity", body.maxLinearVelocity);
            exportBody.Add("maxAngularVelocity", body.maxAngularVelocity);
            exportBody.Add("gravityFactor", body.gravityFactor);

            JSONNode data = JSON.Parse(body.data);
            exportBody.Add("data", data);

            bodies.Add(exportBody);
        }

        exportWorldState.Add("bodies", bodies);

        JSONNode constraints = new JSONArray();
        foreach (var constraint in Object.FindObjectsOfType<ConstraintBase>())
        {
            constraints.Add(constraint.ExportToJSON());
        }

        exportWorldState.Add("constraints", constraints);

        string output = exportWorldState.ToString(4);

        return output;
    }
}