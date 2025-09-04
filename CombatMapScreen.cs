using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using RogueSharp.MapCreation;
using SINEATER.Content;

namespace SINEATER;

public enum ETerrainKind
{
    Tomb,
    Temple,
    Cave,
    Clearing,
    Ruin,
    Unknown
}

public class CombatMapScreen : IScreen
{
    private readonly int _fullWidth = 26, _fullHeight = 20;
    private readonly int _offsetX = 4, _offsetY = 3;
    private int _width, _height;
    private SineaterGame _game;
    private ETerrainKind _kind;
    private IMap? _map;
    private bool _rendered = false;
    private bool _debugView = false;
    private List<(int, int)> _entryPositions = new();
    
    private void Regenerate(bool resize) {
        if (resize)
        {
            this._width = Rnd.Instance.Next(3 * _fullWidth / 4, _fullWidth - 2);
            this._height = Rnd.Instance.Next(3 * _fullHeight / 4, _fullHeight - 2);
        }

        Regenerate();
    }
    
    private void Regenerate() => Regenerate(_kind);
    private void Regenerate(ETerrainKind kind)
    {
        _entryPositions.Clear();
        _kind = kind;
        var (a, b, c, d, e) = (0, 0, 0, _width, _height);
        switch (_kind)
        {
            case ETerrainKind.Tomb:
                (a, b, c, d, e) = (32, 2, 2, 48, 38);//36
                break;
            case ETerrainKind.Temple:
                (a, b, c, d, e) = (40, 1, 1, 48, 38); //45
                break;
            case ETerrainKind.Cave:
                (a, b, c) = (52, 3, 3); //47
                break;
            case ETerrainKind.Clearing:
                (a, b, c) = (54, 3, 1); //49
                break;
            case ETerrainKind.Ruin:
                (a, b, c) = (92, 2, 2);//89
                break;
            default:
                (a, b, c) = (Rnd.Instance.Next(1, 99), Rnd.Instance.D4, Rnd.Instance.D4);
                break;
        }
        
        Console.WriteLine($"Fill probability: {a}, iterations: {b}, cutoff: {c}, size: {_width} x {_height}");

        IMapCreationStrategy<Map>? mapCreationStrategy = null;
        
        if (_width > _fullWidth - 2 || _height > _fullHeight - 2)
        {
            throw new Exception($"MAP CAN'T BE LARGER THAN {_fullWidth - 2}x{_fullHeight - 2} (is {_width}x{_height})");
        }
        if (_kind == ETerrainKind.Ruin)
        {
            mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(_width, _height, a, b, c, Rnd.Instance);
        }
        else
        {
            mapCreationStrategy = new CaveMapCreationStrategy<Map>( _width, _height, a, b, c, Rnd.Instance);
        }

        var inner = Map.Create(mapCreationStrategy);
        _map = Map.Create(new FilledMapCreationStrategy<Map>(_fullWidth, _fullHeight));
        _map.Copy(inner, 1 + Rnd.Instance.Next(0, _fullWidth - 2 - _width), Rnd.Instance.Next(0, 1 + (_fullHeight - 2 - _height)));
        _rendered = false;

        var vas = _map.GetAllCells().Where(t => t.IsWalkable).ToArray();
        vas.Shuffle();
        var vs = vas.AsEnumerable().GetEnumerator();
        if (vs.MoveNext())
        {
            var v = vs.Current;
            _entryPositions.Add((v.X, v.Y));
            var freeTiles = new HashSet<Cell>(_map.GetAdjacentCells(v.X, v.Y).Where(t => t.IsWalkable));
            for (int i = 0; i < 3; i++)
            {
                if (freeTiles.Count == 0)
                {
                    if (vs.MoveNext())
                    {
                        freeTiles.Add(vs.Current);
                    }
                    else
                    {
                        Console.WriteLine("HAD TO REGENERATE FORCEFULLY INNER!");
                        Regenerate(true);
                        return;
                    }
                }
                v = freeTiles.ToArray()[Rnd.Instance.Next(freeTiles.Count)];
                _entryPositions.Add((v.X, v.Y));
                freeTiles.UnionWith(_map.GetAdjacentCells(v.X, v.Y).Where(t => t.IsWalkable));
                freeTiles.RemoveWhere(t => _entryPositions.Contains((t.X, t.Y)));
            }
        }
        else
        {
            Console.WriteLine("HAD TO REGENERATE FORCEFULLY!");
            Regenerate(true);
        }
    }
    
    public CombatMapScreen(SineaterGame game, ETerrainKind kind, int width = -1, int height = -1)
    {
        _width = width;
        _height = height;
        
        _kind = kind;
        
        Initialize(game);
        Regenerate(_width == -1 || _height == -1);
    }

    public void Initialize(SineaterGame game)
    {
        _game = game;
    }

    public void Update(GameTime gameTime)
    {
        if (KB.HasBeenPressed(Keys.D))
        {
            _debugView = !_debugView;
            _rendered = false;
        }
        
        if (KB.HasBeenPressed(Keys.D1))
        {
            this.Regenerate(ETerrainKind.Tomb);
        }
        
        if (KB.HasBeenPressed(Keys.D2))
        {
            this.Regenerate(ETerrainKind.Temple);
        }
        
        if (KB.HasBeenPressed(Keys.D3))
        {
            this.Regenerate(ETerrainKind.Cave);
        }
        
        if (KB.HasBeenPressed(Keys.D4))
        {
            this.Regenerate(ETerrainKind.Clearing);
        }
        
        if (KB.HasBeenPressed(Keys.D5))
        {
            Regenerate(ETerrainKind.Ruin);
        }

        if (KB.HasBeenPressed(Keys.D6))
        {
            Regenerate(ETerrainKind.Unknown);
        }

        if (KB.HasBeenPressed(Keys.Space))
        {
            Regenerate(true);
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (_map == null) return;
        if (_rendered) return;
        
        _rendered = true;
        _game.Layers["mrmo"].Set(1, 1, "                ");
        _game.Layers["mrmo"].Set(1, 1, _kind.ToString());

        for (int i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                _game.Layers["mrmo"].Unset(i + _offsetX, j + _offsetY);
            }
        }

        var shouldDrawAll = _debugView;
        if (!shouldDrawAll) shouldDrawAll = _entryPositions.Count == 0;
        if (!shouldDrawAll)
        {
            var fieldOfView = new FieldOfView(_map);
            ReadOnlyCollection<Cell>? cells = null;
            foreach (var (x, y) in _entryPositions)
            {
                cells = fieldOfView.AppendFov(x, y, 10, true);
            }

            foreach (var cell in cells)
            {
                var (i, j) = (cell.X, cell.Y);
                var g = Glyph.Bw(0, 0);
                if (!cell.IsTransparent)
                {
                    g.U = Rnd.Instance.Next(6, 12);
                    g.V = Rnd.Instance.Next(5, 6);
                    _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, g);
                }
                else
                {
                    _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, ".");
                }
            }
        }
        else
        {
            for (int i = 0; i < _fullWidth; i++)
            {
                for (int j = 0; j < _fullHeight; j++)
                {
                    var g = Glyph.Bw(0, 0);
                    if (!_map[i, j].IsTransparent)
                    {
                        g.U = Rnd.Instance.Next(6, 12);
                        g.V = Rnd.Instance.Next(5, 6);
                    }

                    _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, g);
                }
            }
        }
        
        foreach (var (x, y) in _entryPositions)
        {
            _game.Layers["mrmo"].Set(x + _offsetX, y + _offsetY, new Glyph(11, 25, Color.Black, Color.Yellow));
        }
    }
}