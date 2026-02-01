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
        var fileName = $"Crash\\crash_{timeInfo.Year}-{timeInfo.Month}-{timeInfo.Day}-{timeInfo.Hour}h_{timeInfo.Minute}min.txt";
        using StreamWriter w = new StreamWriter(fileName);

        w.WriteLine(e.Source);
        w.WriteLine(e.Message);
        w.Write(e.StackTrace);
        w.Flush();
        w.Close();

        Environment.Exit(1);
    }
}