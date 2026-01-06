using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace SINEATER.Game.CoreUtils.Input
{
    public class InputDefinitionsSerializationBinder : ISerializationBinder
    {
        private Dictionary<Type, string> _typesMapping = new Dictionary<Type, string>
        {
            {
                typeof(InputAction),
                "Input"
            },
            {
                typeof(InputContext),
                "InputContext"
            }
        };

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            assemblyName = null;
            typeName = _typesMapping.Keys.Contains(serializedType) ? _typesMapping[serializedType] : serializedType.FullName;
        }

        public Type BindToType(string? assemblyName, string typeName)
        {
            return _typesMapping.First(x => x.Value == typeName).Key;
        }
    }
}
