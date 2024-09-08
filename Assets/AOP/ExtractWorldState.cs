using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using SimpleJSON;


public class ExtractWorldState : MonoBehaviour
{
    public string filePath = "Assets/extracted_world_state.json";

    public void ExportSceneToJson()
    {
        // ExportWorldState exportWorldState = new ExportWorldState();

        JSONNode exportWorldState = new JSONObject();

        JSONNode bodies = new JSONArray();
        JSONNode constraints = new JSONArray();

        // Collect all bodies and constraints
        foreach (var body in FindObjectsOfType<BodyComponent>())
        {

            JSONNode exportBody = new JSONObject();
            JSONArray position = new();
            position.Add(body.position.x);
            position.Add(body.position.y);
            position.Add(body.position.z);
            exportBody.Add("position", position);
            JSONArray rotation = new();
            rotation.Add(body.rotation.x);
            rotation.Add(body.rotation.y);
            rotation.Add(body.rotation.z);
            rotation.Add(body.rotation.w);
            exportBody.Add("rotation", rotation);
            JSONArray size = new();
            size.Add(body.size.x);
            size.Add(body.size.y);
            size.Add(body.size.z);
            exportBody.Add("size", size);
            JSONArray center = new();
            center.Add(body.center.x);
            center.Add(body.center.y);
            center.Add(body.center.z);
            exportBody.Add("center", center);
            exportBody.Add("radius", body.radius);
            exportBody.Add("height", body.height);
            exportBody.Add("motionType", body.motionType.ToString());
            exportBody.Add("motionQuality", body.motionQuality.ToString());
            exportBody.Add("layer", body.layer.ToString());
            exportBody.Add("shape", body.shape.ToString());
            exportBody.Add("activate", body.activate);
            exportBody.Add("enhancedInternalEdgeRemoval", body.enhancedInternalEdgeRemoval);
            exportBody.Add("allowSleeping", body.allowSleeping);
            JSONArray linearVelocity = new();
            linearVelocity.Add(body.linearVelocity.x);
            linearVelocity.Add(body.linearVelocity.y);
            linearVelocity.Add(body.linearVelocity.z);
            exportBody.Add("linearVelocity", linearVelocity);
            JSONArray angularVelocity = new();
            angularVelocity.Add(body.angularVelocity.x);
            angularVelocity.Add(body.angularVelocity.y);
            angularVelocity.Add(body.angularVelocity.z);
            exportBody.Add("angularVelocity", angularVelocity);
            exportBody.Add("friction", body.friction);
            exportBody.Add("restitution", body.restitution);
            exportBody.Add("linearDamping", body.linearDamping);
            exportBody.Add("angularDamping", body.angularDamping);
            exportBody.Add("maxLinearVelocity", body.maxLinearVelocity);
            exportBody.Add("maxAngularVelocity", body.maxAngularVelocity);
            exportBody.Add("gravityFactor", body.gravityFactor);
            JSONNode data = JSON.Parse(body.data);
            exportBody.Add("data",data);


            
            bodies.Add(exportBody);

        }

        exportWorldState.Add("bodies", bodies);

        // foreach (var constraint in FindObjectsOfType<ConstraintComponent>())
        // {
        //     JSONNode exportConstraint = new JSONObject();
        //     exportConstraint.Add("id", constraint.id);
        //     exportConstraint.Add("body1ID", constraint.body1ID);
        //     exportConstraint.Add("body2ID", constraint.body2ID);
        //     exportConstraint.Add("type", constraint.type);
        //     exportConstraint.Add("space", constraint.space);
        //     JSONObject point1 = new JSONObject();
        //     point1.Add("x", constraint.point1.x);
        //     point1.Add("y", constraint.point1.y);
        //     point1.Add("z", constraint.point1.z);
        //     exportConstraint.Add("point1", point1);
        //     JSONObject point2 = new JSONObject();
        //     point2.Add("x", constraint.point2.x);
        //     point2.Add("y", constraint.point2.y);
        //     point2.Add("z", constraint.point2.z);
        //     exportConstraint.Add("point2", point2);

        //     constraints.Add(exportConstraint);
        // }

        // Convert to JSON and write to file
        File.WriteAllText(filePath, exportWorldState.ToString(4));

        Debug.Log("World state exported to " + filePath);
    }
}
