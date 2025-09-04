using System.Timers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SINEATER;

public enum EFocus
{
    Stay, In, Out
}

public class Focus
{
    private readonly Effect _crt;
    private readonly float _inSpeed;
    private readonly float _outSpeed;
    private readonly Timer _timer;
    
    public Focus(Effect crt, float inSpeed = 0.0001f, float outSpeed = -0.02f, int interval = 1000 * 60)
    {
        _crt = crt;
        _inSpeed = inSpeed;
        _outSpeed = outSpeed;
        
        _timer = new Timer();
        _timer.Interval = interval;
        _timer.AutoReset = false;
        _timer.Elapsed += (sender, args) =>
        {
            if (!_contact)
            {
                FocusOut();
            }

            _contact = false;
            _timer.Interval = interval;
        };
        _timer.Start();
    }
    
    private bool _contact = false;
    private EFocus _state = EFocus.Stay;

    private float _focus = 0.0f;

    static float Lerp(float a, float b, float t) =>  a + (b - a) * t;

    private void CrtFocus(float f)
    {
        _crt.Parameters["warpX"]?.SetValue(Lerp(0.05f, 0.0f, f));
        _crt.Parameters["warpY"]?.SetValue(Lerp(0.07f, 0.0f, f));
        _crt.Parameters["hardBloomScan"]?.SetValue(Lerp(-1.5f, 0.0f, f));
        _crt.Parameters["bloomAmount"]?.SetValue(Lerp(0.15f, 0.0f, f));
        _crt.Parameters["hardScan"]?.SetValue(Lerp(-5.0f, 0.0f, f));
    }

    public void FocusIn()
    {
        _state = EFocus.In;
    }
    
    public void FocusOut()
    {
        _state = EFocus.Out;
    }

    public float Get() => _focus;
    
    public void Update()
    {
        _contact |= Keyboard.GetState().GetPressedKeys().Length > 0;
        if (_contact) FocusIn();

        if (_state == EFocus.Stay) return;
        if (_state == EFocus.In)
        {
            _focus += _inSpeed;
            if (_focus >= 1.0f)
            {
                _focus = 1.0f;
                _state = EFocus.Stay;
            }
            CrtFocus(_focus);
        }
        else if (_state == EFocus.Out)
        {
            _focus += _outSpeed;
            if (_focus <= 0.0f)
            {
                _focus = 0.0f;
                _state = EFocus.Stay;
            }
            CrtFocus(_focus);
        }
    }
}