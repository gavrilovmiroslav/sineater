using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RogueSharp;

namespace SINEATER;

public class Frenzy(SineaterGame game, CombatMapScreen level) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var fields = new Dictionary<(int, int), ICharacter>();
        // foreach (var e in level.Enemies)
        // {
        //     fields.Add((e.X, e.Y), e);
        // }
        //
        // foreach (var ch in level.Party)
        // {
        //     var x = ch.X;
        //     var y = ch.Y;
        //     if (!fields.ContainsKey((x, y)))
        //         fields.Add((x, y), ch);
        // }

        List<ICharacter> chars = [];
        chars.AddRange(game.Party.Characters);
        //chars.AddRange(level.Enemies);
        foreach (var chr in chars)
        {
            var insanity = chr.GetAP().Count(EStatus.Insanity);

            if (Rnd.Instance.Next(0, insanity) > chr.Stats.Clarity)
            {
                var x = 0;
                var y = 0;

                if (chr is PartyMember c)
                {
                    x = c.X;
                    y = c.Y;
                } 
                else if (chr is Enemy e)
                {
                    x = e.X;
                    y = e.Y;
                }

                var letters = "!@#$%^&*+!";
                for (int i = 0; i < 10; i++)
                {
                    game.Layers["mrmo"].Set(x, y + 2, letters[i].ToString(), Color.Yellow);
                    yield return new WaitForSeconds((10 - i) * 0.01f);
                }
                
                // FRENZY!
                var dst = Math.Max(2, 4 + chr.Stats.Mod(EStat.Clarity));
                var edge = level.Map.GetBorderCellsInCircle(x, y, dst).ToList();
                edge.Shuffle();
                
                var goals = new GoalMap<Cell>(level.Map, true);
                var pathCount = edge.Count;
                List<Path> paths = [];
                List<int> pause = [];
                for (var i = 0; i < pathCount; i++)
                {
                    goals.ClearGoals();
                    if (!level.Map.IsWalkable(edge[i].X, edge[i].Y))
                    {
                        continue;
                    }

                    if (Rnd.Instance.Next(0, 6) < 2)
                        continue;
                    
                    goals.AddGoal(edge[i].X, edge[i].Y, 100);
                    goals.ClearObstacles();

                    var path = goals.TryFindPath(x, y);
                    if (path != null)
                    {
                        paths.Add(path);
                        pause.Add(Rnd.Instance.Next(0, 3));
                    }
                }

                while (true)
                {
                    bool anyPathTaken = false;
                    level.DrawCombat();
                    for (int ip = paths.Count - 1; ip >= 0; ip--)
                    {
                        if (pause[ip] > 0)
                        {
                            pause[ip] -= 1;
                            continue;
                        }

                        HashSet<Cell> light = [];
                        var p = paths[ip];
                        var cell = p.TryStepForward();
                        
                        if (cell != null)
                        {
                            var dist = Vector2.Distance(new Vector2(x, y), new Vector2(cell.X, cell.Y)) / dst;
                            game.Layers["mrmo"].Set(cell.X, cell.Y + 2, "z",
                                Color.Lerp(Color.Yellow, Color.Red, dist));
                            var adjacent = level.Map.GetAdjacentCells(cell.X, cell.Y, true);
                            if (adjacent != null) foreach (var a in adjacent) light.Add(a);

                            if (fields.ContainsKey((cell.X, cell.Y)))
                            {
                                fields[(cell.X, cell.Y)].GetAP().Reduce(EStatus.Insanity, 1);
                                fields[(cell.X, cell.Y)].GetAP().Add(EStatus.Wound, 1);
                            }
                            anyPathTaken = true;

                            foreach (var l in light)
                            {
                                game.Layers["mrmo"].Set(l.X, l.Y + 2, Color.Lerp(Color.Yellow, Color.Red, dist), Color.Red);
                                game.Layers["mrmo"].Lighten(l.X, l.Y + 2, 0.25f);
                            }
                        }
                    }
                    yield return new WaitForSeconds(0.05f);
                    if (!anyPathTaken) break;
                }

                // if (!chr.GetTraits().Any(t => t is TraitFrenzied))
                // {
                //     yield return chr.AddTrait(new TraitFrenzied(5));
                // }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}