using LDtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SINEATER.Game.CoreUtils
{
    public class LDTKEntity<T> 
    {
        public T Component;
        public EntityInstance Instance;
    }
    public static class LDTKLevelExtension
    {
        public static EntityInstance[] GetEntityInstances<T>(this LDtkLevel level) where T : ILDtkEntity, new()
        {
            List<EntityInstance> entities = [];

            foreach (LayerInstance layer in level.LayerInstances ?? Array.Empty<LayerInstance>())
            {
                if (layer._Type == LayerType.Entities)
                {
                    foreach (EntityInstance entityInstance in layer.EntityInstances)
                    {
                        if (entityInstance._Identifier != typeof(T).Name)
                        {
                            continue;
                        }

                        entities.Add(entityInstance);
                    }
                }
            }

            return [.. entities];

        }

        public static LDTKEntity<T>[] GetLDKTEntities<T>(this LDtkLevel level) where T : ILDtkEntity, new()
        {
            List<LDTKEntity<T>> result = [];
            var entityInstances = level.GetEntityInstances<T>();
            var entities = level.GetEntities<T>();

            foreach(var entityInstance in entityInstances)
            {
                var item = new LDTKEntity<T>();
                item.Instance = entityInstance;
                item.Component = entities.First(x => x.Iid == entityInstance.Iid);
                result.Add(item);
            }

            return [.. result];
        }
    }
}
