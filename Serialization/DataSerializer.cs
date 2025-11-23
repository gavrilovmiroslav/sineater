using Newtonsoft.Json;
using System.IO;

namespace SINEATER.Serialization
{
    public static class DataSerializer
    {
        public static T? Load<T>(string json, JsonSerializerSettings settings)
        {
            var deserializedObject = JsonConvert.DeserializeObject<T>(json, settings);

            return deserializedObject;
        }
        public static T? Load<T>(string json)
        {
            var deserializedObject = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });

            return deserializedObject;
        }
        public static void Load<T>(string json, out T result)
        {
            var deserializedObject = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
            });

            result =  deserializedObject;
        }
        public static void Serialize<T>(T target, JsonSerializerSettings settings)
        {
            string serializedJson = JsonConvert.SerializeObject(target, Formatting.Indented, settings);

            using (var sw = new StreamWriter("result.json"))
            {
                sw.WriteLine(serializedJson);
            }
        }
        public static void Serialize<T>(T target)
        {
            string serializedJson = JsonConvert.SerializeObject(target, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
            });

            using (var sw = new StreamWriter("result.json"))
            {
                sw.WriteLine(serializedJson);
            }
        }

        public static void Serialize<T>(T target, out string result)
        {
            result = JsonConvert.SerializeObject(target, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
            });

        }
    }
}