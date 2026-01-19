using System;
using System.Collections;
using System.Threading.Tasks;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Graphics;
using SINEATER.Game.Loadable;
using SINEATER.Tools.SinMod;
using Color = Microsoft.Xna.Framework.Color;
using SINEATER.Game.LookNFeel;
using Encounter = SINEATER.Game.Gameplay.Encounter;
using Reward = SINEATER.Game.Gameplay.Reward;
using LDtk;
using LDtk.Renderer;
using Newtonsoft.Json;

namespace SINEATER.Game.Screens;

public enum EOption
{
    Volume,
}

public class OptionsSaveFile
{
    [JsonProperty] public int Volume = 5;
}

public class OptionsStateContext
{
    public EOption Option;
}

public partial class OptionsStateEventReceiver
{
    public OptionsStateEventReceiver() { Hook(); }
    [Event] public void OnVolumeChangedEvent(ref int dv) {}
}

public static class OptionsEventHandler
{
    [Event(order: 1)]
    public static void OnVolumeChangedEvent(ref int dv)
    {
        // change and save volume
    }
}

public class OptionsScreen(SineaterGame game) : Screen(game)
{
    private OptionsStateContext _ctx;
    private OptionsSaveFile _save;
    
    public override void Initialize(SineaterGame game)
    {
        _ctx = new OptionsStateContext() { Option = EOption.Volume };
        // TODO: load save from somewhere 
    }
    
    public override void Update(GameTime gameTime)
    {
        // handle inputs and send events for OptionsEventHandler
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        // just draw things
    }
}
