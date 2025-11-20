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
        public static T? Load<T>(string json, bool ignoreTypes = false)
        {
            var deserializedObject = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                TypeNameHandling = ignoreTypes ? TypeNameHandling.None : TypeNameHandling.Objects
            });

            return deserializedObject;
        }
        public static void Serialize<T>(T target, JsonSerializerSettings settings)
        {
            string serializedJson = JsonConvert.SerializeObject(target, Formatting.Indented, settings);

            using (var sw = new StreamWriter("result.json"))
            {
                sw.WriteLine(serializedJson);
            }
        }
        public static void Serialize<T>(T target, bool ignoreTypes = false, string fileName = "result.json")
        {
            string serializedJson = JsonConvert.SerializeObject(target, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = ignoreTypes ? TypeNameHandling.None : TypeNameHandling.Objects
            });

            using (var sw = new StreamWriter(fileName))
            {
                sw.WriteLine(serializedJson);
            }
        }
    }
}