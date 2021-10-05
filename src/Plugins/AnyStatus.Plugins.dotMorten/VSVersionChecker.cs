using AnyStatus.API.Attributes;
using AnyStatus.API.Notifications;
using AnyStatus.API.Widgets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AnyStatus.Plugins.dotMorten
{
    [Category("Morten")]
    [DisplayName("Visual Studio Version checker")]
    [Description("Notifies when a new version of Visual Studio is available")]
    public class VSVersionCheckerWidget : TextLabelWidget, IPollable, IStandardWidget
    {
        
        [Required]
        [DisplayName("VS Channel")]
        [Description("The VS Channel.")]
        public string VSChannel { get; set; }

        [Order(10)]
        [Required]
        [DisplayName("Enable Notification")]
        [Description("Whether a notification should be made when content changes.")]
        public bool Notify { get; set; } = true;


    }
    public class VSVersionCheckerQuery : AsyncStatusCheck<VSVersionCheckerWidget>
    {
        const string url = "https://aka.ms/vs/channels";


        private readonly INotificationService _notificationService;
        public VSVersionCheckerQuery(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        protected override async Task Handle(StatusRequest<VSVersionCheckerWidget> request, CancellationToken cancellationToken)
        {
            request.Context.NotificationsSettings.IsEnabled = true; // Didn't find a way to set this in the UI, so just force it here
            using HttpClient client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip }, true);
            var oldStatus = request.Context.Status;
            request.Context.Status = Status.Running;
            var text = await client.GetStringAsync(url).ConfigureAwait(false);

            var data = JsonSerializer.Deserialize<Root>(text);
            var channel = data.channels.Where(c => c.channelId == request.Context.VSChannel).FirstOrDefault();
            if(channel is null)
            {
                if (string.IsNullOrEmpty(request.Context.VSChannel))
                    channel = data.channels.First();
                else
                {
                    request.Context.Status = Status.Error;
                }
            }
            if (channel != null && request.Context.Notify && channel.displayVersion != request.Context.Text)
            {
                _notificationService.Send(new Notification("Visual Studio", $"Visual Studio version {channel.displayVersion} available", NotificationIcon.Info));
            }
            request.Context.Text = channel?.displayVersion;
            if (channel != null)
                request.Context.Status = Status.OK;
            client.Dispose();
        }
        public class Root
        {
            public List<Channel> channels { get; set; }
        }

        public class Channel
        {
            public string channelUri { get; set; }
            public string channelId { get; set; }
            public string type { get; set; }
            public string version { get; set; }
            public string displayVersion { get; set; }
        }

    }
}
