using Microsoft.Xna.Framework.Content;
using System;

namespace SINEATER.Game.CoreUtils.Resources
{
    public class ResourceHandle<T> : IDisposable where T : class
    {
        public ResourceHandle(string name)
        {
            _resource = null;
            _name = name;
        }

        public void Reload(object? obj, EventArgs e) => Load(SineaterGame.Instance.Content);

        T? _resource;
        string _name = "";

        public void Load(ContentManager? manager)
        {
            if (manager is null)
            {
                // reload from file?
            }
            else
            {
                _resource = manager.Load<T>(_name);
            }
        }

        public void Dispose()
        {
            if (_resource is IDisposable d) d.Dispose();
        }

        public static implicit operator T(ResourceHandle<T> t)
        {
            if (t._resource == null)
            {
                Console.WriteLine($"Using resource that is not loaded: {t._name}");
            }

            return t._resource;
        }
    }
}
