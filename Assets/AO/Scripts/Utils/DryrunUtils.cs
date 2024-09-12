using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using System;
using SimpleJSON;
using System.Threading.Tasks;
using System.Collections;

namespace AO
{
    public static class DryrunUtils
    {
        private static string baseUrl = "https://cu.ao-testnet.xyz/dry-run?process-id=";
        private static int timeout = 120;
        private static int resendIndex = 0;

        // Resend settings (can be customized as needed)
        private static List<int> resendDelays = new List<int> { 3, 30, 60 };
        private static bool increaseResendDelay = true;
        private static bool resendIfResultFalse = false;

        // public static void SendRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, string ownerId = "1234", MonoBehaviour caller = null)
        // {
        //     if (caller != null)
        //     {
        //         caller.StartCoroutine(SendRequestCoroutine(pid, tags, callback, data, ownerId));
        //     }
        //     else
        //     {
        //         Debug.LogError("Caller MonoBehaviour is null. Can't start coroutine.");
        //     }
        // }

        /// <summary>
        /// Coroutine to send the request and handle the response.
        /// </summary>
        public static IEnumerator SendRequestCoroutine(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, string ownerId = "1234")
        {
            yield return SendHttpPostRequest(pid, tags, callback, data, ownerId);
        }

        /// <summary>
        /// Coroutine to send the HTTP POST request to the server and handle the response.
        /// </summary>
        private static IEnumerator SendHttpPostRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = "", string ownerId = "1234")
        {
            string url = baseUrl + pid;
            string jsonBody = CreateJsonBody(pid, ownerId, tags, data);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeout;
                request.SetRequestHeader("Content-Type", "application/json");

                Debug.Log("Sending request to: " + url);

                yield return request.SendWebRequest();

                // Handle the response
                NodeCU jsonResponse;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    jsonResponse = new NodeCU($"{{\"Error\":\"{request.error}\"}}");

                    if (resendIfResultFalse)
                    {
                        yield return ResendRequestCoroutine(pid, tags, callback, data, ownerId);
                    }
                    else
                    {
                        callback?.Invoke(false, jsonResponse);
                    }
                }
                else
                {
                    Debug.Log("[DryrunUtils]: " + request.downloadHandler.text);
                    jsonResponse = new NodeCU(request.downloadHandler.text);

                    if (ShouldResend(jsonResponse))
                    {
                        yield return ResendRequestCoroutine(pid, tags, callback, data, ownerId);
                    }
                    else
                    {
                        callback?.Invoke(true, jsonResponse);
                        resendIndex = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Coroutine to resend the request in case of failure or conditions for resending are met.
        /// </summary>
        private static IEnumerator ResendRequestCoroutine(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = "", string ownerId = "1234")
        {
            yield return new WaitForSeconds(resendDelays[resendIndex]);
            yield return SendRequestCoroutine(pid, tags, callback, data, ownerId);

            if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
            {
                resendIndex++;
            }
        }

        /// <summary>
        /// Checks whether the response should trigger a resend based on the number of messages or the presence of data.
        /// </summary>
        private static bool ShouldResend(NodeCU response)
        {
            // return response.Messages.Count == 0 || response.Messages.Any(m => m.Data == null);
            return false; // Modify this logic based on actual resending conditions
        }

        /// <summary>
        /// Constructs the JSON body for the request.
        /// </summary>
        private static string CreateJsonBody(string pid, string ownerId, List<Tag> tags, string data = "")
        {
            JSONObject json = new JSONObject();
            json["Id"] = "1234";
            json["Target"] = pid;
            json["Owner"] = ownerId;

            if (!string.IsNullOrEmpty(data))
            {
                json["Data"] = data;
            }

            JSONArray tagsArray = new JSONArray();
            foreach (Tag tag in tags)
            {
                tagsArray.Add(tag.ToJson());
            }
            json["Tags"] = tagsArray;

            return json.ToString();
        }
    }

    [Serializable]
    public class Results
    {
        public List<Edge> Edges;
        //public bool hasNextPage;

        public Results(string jsonString)
        {
            Edges = new List<Edge>();

            var jsonNode = JSON.Parse(jsonString);

            Edges = new List<Edge>();
            if (jsonNode.HasKey("edges"))
            {
                foreach (var edgeNode in jsonNode["edges"].AsArray)
                {
                    Edges.Add(new Edge(edgeNode));
                }
            }
        }

        public List<string> GetAllData(string key)
        {
            List<string> data = new List<string>();

            foreach (Edge edge in Edges)
            {
                if (edge.Node != null && edge.Node is NodeCU networkResponse && networkResponse.Messages.Count > 0)
                {
                    foreach (Message message in networkResponse.Messages)
                    {
                        if (!string.IsNullOrEmpty(message.Data))
                        {
                            JSONNode dataNode = JSON.Parse(message.Data);

                            if (dataNode.HasKey(key))
                            {
                                data.Add(message.Data);
                            }
                        }
                    }
                }
            }

            return data;
        }
    }

    [Serializable]
    public class Edge
    {
        public Node Node;
        public string Cursor;

        public Edge(JSONNode edgeNode)
        {
            if (edgeNode.HasKey("node"))
            {
                // Determine the type of node based on content and instantiate accordingly
                if (edgeNode["node"].HasKey("message"))
                {
                    Node = new NodeSU(edgeNode["node"]);
                }
                else if (edgeNode["node"].HasKey("Messages"))
                {
                    Node = new NodeCU(edgeNode["node"]);
                }
                else
                {
                    Debug.LogError("NO KEY MESSAGES!!");
                }
            }

            if (edgeNode.HasKey("cursor"))
            {
                Cursor = edgeNode["cursor"];
            }
        }
    }

    [Serializable]
    public abstract class Node
    {

    }

    [Serializable]
    public class NodeSU : Node
    {
        public Message Message { get; set; }
        //public string Assignment { get; set; }

        public NodeSU(JSONNode node)
        {
            if (node.HasKey("message"))
            {
                Message = new MessageSU(node["message"]);
            }

            //if(node.HasKey("assignment"))
            //{
            //	Assignment = node["assignment"];
            //}
        }
    }

    [Serializable]
    public class NodeCU : Node
    {
        public List<Message> Messages { get; set; }
        public List<string> Assignments { get; set; }
        public List<string> Spawns { get; set; }
        public Output Output { get; set; }
        public string Error { get; set; }
        public long GasUsed { get; set; }

        public NodeCU(string jsonString) : this(JSON.Parse(jsonString))
        {
        }

        public NodeCU(JSONNode jsonNode)
        {
            Messages = new List<Message>();
            if (jsonNode.HasKey("Messages"))
            {
                foreach (var messageNode in jsonNode["Messages"].AsArray)
                {
                    Messages.Add(new Message(messageNode));
                }
            }

            Assignments = new List<string>();
            if (jsonNode.HasKey("Assignments"))
            {
                foreach (JSONNode assignmentNode in jsonNode["Assignments"].AsArray)
                {
                    Assignments.Add(assignmentNode);
                }
            }

            Spawns = new List<string>();
            if (jsonNode.HasKey("Spawns"))
            {
                foreach (JSONNode spawnNode in jsonNode["Spawns"].AsArray)
                {
                    Spawns.Add(spawnNode);
                }
            }

            if (jsonNode.HasKey("Output"))
            {
                JSONNode outputObj = jsonNode["Output"];
                Output = new Output();

                if (outputObj.HasKey("data"))
                {
                    Output.Data = outputObj["data"];
                }

                if (outputObj.HasKey("print"))
                {
                    Output.Print = outputObj["print"].AsBool;
                }

                if (outputObj.HasKey("prompt"))
                {
                    Output.Prompt = outputObj["prompt"];
                }
            }

            Error = jsonNode.HasKey("Error") ? jsonNode["Error"] : null;
            GasUsed = jsonNode.HasKey("GasUsed") ? jsonNode["GasUsed"].AsLong : 0;
        }

        public bool IsSuccessful()
        {
            return string.IsNullOrEmpty(Error);
        }
    }

    [Serializable]
    public class Message
    {
        public List<Tag> Tags { get; set; } = new List<Tag>();
        public string Data { get; set; }
        public string Anchor { get; set; }
        public string Target { get; set; }

        public Message(JSONNode messageNode)
        {
            if (messageNode.HasKey("tags") || messageNode.HasKey("Tags"))
            {
                JSONNode tagsNode = messageNode.HasKey("tags") ? messageNode["tags"] : messageNode["Tags"];
                foreach (var tagNode in tagsNode.AsArray)
                {
                    Tags.Add(new Tag(tagNode));
                }
            }

            Data = messageNode.HasKey("data") ? messageNode["data"] : messageNode.HasKey("Data") ? messageNode["Data"] : null;
            Anchor = messageNode.HasKey("anchor") ? messageNode["anchor"] : messageNode.HasKey("Anchor") ? messageNode["Anchor"] : null;
            Target = messageNode.HasKey("target") ? messageNode["target"] : messageNode.HasKey("Target") ? messageNode["Target"] : null;
        }
    }

    [Serializable]
    public class MessageSU : Message
    {
        public string Id { get; set; }
        public Owner Owner { get; set; }
        public string Signature { get; set; }

        public MessageSU(JSONNode messageNode) : base(messageNode)
        {
            Id = messageNode.HasKey("id") ? messageNode["id"] : null;
            Owner = messageNode.HasKey("owner") ? new Owner(messageNode["owner"]) : null;
            Signature = messageNode.HasKey("signature") ? messageNode["signature"] : null;
        }
    }

    [Serializable]
    public class Owner
    {
        public string Address { get; set; }
        public string Key { get; set; }

        public Owner(JSONNode ownerNode)
        {
            Address = ownerNode["address"];
            Key = ownerNode["key"];
        }
    }


    [Serializable]
    public class Tag
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Tag(JSONNode tagNode)
        {
            Name = tagNode.HasKey("name") ? tagNode["name"] : null;
            Value = tagNode.HasKey("value") ? tagNode["value"] : null;
        }

        public Tag(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public JSONObject ToJson()
        {
            var json = new JSONObject();
            json["name"] = Name;
            json["value"] = Value;
            return json;
        }
    }

    [Serializable]
    public class Output
    {
        public string Data;
        public bool Print;
        public string Prompt;
    }

    // [System.Serializable]
    // public class Process
    // {
    //     public string id;
    //     public string name;
    //     public string module;
    //     public string scheduler;
    // }
}