using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGui;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;
using SINEATER.Game.LookNFeel;
using SINEATER.Game.Screens;
using SINEATER.Tools.SinMod;
using SINEATER.steam;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER.Game;

public class SineaterGame : Microsoft.Xna.Framework.Game
{
    public static SineaterGame Instance;
    public static int DeltaTime;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Texture2D Mrmo => _mrmo;
    
    private Texture2D _mrmo;
    private Texture2D _mapmotext;
    private Texture2D _ibm;
    private Texture2D _inputText;
    private Texture2D _largeNums;
    private Texture2D _portraits;
    private Texture2D _statuses;
    private Texture2D[] _room = new Texture2D[24];
    private Texture2D[] _inputs = new Texture2D[2]; // KB + GP
    private float _dHour;
    private Texture2D _monitor;
    
    private Effect _crt;
    private RenderTarget2D _renderTargetGame;
    private RenderTarget2D _renderTargetMonitor;
    
    private const int Width = 1280;
    private const int Height = 960;

    private float _currentMinutes = 0;
    private int _currentHour = 0;
    private int _nextHour = 1;
    private const int HourLengthMillis = 1000 * 60 * 60;
    private Focus _focus;
    
    public Dictionary<string, TextLayer> Layers = new();
    public Stack<IScreen> ScreenStack = new();
    public Party Party;
    public World World { get; set; }

    private IScreen _lastScreen;

    public bool ShouldDrawImgui = false;
    private ImGuiRenderer _render;

    public SineaterGame()
    {
        Instance = this;
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

        Barks.Load(Content);
    }

    public void PopAndPushScreen(IScreen screen)
    {
        _toPush = screen;
        ScreenStack.Pop();
    }
    
    protected override void Initialize()
    {
        SteamManager.Instance.Initialize(Content.Load<string>("stats"));

        this.Window.AllowUserResizing = true;
        
        _render = new ImGuiRenderer(this).Initialize().RebuildFontAtlas();
        InputManager.Instance.Initialize("");
        InputManager.Instance.PushContext("Default"); 
        base.Initialize();

        InputManager.Instance.OnInputSourceChanged += OnInputSourceChanged;
    }

    private void OnInputSourceChanged(object? sender, EventArgs args)
    {
        if (InputManager.Instance.InputSource == InputManager.EInputSource.Keyboard)
        {
            Layers["input"] = new TextLayer(_inputs[0], new Vector2(74, 28), new Vector2(16, 16), new Vector2(12, 12), new Vector2(0, 0), 2, new Vector2(0, 0), new Vector2(0, 0));
        }
        else
        {
            Layers["input"] = new TextLayer(_inputs[1], new Vector2(74, 28), new Vector2(16, 16), new Vector2(5, 6), new Vector2(0, 0), 2, new Vector2(0, 0), new Vector2(0, 0));
        }
    }

