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
            request.Context.Text = state.state;
            request.Context.Status = Status.OK;
        }
    }
}
