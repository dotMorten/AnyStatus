using AnyStatus.API.Attributes;
using AnyStatus.API.Widgets;
using MediatR;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace AnyStatus.Plugins.dotMorten
{
    [Category("Home Assistant")]
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


        private string _unit;

        [JsonIgnore]
        [Browsable(false)]
        public string Unit
        {
            get => _unit;
            set => Set(ref _unit, value);
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Unit))
                return base.ToString() + " " + _unit;
            return base.ToString();
        }
    }

    public class HomeAssistantMetricQuery : AsyncMetricQuery<HomeAssistantMetricWidget>, IEndpointHandler<HomeAssistantEndpoint>
    {
        public HomeAssistantEndpoint Endpoint { get; set; }

        protected override async Task Handle(MetricRequest<HomeAssistantMetricWidget> request, CancellationToken cancellationToken)
        {
            request.Context.Status = Status.Running;
            var client = new HAClient(Endpoint.Token, Endpoint.Address);
            var state = await client.GetState(request.Context.EntityId);
            if (double.TryParse(state.state, out double value))
            {
                request.Context.Value = value;
                request.Context.Status = Status.OK;
                if (state.attributes.ContainsKey("unit_of_measurement"))
                    request.Context.Unit = state.attributes["unit_of_measurement"]?.ToString();
                else
                    request.Context.Unit = null;
            }
            else
            {
                request.Context.Status = Status.Error;
            }
        }
    }
}
