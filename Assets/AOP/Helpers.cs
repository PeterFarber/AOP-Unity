using SimpleJSON;
using UnityEngine;

namespace AOP
{
    public class Helpers
    {
        // Helper method to convert a Vector3 to a JSONArray
        public static JSONArray ConvertVectorToJSONArray(Vector3 vector)
        {
            JSONArray array = new JSONArray();
            array.Add(vector.x);
            array.Add(vector.y);
            array.Add(vector.z);
            return array;
        }

        // Helper method to convert a Quaternion to a JSONArray
        public static JSONArray ConvertQuaternionToJSONArray(Quaternion quaternion)
        {
            JSONArray array = new JSONArray();
            array.Add(quaternion.x);
            array.Add(quaternion.y);
            array.Add(quaternion.z);
            array.Add(quaternion.w);
            return array;
        }
        public static JSONArray ConvertAxisToJsonArray(Axis axis)
        {
            JSONArray array = new JSONArray();
            switch (axis)
            {
                case Axis.X:
                    array.Add(1);
                    array.Add(0);
                    array.Add(0);
                    break;
                case Axis.Y:
                    array.Add(0);
                    array.Add(1);
                    array.Add(0);
                    break;
                case Axis.Z:
                    array.Add(0);
                    array.Add(0);
                    array.Add(1);
                    break;
            }
            return array;
        }

        public static JSONNode ConvertSpringSettingsToJson(SpringSettings springSettings)
        {
            JSONNode json = new JSONObject();
            json.Add("mode", springSettings.mode.ToString());
            json.Add("stiffness", springSettings.stiffness);
            json.Add("damping", springSettings.damping);
            return json;
        }

        public static JSONNode ConvertMotorSettingsToJson(MotorSettings motorSettings)
        {
            JSONNode json = new JSONObject();
            json.Add("frequency", motorSettings.frequency);
            json.Add("damping", motorSettings.damping);
            return json;
        }

    }
}