    protected override void LoadContent()
    {
        Tools.SinMod.System.Init("audio/GUIDs.txt");

        _graphics.PreferredBackBufferWidth = Width;
        _graphics.PreferredBackBufferHeight = Height;
        _graphics.ApplyChanges();
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTargetGame = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _renderTargetMonitor = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        
        Party = new Party();
        
        _mrmo = Content.Load<Texture2D>("MRMOTEXT");
        _mapmotext = Content.Load<Texture2D>("mapmotext");
        _ibm = Content.Load<Texture2D>("Codepage");
        _inputText = Content.Load<Texture2D>("Codepage");
        _largeNums = Content.Load<Texture2D>("largenumbers");
        _portraits = Content.Load<Texture2D>("swordnsorcery_portraits");

        _inputs[0] = Content.Load<Texture2D>("inputs/KEYBOARD/KEYBOARD");
        _inputs[1] = Content.Load<Texture2D>("inputs/XBOX/XBOX");
        
        for (int i = 0; i < 24; i++)
        {
            _room[i] = Content.Load<Texture2D>("daynight/" + i.ToString().PadLeft(2, '0'));    
        }
        
        _monitor = Content.Load<Texture2D>("fingerprints");

        var portraitSmolLayer = new TextLayer(_portraits, new Vector2(Width / 80, Height / 80), new Vector2(80, 80), new Vector2(12, 10), new Vector2(0, 0), 1, new Vector2(75, -25), new Vector2(0, 0));
        Layers.Add("porsmol", portraitSmolLayer);
        
        var portraitLayer = new TextLayer(_portraits, new Vector2(Width / 80, Height / 80), new Vector2(80, 80), new Vector2(80, 80), new Vector2(0, 0), 2, new Vector2(56, -96), new Vector2(0, 0));
        Layers.Add("portrait", portraitLayer);
        
        var portrait2Layer = new TextLayer(_portraits, new Vector2(Width / 80, Height / 80), new Vector2(80, 80), new Vector2(12, 10), new Vector2(0, 0), 1, new Vector2(56, 60), new Vector2(0, 0));
        Layers.Add("portrait2", portrait2Layer);
        
        var mrmoLayer = new TextLayer(_mrmo, new Vector2(36, 28), new Vector2(16, 16),new Vector2(16, 73), new Vector2(2, 1), 2, new Vector2(0, -3), new Vector2(15, 63));
        mrmoLayer.Map(" ", 0, 0);
        mrmoLayer.Map("!\"#$%&'()*+,-./", 1, 54);
        mrmoLayer.Map("@abcdefghijklmno", 0, 55);
        mrmoLayer.Map("ABCDEFGHIJKLMNO", 1, 55);
        mrmoLayer.Map("`{|}~", 0, 56);
        mrmoLayer.Map(":;<=>?", 10, 59);
        mrmoLayer.Map("0123456789", 6, 57);
        mrmoLayer.Map("pqrstuvwxyz[\\]^_", 0, 60);
        mrmoLayer.Map("PQRSTUVWXYZ", 0, 60);
        foreach (var (u, v) in new[]
                 {
                     (2, 64), (3, 64), (4, 64), (5, 64), (6, 64), (7, 64), (8, 64), (10, 64),
                     (0, 65), (5, 65), (7, 65), (8, 65), (9, 65),
                     (0, 66), (1, 66), (2, 66), (3, 66), (7, 66), (8, 66), (9, 66), (10, 66), (11, 66), (12, 66),
                     (0, 67), (1, 67), (2, 67), (3, 67) 
                 })
        {
            mrmoLayer.SetFlip(u, v, SpriteEffects.None);
        }
        
        foreach (var (u, v) in new[]
                 {
                     (0, 64), (1, 64),
                     (1, 65), (2, 65), (3, 65), (4, 65), (6, 65),
                     (4, 66), 
                     (4, 67), (5, 67),
                 })
        {
            mrmoLayer.SetFlip(u, v, SpriteEffects.FlipHorizontally);
            mrmoLayer.SetFlip(u, v - 4, SpriteEffects.FlipHorizontally);
            mrmoLayer.SetFlip(u, v + 5, SpriteEffects.FlipHorizontally);
        }
        Layers.Add("mrmo", mrmoLayer);
        
        var mapLayer = new TextLayer(_mapmotext,new Vector2(36, 28), new Vector2(16, 16),new Vector2(16, 64), new Vector2(2, 1), 2, new Vector2(0, -3), new Vector2(15, 63));
        mapLayer.Map(" ", 0, 0);
        mapLayer.Map("!\"#$%&'()*+,-./", 1, 54);
        mapLayer.Map("@abcdefghijklmno", 0, 55);
        mapLayer.Map("ABCDEFGHIJKLMNO", 1, 55);
        mapLayer.Map("`{|}~", 0, 56);
        mapLayer.Map(":;<=>?", 10, 59);
        mapLayer.Map("0123456789", 6, 57);
        mapLayer.Map("pqrstuvwxyz[\\]^_", 0, 60);
        mapLayer.Map("PQRSTUVWXYZ", 0, 60);
        Layers.Add("map", mapLayer);
        
        var ibmMiniLayer = new TextLayer(_ibm, new Vector2(2 * 74, 2 * 28), new Vector2(8, 16), new Vector2(32, 8), new Vector2(3, 1), 1, new Vector2(2, 3), new Vector2(0, 0));
        ibmMiniLayer.SetOffset(1, 0);
        ibmMiniLayer.Map(" !\"#$%&'()*+,-./0123456789:;<=>?", 0, 1);
        ibmMiniLayer.Map("@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_", 0, 2);
        ibmMiniLayer.Map("`abcdefghijklmnopqrstuvwxyz{|}~", 0, 3);
        Layers.Add("mini", ibmMiniLayer);
        
        var ibmLayer = new TextLayer(_ibm, new Vector2(74, 28), new Vector2(8, 16), new Vector2(32, 8), new Vector2(3, 1), 2, new Vector2(2, 0), new Vector2(31, 7));
        ibmLayer.SetOffset(1, 0);
        ibmLayer.Map(" !\"#$%&'()*+,-./0123456789:;<=>?", 0, 1);
        ibmLayer.Map("@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_", 0, 2);
        ibmLayer.Map("`abcdefghijklmnopqrstuvwxyz{|}~", 0, 3);
        Layers.Add("ascii", ibmLayer);

        var inputText = new TextLayer(_inputText, new Vector2(74, 28), new Vector2(8, 16), new Vector2(32, 8), new Vector2(3, 1), 2, new Vector2(2, 0), new Vector2(31, 7));
        inputText.SetOffset(1, 0);
        inputText.Map(" !\"#$%&'()*+,-./0123456789:;<=>?", 0, 1);
        inputText.Map("@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_", 0, 2);
        inputText.Map("`abcdefghijklmnopqrstuvwxyz{|}~", 0, 3);
        Layers.Add("inputtext", inputText);

        var largeNums = new TextLayer(_largeNums, new Vector2(30, 30), new Vector2(32, 32), new Vector2(20, 20), new Vector2(0, 0), 2, new Vector2(0, 28), new Vector2(0, 0));
        largeNums.Map(" 1234567890", 0, 0);
        Layers.Add("largenums", largeNums);

        var inputLayer = new TextLayer(_inputs[0], new Vector2(74, 28), new Vector2(16, 16), new Vector2(12, 12), new Vector2(0, 0), 2, new Vector2(0, 0), new Vector2(0, 0));
        Layers.Add("input", inputLayer);

        _crt = Content.Load<Effect>("crt");
        SetupCrt(Width, Height);

        _focus = new Focus(_crt);
        ScreenStack.Push(new MainMenuScreen(this));

        Muse.Load();
    }

