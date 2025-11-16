using Microsoft.Xna.Framework.Input;
using SINEATER.Content;
using System.Collections.Generic;

namespace SINEATER.Input
{
    internal abstract class IInputDefinition
    {
        public EInputActions InputAction { get; set; }

        public abstract bool IsPressed();
        public abstract bool IsReleased();
    }

    internal class SingleInputDefinition : IInputDefinition
    {
        private Keys Key = Keys.None;
        public override bool IsPressed()
        {
            return KB.IsPressed(Key);
        }

        public override bool IsReleased()
        {
            return KB.IsReleased(Key);
        }
    }

    internal class InputContext
    {
        public string Name = "";
        public List<IInputDefinition> Inputs = new();
    }
}
