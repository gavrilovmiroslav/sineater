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
    private Texture2D[] _room = new Texture2D[24];
    private float _dHour;
    private Texture2D _monitor;

    private Effect _crt;
    private RenderTarget2D _renderTarget;
    
    private const int Width = 1280;
    private const int Height = 960;

    private float _currentMinutes = 0;
    private int _currentHour = 0;
    private int _nextHour = 1;
    private const int HourLengthMillis = 1000 * 60 * 60;
    private Focus _focus;
    
    public Bar Bar;
    public Dictionary<string, TextLayer> Layers = new();
    public IScreen? CurrentScreen = null;
    public Party Party;
    
    public SineaterGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        var time = DateTime.Now;
        _currentHour = time.Hour;
        var currentMillis = time.Millisecond + time.Second * 1000 + time.Minute * 1000 * 60; 
        _currentMinutes = currentMillis;
        _nextHour = (time.Hour + 1) % 24;
        _dHour = Math.Clamp((float)_currentMinutes / (float)HourLengthMillis, 0, 1);
        _dHour += (float)time.Second * 1000.0f;
        // _currentHour = 12;
        // _nextHour = 13;
        // _currentMinutes = 0;
        // _dHour = 0;
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

        for (int i = 0; i < 24; i++)
        {
            _room[i] = Content.Load<Texture2D>(i.ToString().PadLeft(2, '0'));    
        }
        
        _monitor = Content.Load<Texture2D>("monitor");

        var mrmoLayer = new TextLayer(_mrmo, new Vector2(36, 28), new Vector2(16, 16),new Vector2(16, 68), new Vector2(2, 1), 2, new Vector2(0, -3));
        mrmoLayer.Map(" ", 0, 0);
        mrmoLayer.Map("!\"#$%&'()*+,-./", 1, 54);
        mrmoLayer.Map("@abcdefghijklmno", 0, 55);
        mrmoLayer.Map("ABCDEFGHIJKLMNO", 1, 55);
        mrmoLayer.Map("`{|}~", 0, 56);
        mrmoLayer.Map("0123456789:;<=>?", 0, 59);
        mrmoLayer.Map("pqrstuvwxyz[\\]^_", 0, 60);
        mrmoLayer.Map("PQRSTUVWXYZ", 0, 60);
        Layers.Add("mrmo", mrmoLayer);

        var ibmLayer = new TextLayer(_ibm, new Vector2(74, 28), new Vector2(8, 16), new Vector2(32, 8), new Vector2(3, 1), 2, Vector2.Zero);
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

        Party = new Party();
        CurrentScreen = new CombatMapScreen(this, ETerrainKind.Cave);
    }
    
    protected override void Update(GameTime gameTime)
    {
        _currentMinutes += gameTime.ElapsedGameTime.Milliseconds;
        _dHour = Math.Clamp((float)_currentMinutes / (float)HourLengthMillis, 0, 1);
        
        if (_currentMinutes > HourLengthMillis)
        {
            _currentHour = (_currentHour + 1) % 24;
            _nextHour = (_nextHour + 1) % 24;
            _currentMinutes = 0;
        }
        
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

        foreach (var layer in new[]{ "mrmo", "ascii" })
        {
            Layers[layer].Draw(_spriteBatch);
        }
        
        GraphicsDevice.SetRenderTarget(null);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _crt);
        _spriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();
        
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
        var f = 1 - focus * 0.8f;
        _spriteBatch.Draw(_room[_currentHour], new Vector2(-focus, -focus * 0.5f) * 66, null, 
            new Color(f, f, f, Math.Clamp(1 - _dHour, 0, 1)) * 0.25f, 0, Vector2.Zero, (1.0f + focus * 0.1f) / 1.5f, 
            SpriteEffects.None, 0.0f);
        _spriteBatch.Draw(_room[_nextHour], new Vector2(-focus, -focus * 0.5f) * 66, null, 
            new Color(f, f, f, Math.Clamp(_dHour, 0, 1)) * 0.25f, 0, Vector2.Zero, (1.0f + focus * 0.1f) / 1.5f, 
            SpriteEffects.None, 0.0f);
        _spriteBatch.End();
        
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
        _spriteBatch.Draw(_monitor, new Vector2(-focus, -focus * 0.5f) * 66, null, 
            new Color(f, f, f, 0.01f), 0, Vector2.Zero, (1.0f + focus * 0.1f) / 1.5f, 
            SpriteEffects.None, 0.0f);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}