using Newtonsoft.Json;
using System.IO;

namespace SINEATER.Serialization
{
    public static class DataSerializer
    {
        public static T? Load<T>(string json)
        {
            var deserializedObject = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });

            return deserializedObject;
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
    }
}