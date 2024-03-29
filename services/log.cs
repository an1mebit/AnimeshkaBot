﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimeshkaBot.services
{
    public static class log
    {
        public static async Task LogAsync(string logMessage, string state = "log")
        {
            await Write($"{Tag(state)} {logMessage}");
        }

        public static async Task InfoAsync(string info)
        {
            await LogAsync(info, "info");
        }

        public static async Task ExceptionAsync(Exception exception)
        {
            await LogAsync(exception.Message, "exception");
        }

        private static Task Write(string info)
        {
            Console.WriteLine(info);

            return Task.CompletedTask;
        }

        private static string Tag(string sender)
        {
            return sender switch
            {
                "log" => "[LOGM]",
                "info" => "[INFO]",
                "exception" => "[EXCP]",
                _ => "[EROR]",
            };
        }
    }
}
