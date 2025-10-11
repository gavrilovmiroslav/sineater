using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RogueSharp;

namespace SINEATER;

public interface IBehavior
{
    public bool ShouldFizzleOut();
    public IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y);
}

public class Behavior : IBehavior
{
    public virtual bool ShouldFizzleOut()
    {
        return false;
    }

    public virtual IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        yield break;
    }
}

public class BehaviorIfWounded(int woundsMin, IBehavior next, IBehavior other) : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        if (self.AP.Count<StatusWounds>() >= woundsMin)
        {
            yield return next.Do(self, level, x, y);
        }
        else
        {
            yield return other.Do(self, level, x, y);
        }
    }
}

public class BehaviorIfNotWounded(int woundsMax, IBehavior next, IBehavior other) : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        if (self.AP.Count<StatusWounds>() < woundsMax)
        {
            yield return next.Do(self, level, x, y);
        }
        else
        {
            yield return other.Do(self, level, x, y);
        }
    }
}

public class BehaviorIfNotInsane(int insanityMax, IBehavior next, IBehavior other) : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        if (self.AP.Count<StatusInsanity>() < insanityMax)
        {
            yield return next.Do(self, level, x, y);
        }
        else
        {
            yield return other.Do(self, level, x, y);
        }
    }
}

public class BehaviorIfInsane(int insanityMin, IBehavior next, IBehavior other) : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        if (self.AP.Count<StatusInsanity>() >= insanityMin)
        {
            yield return next.Do(self, level, x, y);
        }
        else
        {
            yield return other.Do(self, level, x, y);
        }
    }
}

public class BehaviorBlind : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        var dx = Rnd.Instance.Next(-1, 2);
        var dy = dx == 0 ? Rnd.Instance.Next(-1, 2) : 0;
        var nextX = x + dx;
        var nextY = y + dy;
        
        if (level.IsCharacterAt(nextX, nextY) is { } chr)
        {
            yield return level.Attack(self, chr);
        }
        else if (level.IsEnemyAt(nextX, nextY) is { } enm)
        {
            yield return level.Attack(self, enm);
        }
        else if (level.Map.IsWalkable(nextX, nextY))
        {
            yield return self.MoveTo(level, nextX, nextY, x, y);
            if (level.IsInActivePartyMemberFOV?.Contains((nextX, nextY)) ?? false)
            {
                yield return new WaitForSeconds(0.01f);
            }
        }
    }

    public override bool ShouldFizzleOut()
    {
        return true;
    }
}

public class BehaviorFlyAbout : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        var ap = self.Stats.Will;
        for (var i = 0; i < ap; i++)
        {
            if (self.IsDone) yield break;
            
            var dx = Rnd.Instance.Next(-1, 2);
            var dy = dx == 0 ? Rnd.Instance.Next(-1, 2) : 0;
            var nextX = x + dx;
            var nextY = y + dy;
            if (level.IsCharacterAt(nextX, nextY) is { } chr)
            {
                yield return level.Attack(self, chr);
                break;
            }
            else if (level.IsEnemyAt(nextX, nextY) is { } enm)
            {
                continue;
            }
            else if (level.Map.IsWalkable(nextX, nextY))
            {
                yield return self.MoveTo(level, nextX, nextY, x, y);
                yield return new WaitForSeconds(0.01f);
            }
        }
    }
}

public class BehaviorAggro : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        var gm = new GoalMap<Cell>(level.Map, false);
        foreach (var (ch, cs) in level.CombatStates)
        {
            gm.AddGoal(cs.X, cs.Y, ch.Stats.Vigor);
        }
        
        gm.ClearObstacles();
        foreach (var e in level.Enemies.Where(e => e != self))
        {
            gm.AddObstacle(e.X, e.Y);
        }
        var path = gm.TryFindPath(self.X, self.Y);

        if (path != null)
        {
            int ap = self.Stats.Will;
            for (var i = 0; i < ap; i++)
            {
                if (self.IsDone) yield break;
                
                var next = path.TryStepForward();
                if (next == null) continue;

                var ox = self.X;
                var oy = self.Y;
                if (level.IsCharacterAt(next.X, next.Y) is {} chr)
                {
                    var (ex, ey) = self.Icon;
                    var (cx, cy) = chr.Job.GetImage();
                    for (int f = 0; f < 10; f++)
                    {
                        SineaterGame.Instance.Layers["mrmo"].Set(self.X, self.Y + 2,
                            new Glyph(ex, ey, Color.Black, f % 2 == 0 ? Color.Red : self.Tint));
                        SineaterGame.Instance.Layers["mrmo"].Set(next.X, next.Y + 2,
                            new Glyph(cx, cy, Color.Black, f % 2 == 1 ? Color.Red : chr.Tint));
                        yield return new WaitForSeconds(0.01f);
                    }
                    level.DrawCombat();
                    yield return level.Attack(self, chr);
                    ap = 0;
                }
                else
                {
                    yield return self.MoveTo(level, next.X, next.Y, ox, oy);
                    ap -= 1;
                }

                level.DrawGui();
                level.DrawCombat();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            Console.WriteLine("NO PATH!");
        }
    }
}

