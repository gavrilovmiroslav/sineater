using LDtk;
using LDtkTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGui;
using SadRex;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;
using SINEATER.Game.LookNFeel;
using SINEATER.Game.Screens;
using SINEATER.steam;
using SINEATER.Tools.SinMod;
using Color = Microsoft.Xna.Framework.Color;
using SINEATER.Game.Graphics;
using SINEATER.Game.CoreUtils.Resources;

namespace SINEATER.Game;

public class SineaterGame : Microsoft.Xna.Framework.Game
{
    public static SineaterGame Instance;
    public static float DeltaTime;
    public static ResourceManager RM => SineaterGame.Instance.ResourceM;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private ResourceManager _resourceManager = new ResourceManager();
    public Dictionary<string, (int, int)> AllSpritesMap => _allSpritesMap;
    private readonly Dictionary<string, (int, int)> _allSpritesMap = [];

    private float _dHour;
    private Image _rex;
    public Image Rex => _rex;

    public Effect Grayscale;
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
    
    public Stack<IScreen> ScreenStack = new();
    public Party Party;
    public World World { get; set; }
    public bool ShowHelp { get; set; } = false;
    
    private IScreen _lastScreen;
    public Options CurrentOptions;
    
    public bool ShouldDrawImgui = false;
    private ImGuiRenderer _render;

    private LDTKRender _ldtkRenderer;
    public LDTKRender LDtkRenderer => _ldtkRenderer;
    public LDtkWorld LDTKWorld => _ldtkWorld;

    public ResourceManager ResourceM => _resourceManager;

    private LDtkWorld _ldtkWorld;
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

    private void LoadOrCreateOptions()
    {
        try
        {
            var optionsStream = TitleContainer.OpenStream("options.json");
            var optionsJson = string.Join("\n", optionsStream.ReadLines(Encoding.Default));
            CurrentOptions = DataSerializer.Load<Options>(optionsJson);
        }
        catch
        {
            CurrentOptions = new Options();
            DataSerializer.Serialize(CurrentOptions, out var json);
            const string writePath = "options.json";
            File.WriteAllText(writePath, json);
        }
        CurrentOptions?.UpdateOptions();
    }

    private LDtkLevel _ldtkLevel;
    
    protected override void Initialize()
    {
        //SteamManager.Instance.Initialize(Content.Load<string>("stats"));
        
        this.Window.AllowUserResizing = true;
        
        var filePath = System.IO.Path.Combine(Content.RootDirectory, $"map.xp");
        using var stream = TitleContainer.OpenStream(filePath);
        _rex = Image.Load(stream);
        
        _render = new ImGuiRenderer(this).Initialize().RebuildFontAtlas();
        InputManager.Instance.Initialize("");
        InputManager.Instance.PushContext("Default"); 
        base.Initialize();

        var file = LDtk.LDtkFile.FromFile("Content/map.ldtk");
        _ldtkWorld = file.LoadWorld(Worlds.World.Iid);
        _ldtkRenderer = new LDTKRender(_spriteBatch, null);
        _ldtkLevel = _ldtkWorld.LoadLevel(0);
        
        foreach (LDtkLevel level in _ldtkWorld.Levels)
        {
            _ldtkRenderer.PrerenderLevel(level);
        }
    }

    protected override void LoadContent()
    {        
        LoadOrCreateOptions();
        Tools.SinMod.System.Init("audio/GUIDs.txt");

        _graphics.PreferredBackBufferWidth = Width;
        _graphics.PreferredBackBufferHeight = Height;
        _graphics.ApplyChanges();
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTargetGame = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _renderTargetMonitor = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        
        Party = new Party();

        _resourceManager.Load(Content);

        using var allSpritesList = TitleContainer.OpenStream("Content/sprites/all-sprites.txt");
        if (allSpritesList != null)
        {
            var lines = allSpritesList.ReadLines(Encoding.ASCII);
            foreach (var line in lines)
            {
                var split = line.Split(' ');
                var x = int.Parse(split[0]);
                var y = int.Parse(split[1]);
                var name = split[2];
                _allSpritesMap[name] = (y, x);
            }
        }

        _crt = Content.Load<Effect>("crt");
        SetupCrt(Width, Height);

        Grayscale = Content.Load<Effect>("Grayscale");

        _focus = new Focus(_crt);
        ScreenStack.Push(new MainMenuScreen());

        Muse.Load();
        CurrentOptions.UpdateOptions();
    }

    protected override void Update(GameTime gameTime)
    {
        Tools.SinMod.System.Update(gameTime);
        Muse.Update(gameTime);

        ShowHelp = InputM.IsActive(EInputAction.ShowHelp);

        if (_toPush != null)
        {
            ScreenStack.Push(_toPush);
            _toPush = null;
            return;
        }

        var millis = gameTime.ElapsedGameTime.Milliseconds;
        DeltaTime = (float)millis / 1000.0f;
        _currentMinutes += millis;
        _dHour = Math.Clamp((float)_currentMinutes / (float)HourLengthMillis, 0.01f, 0.99f);

        SteamManager.Instance.Update();

        if (_currentMinutes > HourLengthMillis)
        {
            _currentHour = (_currentHour + 1) % 24;
            _nextHour = (_nextHour + 1) % 24;
            _currentMinutes = 0;
        }

        InputManager.Instance.Update(millis);
        
        if (InputM.IsActive(EInputAction.RestartExploration))
        {
            ScreenStack.Pop();
            ScreenStack.Push(new WorldMapScreen());
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

    private const int X = 35;
    private const int Y = 27;
    private const int W = -71;
    private const int H = 0;
    
    private IScreen? _toPush;

    protected override void Draw(GameTime gameTime)
    {
        var focus = _focus.Get();

        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.SetRenderTarget(_renderTargetGame);
        
        var rasterizerState = new RasterizerState() { ScissorTestEnable = true };
        var targetRect = new Rectangle(X, Y, GraphicsDevice.Viewport.Width + W, GraphicsDevice.Viewport.Height + H - 50);
        _spriteBatch.GraphicsDevice.ScissorRectangle = targetRect;

        if (ScreenStack?.TryPeek(out var scr) ?? false)
        {
            scr.Draw(_spriteBatch, gameTime, rasterizerState);
        }

        GraphicsDevice.SetRenderTarget(null);
        
        GraphicsDevice.SetRenderTarget(_renderTargetMonitor);
        _spriteBatch.Begin(blendState: BlendState.NonPremultiplied);
        _spriteBatch.Draw(RM.Room[_currentHour], new Vector2(-focus, -focus * 0.5f) * 66, null, 
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 0, Vector2.Zero, (1.0f + focus * 0.1f) / 1.5f, 
            SpriteEffects.None, 0.0f);
        _spriteBatch.Draw(RM.Room[_nextHour], new Vector2(-focus, -focus * 0.5f) * 66, null, 
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
        
        GraphicsDevice.SetRenderTarget(null);
        
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