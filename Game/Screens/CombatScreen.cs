using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Graphics;
using SINEATER.Game.Loadable;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.SinMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MonoGame.Extended.Animations;
using Wintellect.PowerCollections;
using Encounter = SINEATER.Game.Gameplay.Encounter;
using IDrawable = SINEATER.Game.CoreUtils.IDrawable;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Reward = SINEATER.Game.Gameplay.Reward;

namespace SINEATER.Game.Screens;

public abstract class CombatAnimation : IDrawable
{
    public bool Finished { get; set; } = false;
    public int Time { get; private set; } = 0;

    public virtual void Update(Drawing.RenderContext context)
    {
        Time += context.Time.ElapsedGameTime.Milliseconds;
    }
}

public class CombatScreen : Screen
{
    private List<Texture2D> _city = [];
    private Queue<Character> _turn = [];
    private bool _timeFlow = true;
    private float _levelTime = 60;
    private bool _paused = false;
    private bool _over = false;
    private WorldMapScreen _world;
    
    public List<CombatAnimation> Animations = [];
    
    public CombatScreen(WorldMapScreen world, (int X, int Y) xy, Encounter encounter, Reward reward) : base()
    {
        _world = world;
        _xy = xy;
        _reward = reward.Rewards.ToArray();
        foreach (var p in Game.Party.Characters)
        {
            p.Guard = 1;
        }
    }

    public override void Initialize()
    {
        Muse.SetGameState(EMusicState.Combat);
        for (var i = 1; i < 7; i++)
        {
            _city.Add(Game.Content.Load<Texture2D>($"locations/Dusk City/City Dusk - {i}"));
        }
    }

    private EInputAction[] _qwer = [
        EInputAction.Combat1,
        EInputAction.Combat2,
        EInputAction.Combat3,
        EInputAction.Combat4,
    ];
    
    public override void Update(EScreenFadeState fade, GameTime gameTime)
    {
        // TODO: if animations are running, don't update the items
        if (Animations.Count > 0)
        {
            return;
        }
        
        var i = 0;
        foreach (var chr in SineaterGame.Instance.Party.Characters)
        {
            foreach (var item in chr.Items)
            {
                if (item != null)
                {
                    if (item.TimeGauge < 100)
                    {
                        var stat = chr.Stats[i];
                        var scale = item.Scale[i];
                        item.TimeGauge += Math.Max(0.05f, stat * scale * 2 * gameTime.ElapsedGameTime.Milliseconds / 1000.0f);
                    }
                    
                    if (item.TimeGauge >= 100)
                    {
                        item.TimeGauge = 100;
                    }
                }
            }

            if (InputM.IsActive(_qwer[i]))
            {
                if (chr.AnyItemReady)
                {
                    foreach (var item in chr.Items)
                    {
                        if (item != null)
                        {
                            item.TimeGauge = 0;
                            // TODO: run animations here
                        }
                    }
                }
            }
            i++;
        }
    }
    
    public override void Draw(EScreenFadeState fade, SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        var rc = new Drawing.RenderContext(batch, gameTime);
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);
        
            foreach (var player in SineaterGame.Instance.Party.Characters)
            {
                
            }
        
            if (Animations.Count > 0)
            {
                Animations[0].Update(rc);
            }
        batch.End();
        
        // GUI
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);

            rc.Party(60, 800);
        batch.End();
    }

    private readonly (int, List<Item>)[] _reward;
    private readonly (int X, int Y) _xy;
    
    private void Swap(int leftIndex, int rightIndex)
    {
        (Game.Party.Characters[leftIndex], Game.Party.Characters[rightIndex]) = (Game.Party.Characters[rightIndex], Game.Party.Characters[leftIndex]);
        //(_times[leftIndex], _times[rightIndex]) = (_times[rightIndex], _times[leftIndex]);
    }
}
