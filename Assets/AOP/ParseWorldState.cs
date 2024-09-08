using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using SimpleJSON;
using Unity.VisualScripting;

public class MetaData
{
    public string shape;
    public string prefab;
    public string primitive;
    public Vector3 center;
    public Vector3 size;
    public Vector3 scale;

    public MetaData(string data)
    {
        UpdateMetaData(data);
    }

    public void UpdateMetaData(string data)
    {
        JSONNode dataJSON = GetDataJSON(data);
        shape = dataJSON["shape"];
        prefab = dataJSON["prefab"];
        primitive = dataJSON["primitive"];
        center = new Vector3(dataJSON["collider"]["center"]["x"], dataJSON["collider"]["center"]["y"], dataJSON["collider"]["center"]["z"]);
        size = new Vector3(dataJSON["collider"]["size"]["x"], dataJSON["collider"]["size"]["y"], dataJSON["collider"]["size"]["z"]);
        scale = new Vector3(dataJSON["collider"]["scale"]["x"], dataJSON["collider"]["scale"]["y"], dataJSON["collider"]["scale"]["z"]);
    }

    private JSONNode GetDataJSON(string data)
    {
        data = data.Replace("\\\"", "");
        data = data.Replace("\"", "");
        data = data.Replace("'", "\"");
        return JSON.Parse(data);
    }
}

public class Body
{
    public int id;
    public Vector3 position;

    public Quaternion rotation;

    public Vector3 size;


    public float radius;
    public float height;

    public string motion_type;

    public string shape;
    public MetaData data;

    public GameObject gameObject;

    public Body(JSONNode body)
    {
        id = body["id"];
        data = new MetaData(body["data"]);
        UpdateBody(body);
        gameObject = null;
    }

    public void UpdateBody(JSONNode body)
    {
        position = new Vector3(body["position"][0], body["position"][1], body["position"][2]);
        rotation = new Quaternion(body["rotation"][0], body["rotation"][1], body["rotation"][2], body["rotation"][3]);
        size = new Vector3(body["size"][0], body["size"][1], body["size"][2]);
        radius = body["radius"];
        height = body["height"];
        motion_type = body["motion_type"];
        shape = body["shape"];
        data.UpdateMetaData(body["data"]);
    }

}

public class ParseWorldState : MonoBehaviour
{
    public float deltaTime;

    public Texture2D normalMap;
    public Texture2D albedoMap;

    public Material bodyMaterial;
    public Material constraintMaterial;

    // private Dictionary<int, Constraint> constraints = new Dictionary<int, Constraint>();
    private Dictionary<int, Body> bodies = new Dictionary<int, Body>();

    // List of colors to use for materials
    private readonly List<Color> colors = new List<Color>
    {
        new Color(0.4f, 0.4f, 0.4f),
        new Color(1.0f, 0.0f, 0.0f),
        new Color(0.0f, 1.0f, 0.0f),
        new Color(0.0f, 0.0f, 1.0f),
        new Color(1.0f, 1.0f, 0.0f),
        new Color(1.0f, 0.0f, 1.0f),
        new Color(0.0f, 1.0f, 1.0f),
        new Color(1.0f, 1.0f, 1.0f),
        new Color(0.5f, 0.5f, 0.5f),
        new Color(0.5f, 0.5f, 0.0f)
    };


    // Coroutine to parse and display the world states
    private IEnumerator Start()
    {
        int frame = 0;

        // Read and parse the JSON file
        string jsonString = File.ReadAllText("\\\\wsl.localhost\\Ubuntu\\home\\peterfarber\\AOLibs\\AO-Physics\\tests-loader\\simulated_world_state.json");
        JSONNode parseWorldState = JSON.Parse(jsonString);
        JSONArray worldStates = parseWorldState["worldStates"].AsArray;

        // Iterate through each worldstate in the parseWorldState JSON
        foreach (JSONNode worldState in worldStates)
        {
            // Handle each body in the world state
            foreach (JSONNode body in worldState["bodies"].AsArray)
            {
                Body _body = null;

                if (!bodies.ContainsKey(body["id"]))
                {
                    _body = new Body(body);

                    // Check if data contains prefab or primitive
                    if (_body.data.prefab == null)
                    {
                        string primitive = _body.data.primitive;
                        _body.shape = primitive;
                        // Create a GameObject based on the shape type
                        _body.gameObject = CreateGameObject(_body);

                    }
                    else
                    {
                        //Search for prefab
                        string prefabstr = _body.data.prefab;

                        // Load the prefab
                        GameObject prefabObj = Resources.Load<GameObject>("Prefabs/" + prefabstr);
                        _body.gameObject = Instantiate(prefabObj);

                    }
                    // Set up the material for the body
                    SetupBodyMaterial(_body);

                    // Add the body to the dictionary
                    bodies.Add(body["id"], _body);

                    // Turn body.data into json
                }
                else
                {
                    // Retrieve the existing GameObject
                    _body = bodies[body["id"]];
                    _body.UpdateBody(body);

                }

                // Update the transform and scale of the GameObject
                UpdateTransform(_body);
            }

            // Destroy any bodies that are no longer present in the world state
            DestroyAbsentBodies(worldState);

            // // Handle each constraint in the world state
            // foreach (JSONNode constraint in worldState["constraints"].AsArray)
            // {
            //     if (!constraints.ContainsKey(constraint["id"]))
            //     {
            //         // Create GameObjects for the constraint points
            //         SetupConstraintPoints(constraint);

            //         // Add the constraint to the dictionary
            //         constraints.Add(constraint["id"], constraint);
            //     }
            // }

            // Increment the frame count and wait for the next update
            frame++;
            yield return new WaitForSeconds(frame == 1 ? 2 : deltaTime);
        }

        yield return null;
    }

