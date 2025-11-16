using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;

namespace SINEATER.Input
{
    internal class InputManager
    {
        public static InputManager Instance = new();

        private Dictionary<string, InputContext> _loadedContexts = new();

        private Stack<InputContext> InputStacks = new();

        public void Initialize(ContentManager contentManager)
        {
            // load json
        }

        public bool IsActionActive(EInputActions action)
        {
            var context = InputStacks.Peek();
            if (context != null)
            {
                var definition = context.Inputs.Find(x => x.InputAction == action);
                return true;
                //definition.
            }
            return false;
        }

        public void PushContext(string contextName)
        {
            if (_loadedContexts.ContainsKey(contextName))
            {
                InputStacks.Push(_loadedContexts[contextName]);
            }
        }

        public void PopContext()
        {
            InputStacks.Pop();
        }
    }
}
