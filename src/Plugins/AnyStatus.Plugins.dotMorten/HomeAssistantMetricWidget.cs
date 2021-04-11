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
    [Category("Morten")]
    [DisplayName("Home Assistant - Graph")]
    [Description("Home Assistant Graph")]
    public class HomeAssistantMetricWidget : MetricWidget, IPollable, IStandardWidget, IRequireEndpoint<HomeAssistantEndpoint>
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

    public class HomeAssistantMetricQuery : AsyncMetricQuery<HomeAssistantMetricWidget>, IEndpointHandler<HomeAssistantEndpoint>
    {
        public HomeAssistantEndpoint Endpoint { get; set; }

        protected override async Task Handle(MetricRequest<HomeAssistantMetricWidget> request, CancellationToken cancellationToken)
        {
            request.Context.Status = Status.Running;
            var client = new HAClient(Endpoint.Token, Endpoint.Address);
            var state = await client.GetState(request.Context.EntityId);
            if (double.TryParse(state, out double value))
            {
                request.Context.Value = value;
                request.Context.Status = Status.OK;
            }
            else
            {
                request.Context.Status = Status.Error;
            }
        }
    }
}