    protected override void Update(GameTime gameTime)
    {
        Tools.SinMod.System.Update(gameTime);
        Muse.Update(gameTime);
        
        if (_toPush != null)
        {
            ScreenStack.Push(_toPush);
            _toPush = null;
            return;
        }
        
        DeltaTime = gameTime.ElapsedGameTime.Milliseconds;
        _currentMinutes += gameTime.ElapsedGameTime.Milliseconds;
        _dHour = Math.Clamp((float)_currentMinutes / (float)HourLengthMillis, 0.01f, 0.99f);

        SteamManager.Instance.Update();

        if (_currentMinutes > HourLengthMillis)
        {
            _currentHour = (_currentHour + 1) % 24;
            _nextHour = (_nextHour + 1) % 24;
            _currentMinutes = 0;
        }

        InputManager.Instance.Update(DeltaTime);

        if (InputM.IsActive(EInputAction.Exit))
        {
            Exit();
        }
        
        if (InputM.IsActive(EInputAction.RestartExploration))
        {
            ScreenStack.Pop();
            ScreenStack.Push(new WorldMapScreen(this));
        }

        if (InputM.IsActive(EInputAction.ShowImGui))
        {
            ShouldDrawImgui = !ShouldDrawImgui;
        }

        if (ScreenStack?.Peek() is { } screen)
        {
            screen.Update(gameTime);
        }

        base.Update(gameTime);
    }

    private int _x = 35, _y = 27, _w = -71, _h = 0;
    private IScreen? _toPush;

    protected override void Draw(GameTime gameTime)
    {
        if (ScreenStack?.Peek() is { } screen)
        {
            screen.Draw(_spriteBatch, gameTime);
        }
        
        var focus = _focus.Get();

        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.SetRenderTarget(_renderTargetGame);

        Rectangle orgScissorRec = _spriteBatch.GraphicsDevice.ScissorRectangle;
        RasterizerState rasterizerState = new RasterizerState() { ScissorTestEnable = true };
        Rectangle targetRect = new Rectangle(_x, _y, GraphicsDevice.Viewport.Width + _w, GraphicsDevice.Viewport.Height + _h);
        _spriteBatch.GraphicsDevice.ScissorRectangle = targetRect;
        
        if (ScreenStack?.TryPeek(out var scrn) ?? false)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, rasterizerState);
            scrn.PreDraw(_spriteBatch, gameTime);
            _spriteBatch.End();
        }
        foreach (var layer in LayerNames)
        {
            Layers[layer].Draw(_spriteBatch);
        }
        if (ScreenStack?.TryPeek(out var scrn2) ?? false)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, rasterizerState);
            scrn2.PostDraw(_spriteBatch, gameTime);
            _spriteBatch.End();
        }
        GraphicsDevice.SetRenderTarget(null);
        
        GraphicsDevice.SetRenderTarget(_renderTargetMonitor);
        _spriteBatch.Begin(blendState: BlendState.NonPremultiplied);
        _spriteBatch.Draw(_room[_currentHour], new Vector2(-focus, -focus * 0.5f) * 66, null, 
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 0, Vector2.Zero, (1.0f + focus * 0.1f) / 1.5f, 
            SpriteEffects.None, 0.0f);
        _spriteBatch.Draw(_room[_nextHour], new Vector2(-focus, -focus * 0.5f) * 66, null, 
            new Color(1.0f, 1.0f, 1.0f, _dHour), 0, Vector2.Zero, (1.0f + focus * 0.1f) / 1.5f, 
            SpriteEffects.None, 0.0f);
        _spriteBatch.End();
        GraphicsDevice.SetRenderTarget(null);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _crt);
        _spriteBatch.Draw(_renderTargetGame, Vector2.Zero, Color.White);
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_renderTargetMonitor, Vector2.Zero, new Color(1.0f, 1.0f, 1.0f, 0.45f));
        _spriteBatch.End();
        
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
        
        _spriteBatch.End();

        if (ShouldDrawImgui)
        {
            DrawImgui(gameTime);
        }

        base.Draw(gameTime);
    }
    
    private void DrawImgui(GameTime time)
    {
        _render.BeginLayout(time);
        
        Tools.ImGuiTools.Tools.ShowTools(ref ShouldDrawImgui);

        _render.EndLayout();
    }
    
    public static IEnumerable<string> LayerNames 
        => [ "map", "mrmo", "ascii", "portrait", "portrait2", "porsmol", "mini", "largenums", "input", "inputtext"];

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
}