/*
 * Home Assistant Plugin
 *
 * Copyright (c) Morten Nielsen - https://xaml.dev
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AnyStatus.API.Attributes;
using AnyStatus.API.Events;
using AnyStatus.API.Notifications;
using AnyStatus.API.Widgets;
using Newtonsoft.Json;

namespace AnyStatus.Plugins.dotMorten
{
    [Category("Morten")]
    [DisplayName("NuGet Package Version")]
    [Description("NuGet package version number")]
    public class NuGetPackageVersionWidget : TextWidget, IPollable, ICommonWidget
    {
        [Order(10)]
        [Required]
        [DisplayName("NuGet Package")]
        [Description("The NuGet package id.")]
        public string PackageId { get; set; }

        [Order(10)]
        [Required]
        [DisplayName("Enable Notification")]
        [Description("Whether a notification should be made when version changes.")]
        public bool Notify { get; set; } = true;
    }

    public class NuGetPackageQuery : AsyncStatusCheck<NuGetPackageVersionWidget>
    {
        private readonly INotificationService _notificationService;
        public NuGetPackageQuery(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        protected override async Task Handle(StatusRequest<NuGetPackageVersionWidget> request, CancellationToken cancellationToken)
        {
            //request.Context.NotificationsSettings.IsEnabled = true; // Didn't find a way to set this in the UI, so just force it here
            using HttpClient client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip }, true);
            var oldStatus = request.Context.Status;
            request.Context.Status = Status.Running;
            var uri = "https://api.nuget.org/v3-flatcontainer/" + request.Context.PackageId.ToLowerInvariant() + "/index.json";
            var result = await client.GetStringAsync(uri);
            VersionInfo versionInfo = JsonConvert.DeserializeObject<VersionInfo>(result);
            var text = versionInfo.versions.LastOrDefault();
            if (request.Context.Notify && text != request.Context.Text)
            {
                _notificationService.Send(new Notification($"NuGet '{request.Context.PackageId}' version changed from {request.Context.Text} to {text}.", $"NuGet Package Version Update", NotificationIcon.Info));
                //WidgetNotifications.PublishAsync(StatusChangedNotification.Create(this));
            }
            request.Context.Text = text;
            request.Context.Status = Status.OK;
        }

        private class VersionInfo
        {
            public List<string> versions { get; set; }
        }
    }
}
