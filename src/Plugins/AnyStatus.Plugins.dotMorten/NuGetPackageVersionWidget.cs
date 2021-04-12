﻿/*
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
using AnyStatus.API.Widgets;
using Newtonsoft.Json;

namespace AnyStatus.Plugins.dotMorten
{
    [Category("Morten")]
    [DisplayName("NuGet Package Version")]
    [Description("NuGet package version number")]
    public class NuGetPackageVersionWidget : TextLabelWidget, IPollable, IStandardWidget
    {
        [Order(10)]
        [Required]
        [DisplayName("NuGet Package")]
        [Description("The NuGet package id.")]
        public string PackageId { get; set; }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Text)))
            {
                if (Text != previousText)
                {
                    if (PreviousStatus != null && NotificationsSettings?.IsEnabled == true)
                    {
                        API.Events.WidgetNotifications.PublishAsync(API.Events.StatusChangedNotification.Create(this));
                    }
                    previousText = Text;
                }
                base.OnPropertyChanged(e);
            }
        }
        string previousText;
    }

    public class NuGetPackageQuery : AsyncStatusCheck<NuGetPackageVersionWidget>
    {
        protected override async Task Handle(StatusRequest<NuGetPackageVersionWidget> request, CancellationToken cancellationToken)
        {
            request.Context.NotificationsSettings.IsEnabled = true; // Didn't find a way to set this in the UI, so just force it here
            using HttpClient client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip }, true);
            var oldStatus = request.Context.Status;
            request.Context.Status = Status.Running;
            var uri = "https://api.nuget.org/v3-flatcontainer/" + request.Context.PackageId.ToLowerInvariant() + "/index.json";
            var result = await client.GetStringAsync(uri);
            VersionInfo versionInfo = JsonConvert.DeserializeObject<VersionInfo>(result);
            request.Context.Text = versionInfo.versions.LastOrDefault();
            request.Context.Status = Status.OK;
        }

        private class VersionInfo
        {
            public List<string> versions { get; set; }
        }
    }
}
