using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER.Input;

internal class InputContext
{
    public string Name = "";
    public List<IInputDefinition> Inputs = new();
}
