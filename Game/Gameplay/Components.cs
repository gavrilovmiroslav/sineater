using System.Collections.Generic;
using SadRex;

namespace SINEATER.Game.Gameplay;

public readonly record struct WorldMapTile(Cell Cell);
public readonly record struct Position(int X, int Y);

public readonly record struct Dialogue(List<string> Tags, string Text);
public readonly record struct Reward(List<(int, List<Item>)> Rewards);
public readonly record struct Encounter(List<Enemy> Enemies);
