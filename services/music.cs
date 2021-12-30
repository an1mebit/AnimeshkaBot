using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace AnimeshkaBot.services
{
    public sealed class music
    {
        private readonly LavaNode _lavaNode;
        private readonly Dictionary<ulong, TimeSpan> _timeLeft;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;
        private readonly ConcurrentDictionary<ulong, bool> _repeatTokens;

        public music(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
            _timeLeft = new Dictionary<ulong, TimeSpan>();
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _repeatTokens = new ConcurrentDictionary<ulong, bool>();
        }

        public bool Joined(IGuild guild) => _lavaNode.HasPlayer(guild);

        public async Task<string> JoinAsync(IGuild guild, SocketVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            if (_lavaNode.HasPlayer(guild))
            {
                return "Я уже в канала^_^";
            }

            if (voiceChannel is null)
            {
                return "Те нужно зайти в канал<3";
            }

            try
            {
                await log.InfoAsync("Зашла чо");

                await _lavaNode.JoinAsync(voiceChannel, textChannel);
                return $"Зашлаа `{voiceChannel.Name}`.";
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);

                return exception.Message;
            }
        }

        public async Task<Embed> PlayAsync(SocketGuildUser user, IGuild guild, string trackTitle)
        {
            if (user.VoiceChannel is null)
            {
                return await embeded.ErrorEmbed("Нет соединения", "Тебе нужно зайти к нам<3", Color.DarkBlue);
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                return await embeded.ErrorEmbed("Нет соединения", "Я не в канале чо", Color.DarkBlue);
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                SearchResponse results = await _lavaNode.SearchYouTubeAsync(trackTitle);
                if (results.Status.Equals(SearchStatus.LoadFailed) || results.Status.Equals(SearchStatus.NoMatches))
                {
                    return await embeded.ErrorEmbed("Нич не нашлося", $"Нет совпадений с `{trackTitle}`", Color.DarkPurple);
                }

                LavaTrack track = results.Tracks.FirstOrDefault();
                if (track is null)
                {
                    return await embeded.ErrorEmbed("Нич не нашлося", $"Нет совпадений с `{trackTitle}`", Color.DarkPurple);
                }

                if (player.PlayerState is PlayerState.Playing)
                {
                    player.Queue.Enqueue(track);
                    if (_timeLeft.TryGetValue(player.VoiceChannel.Id, out TimeSpan timeLeft))
                    {
                        _timeLeft[player.VoiceChannel.Id] = timeLeft + track.Duration;
                    }

                    return await embeded.Embed("Оно ожидает", track.Title, track.Url, youtube.GetThumbnail(track.Url), player.Queue.Count, $"{track.Duration:hh\\:mm\\:ss}", $"{(timeLeft - player.Track.Position):hh\\:mm\\:ss}", Color.Green);
                }
                else
                {
                    await player.PlayAsync(track);
                    if (!_timeLeft.TryGetValue(player.VoiceChannel.Id, out TimeSpan timeLeft))
                    {
                        timeLeft = track.Duration;
                        _timeLeft.TryAdd(player.VoiceChannel.Id, timeLeft);
                    }
                    else
                    {
                        _timeLeft[player.VoiceChannel.Id] = TimeSpan.Zero;
                    }

                    return await embeded.Embed("Ща играет", track.Title, track.Url, youtube.GetThumbnail(track.Url), player.Queue.Count, $"{track.Duration:hh\\:mm\\:ss}", $"{TimeSpan.Zero:hh\\:mm\\:ss}", Color.Green);
                }
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return await embeded.ErrorEmbed("Что-то не так...", exception.Message, Color.Red);
            }
        }

        public async Task<string> PauseAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я не в канале чо";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player.PlayerState is PlayerState.Playing)
                {
                    await player.PauseAsync();
                    return "Паузаа";
                }

                if (player.PlayerState is PlayerState.Paused)
                {
                    return "Песенка уже остановлена...";
                }

                return "Нечего паузить хехе F9";
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<string> ResumeAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я не в канале...";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player.PlayerState is PlayerState.Paused)
                {
                    await player.ResumeAsync();
                    return "Продолжаем";
                }

                if (player.PlayerState is PlayerState.Playing)
                {
                    return "брух";
                }

                return "Треков нет в плейлисте";
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<string> SkipAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я не в канале";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player is null)
                {
                    return "Де бил?";
                }

                LavaTrack track = player.Track;
                if (!_timeLeft.TryGetValue(player.VoiceChannel.Id, out TimeSpan timeLeft))
                {
                    timeLeft = TimeSpan.Zero;
                    _timeLeft.TryAdd(player.VoiceChannel.Id, timeLeft);
                }
                else
                {
                    _timeLeft[player.VoiceChannel.Id] = timeLeft - player.Track.Duration;
                }

                if (_repeatTokens.TryGetValue(player.VoiceChannel.Id, out bool isRepeat) && isRepeat)
                {
                    _repeatTokens[player.VoiceChannel.Id] = false;
                }

                if (player.Queue.Count == 0 && player.PlayerState == PlayerState.Playing)
                {
                    await player.StopAsync();
                    await log.InfoAsync("Трек пропущен");
                    return $"Трек `{track.Title}` скипнут";
                }

                if (player.Queue.Count == 0)
                {
                    return "Нечего скипать";
                }

                try
                {
                    await player.SkipAsync();
                    await log.InfoAsync("Трек пропущен");
                    return $"Трек `{track.Title}` скипнут";
                }
                catch (Exception exception)
                {
                    await log.ExceptionAsync(exception);
                    return exception.Message;
                }
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<string> LeaveAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я не канале";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player.PlayerState is PlayerState.Playing)
                {
                    await player.StopAsync();
                }

                await _lavaNode.LeaveAsync(player.VoiceChannel);
                return "Я это... дисконнект вообщем";
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<string> LeftAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я не в канале:/";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player is null)
                {
                    return "Де бил?";
                }

                if (player.PlayerState is not PlayerState.Playing)
                {
                    return "Ничо не играет";
                }

                LavaTrack track = player.Track;
                TimeSpan time = track.Duration - player.Track.Position;
                await log.InfoAsync($"время осталось: {time:hh\\:mm\\:ss}");
                return $"время осталось `{time:hh\\:mm\\:ss}`";

            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<Embed> QueueAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return await embeded.ErrorEmbed("Нет соединения", "Я не в канале лмао", Color.DarkBlue);
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player is null)
                {
                    return await embeded.ErrorEmbed("Нет соединения", "Ты кто?", Color.DarkBlue);
                }

                if (player.PlayerState is not PlayerState.Playing)
                {
                    return await embeded.ErrorEmbed("Чото нитак:", "спектакль окончен крч",
                        Color.Red);
                }

                StringBuilder stringBuilder = new();
                if (player.Queue.Count < 1 && player.Track != null)
                {
                    return await NowAsync(guild);
                }

                stringBuilder.Append($"Ща игарет: \n[{player.Track?.Title}]({player.Track?.Url}) | `время: {player.Track?.Duration - player.Track?.Position:hh\\:mm\\:ss}`\n**Список**\n");
                int trackIndex = 1;
                foreach (LavaTrack track in player.Queue)
                {
                    stringBuilder.Append($"`{trackIndex++}` - [**{track.Title}**]({track.Url}) | `время осталось: {track.Duration - track.Position:hh\\:mm\\:ss}`\n");
                }

                return await embeded.QueueEmbed("Оооочередь", stringBuilder.ToString(), Color.DarkGrey);

            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return await embeded.ErrorEmbed("чото нетак", exception.Message, Color.Red);
            }
        }

        public async Task<Embed> NowAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return await embeded.ErrorEmbed("Нет соединения:0", "Я не в канале дурашка", Color.DarkBlue);
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player is null)
                {
                    return await embeded.ErrorEmbed("Нет соединения:0", "Де био?", Color.DarkBlue);
                }

                if (player.PlayerState is not PlayerState.Playing)
                {
                    return await embeded.ErrorEmbed("Нет соединения:0", "Ничо не играет хд",
                        Color.DarkBlue);
                }

                LavaTrack track = player.Track;
                return await embeded.NowEmbed("Ща играет", track.Title, track.Url, track.Author, track.Duration.ToString(), Color.DarkGrey);

            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return await embeded.ErrorEmbed("оааоаооа", exception.Message, Color.Red);
            }
        }

        public async Task<string> SetVolumeAsync(IGuild guild, int volumeValue)
        {
            if (volumeValue is < 0 or > 200)
            {
                return "Твоим ушам пизда \n поставь громкость от 0 до 200";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                await player.UpdateVolumeAsync((ushort)volumeValue);
                await log.InfoAsync($"Громкость {volumeValue}");
                return $"Громкость установлена на {volumeValue}";
            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<string> RepeatAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я всё ещё не в канале...";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player is null)
                {
                    return "Де бил?";
                }

                if (player.PlayerState is not PlayerState.Playing)
                {
                    return "Нет треков шобы повторять";
                }

                if (!_repeatTokens.TryGetValue(player.VoiceChannel.Id, out bool isRepeat))
                {
                    isRepeat = true;
                    _repeatTokens.TryAdd(player.VoiceChannel.Id, true);
                }
                else
                {
                    _repeatTokens.TryUpdate(player.VoiceChannel.Id, !isRepeat, isRepeat);
                    isRepeat = _repeatTokens[player.VoiceChannel.Id];
                }

                return isRepeat ? "Повтор включаен" : "повтор выключен";

            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<string> ClearAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я не в канале дебик";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player is null)
                {
                    return "Де бил?";
                }

                if (player.PlayerState is not PlayerState.Playing || player.Queue.Count <= 0)
                {
                    return "Список пуст";
                }

                player.Queue.Clear();
                _timeLeft.TryGetValue(player.VoiceChannel.Id, out _);
                _timeLeft[player.VoiceChannel.Id] = player.Track.Duration;
                return "Список очистен чо";

            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task<string> ShuffleAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                return "Я не в канале дебик";
            }

            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
                if (player is null)
                {
                    return "Де бил?";
                }

                if (player.Queue.Count <= 0)
                {
                    return "Список пуст";
                }

                player.Queue.Shuffle();
                return "Список заполнен чо";

            }
            catch (Exception exception)
            {
                await log.ExceptionAsync(exception);
                return exception.Message;
            }
        }

        public async Task TrackEnded(TrackEndedEventArgs trackEnded)
        {
            if (trackEnded.Reason != TrackEndReason.Finished && trackEnded.Reason != TrackEndReason.Stopped)
            {
                return;
            }

            LavaPlayer player = trackEnded.Player;
            if (_repeatTokens.TryGetValue(player.VoiceChannel.Id, out bool isRepeat) && isRepeat)
            {
                LavaTrack currentTrack = trackEnded.Track;
                await player.PlayAsync(currentTrack);
                return;
            }

            if (!player.Queue.TryDequeue(out LavaTrack track))
            {
                _ = InitiateDisconnectAsync(player, TimeSpan.FromMinutes(5));
                return;
            }

            if (track is null)
            {
                return;
            }

            _timeLeft[player.VoiceChannel.Id] -= trackEnded.Track.Duration;
            await player.PlayAsync(track);
        }

        public async Task TrackStarted(TrackStartEventArgs trackStarted)
        {
            if (!_disconnectTokens.TryGetValue(trackStarted.Player.VoiceChannel.Id, out CancellationTokenSource value))
            {
                return;
            }

            if (value.IsCancellationRequested)
            {
                return;
            }

            value.Cancel(true);
            await log.InfoAsync("Auto disconnect has been cancelled!");
        }

        public async Task TrackException(TrackExceptionEventArgs arg)
        {
            await log.LogAsync($"Track {arg.Track.Title} threw an exception");
            arg.Player.Queue.Enqueue(arg.Track);
        }

        public async Task TrackStuck(TrackStuckEventArgs arg)
        {
            await log.LogAsync($"Track {arg.Track.Title} got stuck");
            arg.Player.Queue.Enqueue(arg.Track);
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out CancellationTokenSource value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }
            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }

            bool isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
            {
                return;
            }

            await _lavaNode.LeaveAsync(player.VoiceChannel);
        }
    }
}
