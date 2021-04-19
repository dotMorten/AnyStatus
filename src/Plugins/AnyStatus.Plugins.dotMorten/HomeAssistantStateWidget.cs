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

using AnyStatus.API.Attributes;
using AnyStatus.API.Notifications;
using AnyStatus.API.Widgets;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace AnyStatus.Plugins.dotMorten
{
    [Category("Home Assistant")]
    [DisplayName("Home Assistant - State")]
    [Description("Home Assistant State")]
    public class HomeAssistantStateWidget : TextLabelWidget, IPollable, IStandardWidget, IRequireEndpoint<HomeAssistantEndpoint>
    {
        [Order(10)]
        [DisplayName("Entity ID")]
        public string EntityId { get; set; }

        [Order(20)]
        [Required]
        [EndpointSource]
        [DisplayName("Endpoint")]
        public string EndpointId { get; set; }

        [Order(10)]
        [Required]
        [DisplayName("Enable Notification")]
        [Description("Whether a notification should be made when the state changes.")]
        public bool Notify { get; set; }
    }

    public class HomeAssistantStateQuery : AsyncStatusCheck<HomeAssistantStateWidget>, IEndpointHandler<HomeAssistantEndpoint>
    {
        private readonly INotificationService _notificationService;

        public HomeAssistantStateQuery(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public HomeAssistantEndpoint Endpoint { get; set; }

        protected override async Task Handle(StatusRequest<HomeAssistantStateWidget> request, CancellationToken cancellationToken)
        {
            request.Context.Status = Status.Running;
            var client = new HAClient(Endpoint.Token, Endpoint.Address);
            var state = await client.GetState(request.Context.EntityId);
            string value = state.state;
            if (state.attributes.ContainsKey("device_class"))
            {
                var device_class = state.attributes["device_class"];
                if (device_class.ToString() == "timestamp")
                {                    
                    if (DateTime.TryParse(value, out DateTime date))
                    {
                        var age = DateTime.Now - date;
                        if (age.TotalDays > 1)
                            value = Math.Round(age.TotalDays).ToString() + " days ago";
                        else if (age.TotalHours > 1)
                            value = Math.Round(age.TotalHours).ToString() + " hours ago";
                        else if (age.TotalMinutes > 1)
                            value = Math.Round(age.TotalMinutes).ToString() + " minutes ago";
                        else if (age.TotalSeconds > 1)
                            value = Math.Round(age.TotalSeconds).ToString() + " seconds ago";
                        else 
                            value = age.ToString();
                    }
                }
            }
            if (state.attributes.ContainsKey("unit_of_measurement"))
            {
                value = value + state.attributes["unit_of_measurement"];
            }
            if(request.Context.Notify && request.Context.Text != value)
            {
                _notificationService.Send(new Notification($"State changed from {request.Context.Text} to {value}.", request.Context.Name, NotificationIcon.Info));
            }
            request.Context.Text = value;
            request.Context.Status = Status.OK;
        }
    }
}