    // Creates a GameObject based on the body's shape
    private GameObject CreateGameObject(Body body)
    {
        GameObject obj = body.shape switch
        {
            "Box" => GameObject.CreatePrimitive(PrimitiveType.Cube),
            "Sphere" => GameObject.CreatePrimitive(PrimitiveType.Sphere),
            "Capsule" => GameObject.CreatePrimitive(PrimitiveType.Capsule),
            "Cylinder" => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
            _ => null
        };

        return obj;
    }

    // Sets up the material for the body GameObject
    private void SetupBodyMaterial(Body body)
    {
        Material material = new Material(bodyMaterial)
        {
            mainTextureScale = new Vector2(body.size[0], body.size[2])
        };

        material.SetTexture("_BaseColorMap", albedoMap);
        material.SetTexture("_NormalMap", normalMap);

        // Assign a color from the predefined list
        Color color = colors[bodies.Count % colors.Count];
        material.SetColor("_BaseColor", color);

        // body.gameObject.GetComponent<MeshRenderer>().material = material;
    }


    // Updates the position, rotation, and scale of the body GameObject with a parent object for handling position and rotation
    private void UpdateTransform(Body body)
    {
        // If the body doesn't have a parent object, create one
        if (body.gameObject.transform.parent == null)
        {
            // Create a new empty GameObject to act as the parent
            GameObject parentObject = new GameObject("BodyParent_" + body.id);

            // Set the parent of the body GameObject to this new parent
            body.gameObject.transform.SetParent(parentObject.transform);

            // Set the parent object's position and rotation at the body's center
            parentObject.transform.position = body.position;
            parentObject.transform.rotation = body.rotation;
        }
        // Adjust the body's position relative to the parent object
        Vector3 center = body.data.center;
        Vector3 scale = body.data.scale;

    
        // Now update the parent object's position and rotation
        Transform parentTransform = body.gameObject.transform.parent;

        // Update the parent object to the body's position and rotation
        parentTransform.position = body.position;
        parentTransform.rotation = body.rotation;

        // Update the body's scale independently
        body.gameObject.transform.localScale = body.data.scale;

        // Offset the body's local position to account for the center of the collider
        body.gameObject.transform.localPosition = new Vector3(
            -center.x * scale.x,
            -center.y * scale.y,
            -center.z * scale.z
        );


    }

    // Destroys bodies that are not present in the current world state
    private void DestroyAbsentBodies(JSONNode worldState)
    {
        List<int> toDestroy = new List<int>();

        foreach (int id in bodies.Keys)
        {
            bool found = false;

            foreach (JSONObject body in worldState["bodies"].AsArray)
            {
                if (id == body["id"])
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                toDestroy.Add(id);
            }
        }

        foreach (int id in toDestroy)
        {
            Destroy(bodies[id].gameObject);
            bodies.Remove(id);
        }
    }

    // // Sets up the constraint points as GameObjects
    // private void SetupConstraintPoints(Constraint constraint)
    // {
    //     constraint.point1_obj = CreateConstraintPoint();
    //     constraint.point2_obj = CreateConstraintPoint();

    //     if (constraint.space == 0 || constraint.space == 1)
    //     {
    //         constraint.point1_obj.transform.parent = bodies[constraint.body1ID].transform;
    //         constraint.point2_obj.transform.parent = bodies[constraint.body2ID].transform;
    //     }

    //     constraint.point1_obj.transform.position = new Vector3(constraint.point1[0], constraint.point1[1], constraint.point1[2]);
    //     constraint.point2_obj.transform.position = new Vector3(constraint.point2[0], constraint.point2[1], constraint.point2[2]);
    // }

    // // Creates a spherical GameObject for a constraint point
    // private GameObject CreateConstraintPoint()
    // {
    //     GameObject pointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    //     Material material = new Material(constraintMaterial);
    //     pointObj.GetComponent<MeshRenderer>().material = material;
    //     pointObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
    //     return pointObj;
    // }

    // Update is called once per frame
    private void Update()
    {
        // // Draws lines between the constraint points
        // foreach (Constraint constraint in constraints.Values)
        // {
        //     GameObject obj1 = bodies[constraint.body1ID];
        //     GameObject obj2 = bodies[constraint.body2ID];

        //     Debug.DrawLine(obj1.transform.position, obj2.transform.position, Color.green);
        // }

        // Resets the simulation on space key press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetSimulation();
        }
    }

    // Resets the simulation by clearing bodies and constraints
    private void ResetSimulation()
    {
        // constraints.Clear();

        foreach (Body body in bodies.Values)
        {
            Destroy(body.gameObject);
        }

        // foreach (Constraint constraint in constraints.Values)
        // {
        //     Destroy(constraint.point1_obj);
        //     Destroy(constraint.point2_obj);
        // }

        bodies.Clear();
        StopAllCoroutines();
        StartCoroutine(Start());
    }


}