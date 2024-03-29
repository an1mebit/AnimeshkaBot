﻿using System;
using System.Threading.Tasks;
using Discord;

namespace AnimeshkaBot.services
{
    public static class embeded
    {
        public static async Task<Embed> Embed(string action, string title, string url, string thumbnailUrl, int position, string duration, string timeLeft, Color color)
        {
            if (timeLeft.Equals(TimeSpan.Zero.ToString()))
            {
                timeLeft = "Now";
            }

            Embed embed = await Task.Run(() => (new EmbedBuilder()
                .WithTitle(action)
                .WithThumbnailUrl(thumbnailUrl)
                .WithDescription($"[**{title}**]({url})")
                .WithColor(color)
                .AddField("Position in queue", position, true)
                .AddField("Duration", duration, true)
                .AddField("Until play", timeLeft, true)
                .WithFooter(new EmbedFooterBuilder().Text = "Animeshka").Build()));

            return embed;
        }

        public static async Task<Embed> QueueEmbed(string title, string description, Color color)
        {
            Embed embed = await Task.Run(() => (new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color).Build()));

            return embed;
        }

        public static async Task<Embed> NowEmbed(string action, string title, string url, string author, string duration, Color color)
        {
            Embed embed = await Task.Run(() => (new EmbedBuilder()
                .WithTitle(action)
                .WithDescription($"[**{title}**]({url})")
                .WithColor(color)
                .AddField("Duration", duration, true)
                .AddField("Author", author, true)
                .WithFooter(new EmbedFooterBuilder().Text = "Animeshka").Build()));

            return embed;
        }

        public static async Task<Embed> ErrorEmbed(string title, string exception, Color color)
        {
            Embed embed = await Task.Run(() => (new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(exception)
                .WithColor(color)
                .WithFooter(new EmbedFooterBuilder().Text = "Animeshka").Build()));

            return embed;
        }
    }
}
