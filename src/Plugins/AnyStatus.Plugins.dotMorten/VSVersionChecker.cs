using AnyStatus.API.Attributes;
using AnyStatus.API.Notifications;
using AnyStatus.API.Widgets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
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
    public class VSVersionCheckerWidget : TextWidget, IPollable, ICommonWidget
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

        [Browsable(false)]
        [JsonIgnore]
        public string Tag1 { get; set; }
        [Browsable(false)]
        [JsonIgnore]
        public string Tag2 { get; set; }
        [Browsable(false)]
        [JsonIgnore]
        public string channelUri;
        [Browsable(false)]
        [JsonIgnore]
        public VSVersionCheckerQuery.Root Root1 { get; set; }
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
            //request.Context.NotificationsSettings.IsEnabled = true; // Didn't find a way to set this in the UI, so just force it here
            using HttpClient client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip }, true);
            var oldStatus = request.Context.Status;
            request.Context.Status = Status.Running;
            var req1 = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(request.Context.Tag1))
                req1.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(request.Context.Tag1));
            var textResponse = await client.SendAsync(req1).ConfigureAwait(false);
            if (textResponse.StatusCode != HttpStatusCode.NotModified)
            {
                request.Context.Tag1 = textResponse.Headers.ETag?.Tag;
                var text = await textResponse.Content.ReadAsStringAsync();

                request.Context.Root1 = System.Text.Json.JsonSerializer.Deserialize<Root>(text);
            }
            var channel = request.Context.Root1.channels.Where(c => c.channelId == request.Context.VSChannel).FirstOrDefault();
            string version = "";
            if(channel is null)
            {
                if (string.IsNullOrEmpty(request.Context.VSChannel))
                    channel = request.Context.Root1.channels.First();
            }
            if (channel != null)
            {
                version = channel.displayVersion;
                var req2 = new HttpRequestMessage(HttpMethod.Get, channel.channelUri);
                if (!string.IsNullOrEmpty(request.Context.Tag2))
                    req2.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(request.Context.Tag2));


                var text2Response = await client.SendAsync(req2).ConfigureAwait(false);
                if (text2Response.StatusCode == HttpStatusCode.NotModified)
                {
                    request.Context.Status = Status.OK;
                    return;
                }
                request.Context.Tag2 = text2Response.Headers.ETag?.Tag;
                var text2 = await text2Response.Content.ReadAsStringAsync();
                var root2 = System.Text.Json.JsonSerializer.Deserialize<Root2>(text2);
                version = root2.info?.productDisplayVersion ?? version;
            }
            if (!string.IsNullOrEmpty(version) && request.Context.Notify && version != request.Context.Text)
            {
                _notificationService.Send(new Notification("Visual Studio", $"Visual Studio version {version} available", NotificationIcon.Info));
            }
            request.Context.Text = version;
            if (!string.IsNullOrEmpty(version))
                request.Context.Status = Status.OK;
            else
                request.Context.Status = Status.Error;
            client.Dispose();
        }
        public class Root2
        {
            public Info info { get; set; }
        }
        public class Info
        {
            public string productDisplayVersion { get; set; }
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
