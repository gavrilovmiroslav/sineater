namespace SINEATER;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RogueSharp;

public class DeathsDoor(SineaterGame game, CombatMapScreen level) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        // var layers = SineaterGame.Instance.Layers;
        // var fields = new Dictionary<(int, int), ICharacter>();
        // var enemies = level.Enemies;
        // enemies.Sort((a, b) => a.HP.CompareTo(b.HP));
        // List<Enemy> toRemove = [];
        // for (int x = 0; x < 3; x++)
        // {
        //     foreach (var e in enemies.Where(e => e.LastHit != null && !toRemove.Contains(e)))
        //     {
        //         var r = Rnd.Instance.Next(0, e.AP.Count<StatusWounds>());
        //         if (r > e.HP)
        //         {
        //             e.AP.Reduce<StatusWounds>(r);
        //             level.Party[0].AP.AddN<StatusSin>(e.Sin);
        //             e.Die();
        //
        //             level.DrawCombat();
        //             layers["porsmol"].Clear();
        //             var (i, j) = e.Icon;
        //             var (u, v) = e.DeadIcon;
        //             toRemove.Add(e);
        //
        //             for (int k = 0; k < 5; k++)
        //             {
        //                 layers["mrmo"].Set(e.X, e.Y + 2, new Glyph(u, v, Color.Black, Color.Red));
        //                 yield return new WaitForSeconds(0.01f);
        //                 layers["mrmo"].Set(e.X, e.Y + 2, new Glyph(i, j, Color.Black, Color.Red));
        //                 yield return new WaitForSeconds(0.01f);
        //             }
        //
        //             if (e.LastHit is PartyMember chr)
        //             {
        //                 yield return new ShowPopupWindowWithPortraitAndWaitForKey(e.LastHit.GetPortait(), (_, bnd) =>
        //                 {
        //                     bnd.Newline();
        //                     bnd.Add($"{chr.GetName()} dispatches the {e.GetName()}.");
        //                     bnd.Newline();
        //                     bnd.Add($"  {chr.GetRandomBark()}");
        //                     bnd.Newline();
        //                 }, true);
        //             }
        //             else
        //             {
        //                 yield return new ShowPopupWindowAndWaitForKey((_, bnd) =>
        //                 {
        //                     bnd.Newline();
        //                     bnd.Add($"{e.GetName()} dies.");
        //                     bnd.Newline();
        //                     bnd.Newline();
        //                 }, true);
        //             }
        //
        //             if (e.LastHit is PartyMember c)
        //             {
        //                 var transferable = e.Traits.Where(t => !(t is LimitedTrait)).ToList();
        //                 if (transferable.Count > 0)
        //                 {
        //                     var t = transferable[Rnd.Instance.Next(0, transferable.Count)];
        //                     yield return new ShowPopupWindowWithPortraitAndWaitForKey(c.GetPortait(),
        //                         (_, bnd) => { bnd.Add($"The {e.LastHit.GetName()} acquires {t.Name.ToUpper()}!"); },
        //                         true);
        //                     yield return e.LastHit.AddTrait(t);
        //                 }
        //             }
        //
        //             level.Draw(new GameTime());
        //         }
        //     }
        // }
        //
        // foreach (var e in enemies)
        // {
        //     e.LastHit = null;
        // }
        //
        // foreach (var e in toRemove)
        // {
        //     level.Enemies.Remove(e);
        // }
        //
        // var d = Rnd.Instance.Next(0, level.Party[0].AP.Count<StatusWounds>());
        // if (d > 5)
        // {
        //     level.Party[0].AP.Reduce<StatusWounds>(d);
        //     level.Party[0].AP.AddN<StatusDeath>(1);
        // }
        yield break;
    }
}