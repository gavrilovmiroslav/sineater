using System;
using System.IO;

namespace SINEATER.Tools;

public static class Crash
{
    public static void Report(Exception e)
    {
        if (!Directory.Exists("Crash"))
        {
            Directory.CreateDirectory("Crash");
        }

        var timeInfo = DateTime.Now;
        var fileName = $"Crash\\crash_{timeInfo.Day}-{timeInfo.Month}-{timeInfo.Year}-{timeInfo.Hour}-{timeInfo.Minute}.txt";
        using StreamWriter w = new StreamWriter(fileName);

        w.Write(e.StackTrace);
        w.Flush();
        w.Close();

        Environment.Exit(1);
    }
}