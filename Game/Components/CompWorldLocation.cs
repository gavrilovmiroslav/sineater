using System.Collections.Generic;
using SINEATER.Game.Gameplay;

namespace SINEATER.Game.Components;

public record struct CompWorldLocation(int X, int Y);
public readonly record struct CompDialogue(List<string> Tags, string Text);
public readonly record struct CompReward(List<(int, List<Item>)> Rewards);
public readonly record struct CompEncounter(List<Enemy> Enemies);