public class BehaviorGoTo(int gx, int gy) : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        var gm = new GoalMap<Cell>(level.Map, false);
        gm.AddGoal(gx, gy, 100);
        var ox = self.X;
        var oy = self.Y;
        
        gm.ClearObstacles();
        foreach (var e in level.Enemies.Where(e => e != self))
        {
            gm.AddObstacle(e.X, e.Y);
        }
        foreach (var e in SineaterGame.Instance.Party.Characters)
        {
            var cs = level.CombatStates[e];
            gm.AddObstacle(cs.X, cs.Y);
        }

        Path? path = null;

        try
        {
            path = gm.TryFindPath(self.X, self.Y);
        }
        catch
        {
            path = null;
        }

        if (path != null)
        {
            for (var i = 0; i < self.Stats.Will; i++)
            {
                if (self.IsDone) yield break;
                
                var next = path.TryStepForward();
                if (next == null) yield break;

                if (level.Map?.IsWalkable(next.X, next.Y) ?? false)
                {
                    yield return self.MoveTo(level, next.X, next.Y, ox, oy);
                    if (level.IsInActivePartyMemberFOV?.Contains((next.X, next.Y)) ?? false)
                    {
                        yield return new WaitForSeconds(0.05f);
                    }
                }
            }
        }
    }
}

public class BehaviorYearnForLight : Behavior
{
    public override bool ShouldFizzleOut()
    {
        return true;
    }

    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        Console.WriteLine($"{self.Name.ToUpper()} YEARNS FOR THE LIGHT!");
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        List<(int, int)> stars = [];
        if (level.Domains.Tiles.ContainsKey((x, y)))
        {
            var domain = level.Domains.Tiles[(x, y)];
            foreach (var tile in level.Map?.GetCellsInCircle(x, y, self.Stats.Clarity)
             .Where(t =>
             {
                 if (level.Domains.Tiles.TryGetValue((t.X, t.Y), out var o))
                 {
                     return o == domain;
                 }

                 return false;
             }) ?? [])
            {
                var uv = mrmo.GetUV(tile.X, tile.Y);
                if (uv.HasValue && uv.Value.Item2 != 0)
                {
                    stars.Add(uv.Value);
                }
            }
        }
        else
        {
            yield return new BehaviorBlind().Do(self, level, x, y);
            yield break;
        }

        var xy = new Vector2(x, y);
        stars.Sort((a, b) =>
        {
            var da = Vector2.Distance(new Vector2(a.Item1, a.Item2), xy);
            var db = Vector2.Distance(new Vector2(b.Item1, b.Item2), xy);
            return da.CompareTo(db);
        });

        var (sx, sy) = stars[0];
        yield return new BehaviorGoTo(sx, sy).Do(self, level, x, y);
    }
}

public class BehaviorThrowHealing : Behavior
{
    public override IEnumerable Do(Enemy self, CombatMapScreen level, int x, int y)
    {
        var ex = 0;
        var ey = 0;
        foreach (var e in level.Enemies)
        {
            ex += e.X;
            ey += e.Y;
        }
        ex /= level.Enemies.Count;
        ey /= level.Enemies.Count;
        
        if (Vector2.Distance(new Vector2(ex, ey), new Vector2(x, y)) <= self.Stats.Vigor)
        {
            if (Rnd.Instance.D100 < 80)
            {
                var enm = level.Enemies[Rnd.Instance.Next(0, level.Enemies.Count)];
                yield return new FlyingObject(x, y, new()
                {
                    Source = new PotionBloodReliquary(),
                    Owner = self,
                    X = enm.X + Rnd.Instance.Next(-1, 2),
                    Y = enm.Y + Rnd.Instance.Next(-1, 2)
                });
            }
            else
            {
                yield return new FlyingObject(x, y, new()
                    {
                        Source = new PotionBloodReliquary(),
                        Owner = self,
                        X = ex + Rnd.Instance.Next(-1, 2),
                        Y = ey + Rnd.Instance.Next(-1, 2)
                    });
            }
        }
        else
        {
            yield return new BehaviorGoTo(ex, ey).Do(self, level, x, y);
        }
    }
}