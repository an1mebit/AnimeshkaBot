using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Victoria;
using AnimeshkaBot.services;
using AnimeshkaBot.handlers;

namespace AnimeshkaBot.conf
{
    public sealed class AnimeshkaClient
    {
        private readonly DiscordSocketClient _client;
        private readonly commandhandler _commandHandler;
        private readonly ServiceProvider _services;
        private readonly LavaNode _lavaNode;
        private readonly music _musicService;
        private readonly jsonserv _configService;

        public AnimeshkaClient()
        {
            _services = ConfigureServices();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commandHandler = _services.GetRequiredService<commandhandler>();
            _lavaNode = _services.GetRequiredService<LavaNode>();
            _configService = _services.GetRequiredService<jsonserv>();
            _musicService = _services.GetRequiredService<music>();

            SubscribeEvents();
        }

        public async Task InitializeAsync()
        {
            await _configService.InitializeAsync();

            await _client.LoginAsync(TokenType.Bot, jsonserv.Config.Token);
            await _client.StartAsync();

            await _commandHandler.InitializeAsync();

            await Task.Delay(-1);
        }

        private void SubscribeEvents()
        {
            _client.Ready += ReadyAsync;
            _client.Log += LogAsync;
            _lavaNode.OnLog += LogAsync;
            _lavaNode.OnTrackStarted += _musicService.TrackStarted;
            _lavaNode.OnTrackEnded += _musicService.TrackEnded;
            _lavaNode.OnTrackException += _musicService.TrackException;
            _lavaNode.OnTrackStuck += _musicService.TrackStuck;
        }

        private async Task ReadyAsync()
        {
            try
            {
                await _lavaNode.ConnectAsync();
                await _client.SetGameAsync(jsonserv.Config.GameStatus);
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
            }
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await log.LogAsync(logMessage.Message);
        }


        private static ServiceProvider ConfigureServices() =>
            new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<commandhandler>()
                .AddSingleton<LavaNode>()
                .AddSingleton(new LavaConfig())
                .AddSingleton<music>()
                .AddSingleton<jsonserv>()
                .BuildServiceProvider();
    }
}
