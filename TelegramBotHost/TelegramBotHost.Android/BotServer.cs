using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Android.Content;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Xamarin.Essentials;
using Message = Telegram.Bot.Types.Message;

namespace TelegramBotHost.Droid
{
    [BroadcastReceiver]
    public class BotServer : BroadcastReceiver
    {
        // GET https://www.googleapis.com/youtube/v3/search?part=snippet&channelId=UC0fW0JbGMFvqYOY3V6p-KRg&maxResults=1&order=date&type=video&key=AIzaSyBAqAfCmWExFtq8YRVRKJjHI03IKTAvyGU

        private const string OffsetString = "offset";
        private const string ChannelsString = "channels";

        private const string Token = "782581289:AAG3Ti-BqmFol4fksF-YBeQtz0fcQ_5yJ0o";   // @EzhiSarmatMargo_Bot
        private const string YouTubeApiKey = "AIzaSyBAqAfCmWExFtq8YRVRKJjHI03IKTAvyGU";
        private static TelegramBotClient bot;

        private static long chatId = 0;

        static BotServer()
        {
            bot = new TelegramBotClient(Token);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                HandleTelegramBot();
            }
            catch (Exception ex)
            {
                bot.SendTextMessageAsync(chatId, ex.ToString());
            }
        }

        private static void HandleTelegramBot()
        {
            var offset = Preferences.Get(OffsetString, 0);

            Update[] updates;

            try
            {
                updates = bot.GetUpdatesAsync(offset).GetAwaiter().GetResult();
            }
            catch
            {
                return;
            }

            foreach (var update in updates)
            {
                var message = update.Message;
                if (message == null)
                {
                    message = update.ChannelPost;
                }

                switch (message.Type)
                {
                    case Telegram.Bot.Types.Enums.MessageType.Text:
                        HandleMessage(message);
                        break;
                }

                offset = update.Id + 1;
                Preferences.Set(OffsetString, offset);
            }

            CheckChannelsForUpdates();
        }

        private static void HandleMessage(Message message)
        {
            chatId = message.Chat.Id;

            var words = message.Text.Split(' ');
            switch (words[0].ToLower())
            {
                case "hi":
                    bot.SendTextMessageAsync(message.Chat.Id, "Hello", replyToMessageId: message.MessageId);
                    break;
                case "guid":
                    bot.SendTextMessageAsync(message.Chat.Id, Guid.NewGuid().ToString(), replyToMessageId: message.MessageId);
                    break;
                case "ежи":
                    HandleYouTubeQuery(message);
                    break;
                case "help":
                    bot.SendTextMessageAsync(
                        message.Chat.Id,
                        "'ежи' - видосы с Маргинальных Хайлайтов за последние сутки.\n" +
                        "'add <youTubeChannekId>' - добавить канал в список отслеживаемых.\n" +
                        "'remove <youTubeChannekId>' - удалить канал из списка отслеживаемых.\n" +
                        "'channels' - показать список отслеживаемых каналов.",
                        replyToMessageId: message.MessageId);
                    break;
                case "add":
                    AddChannel(words[1], message.Chat.Id);
                    break;
                case "remove":
                    RemoveChannel(words[1], message.Chat.Id);
                    break;
                case "channels":
                    var jsonChannels = Preferences.Get(ChannelsString, "");
                    var trackedChannels = JsonConvert.DeserializeObject<List<TrackedChannelModel>>(jsonChannels);

                    var idList = trackedChannels?.Where(ch => ch.TelegramChatId == message.Chat.Id)
                        .Select(ch => ch.ChannelId).ToList();

                    if (trackedChannels == null || trackedChannels.Count == 0 || idList == null || idList.Count == 0)
                    {
                        bot.SendTextMessageAsync(message.Chat.Id, "List of channels is empty");
                        break;
                    }

                    bot.SendTextMessageAsync(message.Chat.Id, string.Join(",\n", idList), replyToMessageId: message.MessageId);
                    break;
                default:
                    bot.SendTextMessageAsync(message.Chat.Id, "\nWrite \"help\"", replyToMessageId: message.MessageId);
                    break;
            }
        }

        // Gets last day videos from YouTube Channel
        private static void HandleYouTubeQuery(Message message)
        {
            bot.SendTextMessageAsync(message.Chat.Id, "TBD", replyToMessageId: message.MessageId);
        }

        private static void CheckChannelsForUpdates()
        {
            var jsonChannels = Preferences.Get(ChannelsString, "");
            var trackedChannels = JsonConvert.DeserializeObject<List<TrackedChannelModel>>(jsonChannels);

            if (trackedChannels == null)
            {
                trackedChannels = new List<TrackedChannelModel>();
            }

            foreach (var channel in trackedChannels)
            {
                WebRequest request = WebRequest.Create($"https://www.youtube.com/channel/{channel.ChannelId}/videos");
                WebResponse response = request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var html = reader.ReadToEnd();
                        var videoId = html.Substring(html.IndexOf("/watch?v="), 20);

                        if (channel.LastVideoId != videoId)
                        {
                            bot.SendTextMessageAsync(channel.TelegramChatId, $"https://www.youtube.com/{videoId}");
                            channel.LastVideoId = videoId;
                        }
                    }
                }
            }

            jsonChannels = JsonConvert.SerializeObject(trackedChannels);
            Preferences.Set(ChannelsString, jsonChannels);
        }

        private static void AddChannel(string channelId, long chatId)
        {
            var jsonChannels = Preferences.Get(ChannelsString, "");
            var trackedChannels = JsonConvert.DeserializeObject<List<TrackedChannelModel>>(jsonChannels);

            if (trackedChannels == null)
            {
                trackedChannels = new List<TrackedChannelModel>();
            }

            var isAlreadyTracked = trackedChannels.Any(ch => ch.ChannelId == channelId && ch.TelegramChatId == chatId);

            if (!isAlreadyTracked)
            {
                trackedChannels.Add(
                   new TrackedChannelModel
                   {
                       ChannelId = channelId,
                       LastVideoId = string.Empty,
                       TelegramChatId = chatId
                   });

                bot.SendTextMessageAsync(chatId, channelId + " was successfully added to channel list.");
            }
            else
            {
                bot.SendTextMessageAsync(chatId, channelId + " is already exists in the channel list.");
            }

            jsonChannels = JsonConvert.SerializeObject(trackedChannels);
            Preferences.Set(ChannelsString, jsonChannels);
        }

        private static void RemoveChannel(string channelId, long chatId)
        {
            var jsonChannels = Preferences.Get(ChannelsString, "");
            var trackedChannels = JsonConvert.DeserializeObject<List<TrackedChannelModel>>(jsonChannels);

            if (trackedChannels == null)
            {
                trackedChannels = new List<TrackedChannelModel>();
            }

            var isRemoved = trackedChannels.Remove(
                trackedChannels.FirstOrDefault(ch => ch.ChannelId == channelId && ch.TelegramChatId == chatId));

            if (isRemoved)
            {
                bot.SendTextMessageAsync(chatId, channelId + " was successfully removed from channel list.");
            }
            else
            {
                bot.SendTextMessageAsync(chatId, channelId + " wasn't found in channel list.\nPlease check the channel id spelling.");
            }

            jsonChannels = JsonConvert.SerializeObject(trackedChannels);
            Preferences.Set(ChannelsString, jsonChannels);
        }
    }
}