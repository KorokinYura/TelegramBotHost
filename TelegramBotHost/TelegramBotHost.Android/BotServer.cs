using System;
using Android.Content;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Telegram.Bot;
using Telegram.Bot.Types;
using Xamarin.Essentials;
using Message = Telegram.Bot.Types.Message;

namespace TelegramBotHost.Droid
{
    [BroadcastReceiver]
    public class BotServer : BroadcastReceiver
    {
        private const string OffsetString = "offset";

        private const string Token = "782581289:AAG3Ti-BqmFol4fksF-YBeQtz0fcQ_5yJ0o";   // @EzhiSarmatMargo_Bot
        private const string YouTubeApiKey = "AIzaSyBAqAfCmWExFtq8YRVRKJjHI03IKTAvyGU";
        private static TelegramBotClient bot;

        static BotServer()
        {
            bot = new TelegramBotClient(Token);
        }

        public override void OnReceive(Context context, Intent intent)
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
        }

        private static void HandleMessage(Message message)
        {
            switch (message.Text.ToLower())
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
                    bot.SendTextMessageAsync(message.Chat.Id, "Пиши 'ежи'.", replyToMessageId: message.MessageId);
                    break;
                default:
                    bot.SendTextMessageAsync(message.Chat.Id, "Message: " + message.Text + "\nWrite \"help\"", replyToMessageId: message.MessageId);
                    break;
            }
        }

        // Gets last day videos from YouTube Channel
        private static void HandleYouTubeQuery(Message message)
        {
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = YouTubeApiKey });

            var searchListRequest = yt.Search.List("snippet");
            searchListRequest.ChannelId = "UC0fW0JbGMFvqYOY3V6p-KRg";   // "Маргинальные Хайлайты"
            searchListRequest.PublishedAfter = DateTime.Now.AddDays(-1);
            var searchListResult = searchListRequest.Execute();

            var retString = "";

            foreach (var item in searchListResult.Items)
            {
                retString += "\n\nName: " + item.Snippet.Title.Replace("&quot;", "\"") + "\n" + "Url: https://www.youtube.com/watch?v=" + item.Id.VideoId;
            }


            bot.SendTextMessageAsync(message.Chat.Id, retString, replyToMessageId: message.MessageId);
        }
    }
}