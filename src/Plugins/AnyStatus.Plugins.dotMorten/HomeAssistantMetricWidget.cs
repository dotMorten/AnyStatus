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

using AnyStatus.API.Attributes;
using AnyStatus.API.Widgets;
using Newtonsoft.Json;
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
                return base.ToString() + _unit;
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