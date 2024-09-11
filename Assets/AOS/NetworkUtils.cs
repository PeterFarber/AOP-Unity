using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;

namespace AO
{
    public static class NetworkUtils
    {
        private const string GRAPHQL_URL = "https://arweave-search.goldsky.com/graphql"; // Updated URL

        public static IEnumerator FetchProcessesCoroutine(string walletID, System.Action<string> onSuccess, System.Action<string> onError)
        {
            // Correctly format the GraphQL query string, remove \n, \r
            string query = @"{
                transactions(
                    first: 100
                    owners: [""" + walletID + @"""]
                    tags: [{name: ""Type"", values: [""Process""]}]
                ) {
                    edges {
                        node {
                            id
                            tags {
                                name
                                value
                            }
                        }
                    }
                }
            }";

            // Remove \r and \n from the query, and escape quotes properly
            string cleanedQuery = query.Replace("\n", " ").Replace("\r", " ").Replace("\"", "\\\"");

            // Manually build the payload string
            string payload = "{\"operationName\":null,\"variables\":{},\"query\":\"" + cleanedQuery + "\"}";

            // Log the payload for debugging
            Debug.Log(payload);

            // Create the UnityWebRequest
            UnityWebRequest request = new UnityWebRequest(GRAPHQL_URL, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError(request.downloadHandler.text); // Log response for more details
                onError?.Invoke(request.error);
            }
        }
    }

    [System.Serializable]
    public class GraphQLResponse
    {
        public GraphQLData data;
    }

    [System.Serializable]
    public class GraphQLData
    {
        public GraphQLTransaction transactions;
    }

    [System.Serializable]
    public class GraphQLTransaction
    {
        public List<GraphQLEdge> edges;
    }

    [System.Serializable]
    public class GraphQLEdge
    {
        public GraphQLNode node;
    }

    [System.Serializable]
    public class GraphQLNode
    {
        public string id;
        public List<Tag> tags;
    }

    [System.Serializable]
    public class Tag
    {
        public string name;
        public string value;
    }

    [System.Serializable]
    public class Process
    {
        public string id;
        public string name;
    }
}