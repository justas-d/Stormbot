// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using Discord;

namespace Stormbot.Bot.Core
{
    public static class Logger
    {
        private static readonly object Lock = new object();

        private static void Write(string text, ConsoleColor fore = ConsoleColor.Gray)
        {
            lock (Lock)
            {
                Console.ForegroundColor = fore;
                Console.Write(text);
                Console.ResetColor();
            }
        }

        public static void DiscordLog(LogMessageEventArgs args)
        {
            switch (args.Severity)
            {
                case LogSeverity.Error:
                    FormattedWrite($"{args.Severity}] [{args.Source}", $"{args.Message} Exception: {args.Exception}",
                        ConsoleColor.Red);
                    break;
                case LogSeverity.Warning:
                    FormattedWrite($"{args.Severity}] [{args.Source}", $"{args.Message}",
                        ConsoleColor.Yellow);
                    break;
                case LogSeverity.Info:
                    FormattedWrite($"{args.Severity}] [{args.Source}", $"{args.Message}",
                        ConsoleColor.Blue);
                    break;
            }
        }

        public static void FormattedWrite(string type, string text, ConsoleColor fore = ConsoleColor.Gray)
        {
            WriteTime();
            Write($"[{type}] ", fore);
            Write($"{text}{Environment.NewLine}");
        }

        public static void Writeline(string text) => FormattedWrite("Main", text, ConsoleColor.White);
        private static void WriteTime() => Write($"[{DateTime.Now.ToString("HH:mm:ss")}] ", ConsoleColor.Green);
    }
}