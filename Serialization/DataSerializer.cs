using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SINEATER.Serialization
{
    public static class DataSerializer
    {
        public static T Load<T>(string json)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T), _types);
                stream.Position = 0; // Ensure begining
                T result = (T)ser.ReadObject(stream);
                return result;
            }
        }

        public static void Serialize<T>(T target)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T), _types);
                ser.WriteObject(stream, target);
                stream.Position = 0;
                FileStream fileStream = new FileStream("result.json", FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);
                fileStream.Flush();
                fileStream.Dispose();
            }
        }

        // Types that can be deserialized
        private static List<Type> _types = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(t => t.GetTypes())
            .Where(p => (typeof(ISkirmishStep).IsAssignableFrom(p) && p != typeof(ISkirmishStep))
                || (typeof(ITrait).IsAssignableFrom(p) && p != typeof(ITrait)))
            .ToList();
    }
}

/*

 */