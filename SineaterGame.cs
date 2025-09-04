using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SINEATER.Content;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER;

public class SineaterGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    private Texture2D _mrmo;
    private Texture2D _ibm;
    private Texture2D _room;

    private Effect _crt;
    private RenderTarget2D _renderTarget;
    
    private const int Width = 1280;
    private const int Height = 960;

    private Focus _focus;
    
    public Bar Bar;
    public Dictionary<string, TextLayer> Layers = new();
    public IScreen? CurrentScreen = null;
    
    public SineaterGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }
    
    private void SetupCrt(int w, int h)
    {
        _crt.Parameters["hardScan"]?.SetValue(-5.0f);
        _crt.Parameters["hardPix"]?.SetValue(-3.0f);
        _crt.Parameters["warpX"]?.SetValue(0.05f);
        _crt.Parameters["warpY"]?.SetValue(0.07f);
        _crt.Parameters["maskDark"]?.SetValue(0.25f);
        _crt.Parameters["maskLight"]?.SetValue(2.5f);
        _crt.Parameters["scaleInLinearGamma"]?.SetValue(0.1f);
        _crt.Parameters["shadowMask"]?.SetValue(3.0f);
        _crt.Parameters["brightboost"]?.SetValue(1.0f);
        _crt.Parameters["hardBloomScan"]?.SetValue(-1.5f);
        _crt.Parameters["hardBloomPix"]?.SetValue(-2.0f);
        _crt.Parameters["bloomAmount"]?.SetValue(0.15f);
        _crt.Parameters["shape"]?.SetValue(2.0f);
        _crt.Parameters["textureSize"].SetValue(new Vector2(w, h));
        _crt.Parameters["videoSize"].SetValue(new Vector2(w, h));
        _crt.Parameters["outputSize"].SetValue(new Vector2(w, h));
    }

    protected override void LoadContent()
    {
        _graphics.PreferredBackBufferWidth = Width;
        _graphics.PreferredBackBufferHeight = Height;
        _graphics.ApplyChanges();
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
    
        _mrmo = Content.Load<Texture2D>("MRMOTEXT");
        _ibm = Content.Load<Texture2D>("Codepage");
        _room = Content.Load<Texture2D>("room");

        var mrmoLayer = new TextLayer(_mrmo, new Vector2(36, 28), new Vector2(16, 16),new Vector2(16, 64), new Vector2(2, 1), 2);
        mrmoLayer.Map(" ", 0, 0);
        mrmoLayer.Map("!\"#$%&'()*+,-./", 1, 54);
        mrmoLayer.Map("@abcdefghijklmno", 0, 55);
        mrmoLayer.Map("ABCDEFGHIJKLMNO", 1, 55);
        mrmoLayer.Map("`{|}~", 0, 56);
        mrmoLayer.Map("0123456789:;<=>?", 0, 59);
        mrmoLayer.Map("pqrstuvwxyz[\\]^_", 0, 60);
        mrmoLayer.Map("PQRSTUVWXYZ", 0, 60);
        Layers.Add("mrmo", mrmoLayer);
        
        var miniMrmoLayer = new TextLayer(_mrmo, new Vector2(74, 56), new Vector2(16, 16),new Vector2(16, 64), new Vector2(3, 2), 1);
        miniMrmoLayer.Map(" ", 0, 0);
        miniMrmoLayer.Map("!\"#$%&'()*+,-./", 1, 54);
        miniMrmoLayer.Map("@abcdefghijklmno", 0, 55);
        miniMrmoLayer.Map("ABCDEFGHIJKLMNO", 1, 55);
        miniMrmoLayer.Map("`{|}~", 0, 56);
        miniMrmoLayer.Map("0123456789:;<=>?", 0, 59);
        miniMrmoLayer.Map("pqrstuvwxyz[\\]^_", 0, 60);
        miniMrmoLayer.Map("PQRSTUVWXYZ", 0, 60);
        Layers.Add("mini", miniMrmoLayer);
        
        var ibmLayer = new TextLayer(_ibm, new Vector2(74, 28), new Vector2(8, 16), new Vector2(32, 8), new Vector2(3, 1), 2);
        ibmLayer.SetOffset(1, 0);
        ibmLayer.Map(" !\"#$%&'()*+,-./0123456789:;<=>?", 0, 1);
        ibmLayer.Map("@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_", 0, 2);
        ibmLayer.Map("`abcdefghijklmnopqrstuvwxyz{|}~", 0, 3);
        Layers.Add("ascii", ibmLayer);
        
        _crt = Content.Load<Effect>("crt");
        SetupCrt(Width, Height);

        _focus = new Focus(_crt);
        Bar = new Bar(50, ibmLayer, new StaminaBar());
        /*_bar.Add<HungerBar>(4);
        _bar.Add<DamageBar>(1);
        _bar.Add<SleepBar>(1);
        _bar.Add<PoisonBar>(5);
        _bar.Add<InsanityBar>(5);
        _bar.Add<FlameBar>(3);
        _bar.Add<DeathBar>(3);
        _bar.Add<FocusBar>(4);
        _bar.Add<FrostBar>(3);
        _bar.Spend(3);
        */

        new Party();
        CurrentScreen = new CombatMapScreen(this, ETerrainKind.Cave);
    }
    
    protected override void Update(GameTime gameTime)
    {
        var frameRate = Math.Ceiling(1 / gameTime.ElapsedGameTime.TotalSeconds);
        Window.Title = "SINEATER | " + frameRate + " FPS";
        
        if (KB.HasBeenPressed(Keys.F10))
        {
            Exit();
        }
        
        CurrentScreen?.Update(gameTime);
        Bar.Update(gameTime);

        _focus.Update();

        base.Update(gameTime);
        KB.Update();
    }

    protected override void Draw(GameTime gameTime)
    {
        CurrentScreen?.Draw(gameTime);
        Bar.Draw(10, 25);
        
        var focus = _focus.Get();
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);

        foreach (var layer in new[]{ "mrmo", "mini", "ascii" })
        {
            Layers[layer].Draw(_spriteBatch);
        }
        
        GraphicsDevice.SetRenderTarget(null);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _crt);
        _spriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();
        
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
        var f = 1 - focus * 0.8f;
        _spriteBatch.Draw(_room, new Vector2(-focus, -focus * 0.5f) * 66, null, 
            new Color(f, f, f, 0.25f), 0, Vector2.Zero, (1.0f + focus * 0.1f) / 1.5f, 
            SpriteEffects.None, 0.0f);
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
}