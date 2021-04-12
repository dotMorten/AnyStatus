using AnyStatus.API.Attributes;
using AnyStatus.API.Widgets;
using MediatR;
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
    }

    public class HomeAssistantStateQuery : AsyncStatusCheck<HomeAssistantStateWidget>, IEndpointHandler<HomeAssistantEndpoint>
    {
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
                value = value + " " + state.attributes["unit_of_measurement"];
            }
            request.Context.Text = value;
            request.Context.Status = Status.OK;
        }
    }
}
