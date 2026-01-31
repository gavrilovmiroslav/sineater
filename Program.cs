using SINEATER.Game;
using System;
using System.IO;
using SINEATER.Tools;

try
{
    using var game = new SineaterGame();
    game.Run();
}
catch (Exception e)
{
    Crash.Report(e);
}