using System.Collections;

namespace SINEATER;

public class FlyingObject : IEnumerable
{
    private int _ox, _oy;
    private RangedTargetting _config;
    public FlyingObject(int ox, int oy, RangedTargetting config)
    {
        _ox = ox;
        _oy = oy;
        _config = config;
    }

    public IEnumerator GetEnumerator()
    {
        if (SineaterGame.Instance.ScreenStack.Peek() is CombatMapScreen cmb)
        {
            var l = SineaterGame.Instance.Layers["mrmo"];

            var px = _ox;
            var py = _oy;

            foreach (var (x, y) in Bresenham.Line(_ox, _oy, _config.X, _config.Y))
            {
                cmb.DrawCombat(true);
                l.Set(x, y + 2, _config.Source.GetIcon());
                if (cmb.Map.IsWalkable(x, y))
                {
                    px = x;
                    py = y;
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    _config.X = x;
                    _config.Y = y;
                    break;
                }
            }

            if (_config.Source is IItem item)
            {
                if (item.CanBeShattered())
                {
                    yield return item.ApplyItemShattered(cmb, _config.X, _config.Y);
                }
                else
                {
                    yield return item.ApplyItemLanded(cmb, _config.X, _config.Y);
                }
            }
            else if (_config.Source is Weapon weapon)
            {
                yield return weapon.ApplyItemLanded(cmb, _config.X, _config.Y);
            }
        }
    }
}