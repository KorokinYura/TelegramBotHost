using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TelegramBotHost.Droid
{
    public class TrackedChannelModel
    {
        public string ChannelId { get; set; }

        //public DateTime LastCheck { get; set; }
        public string LastVideoId { get; set; }

        public long TelegramChatId { get; set; }
    }
}