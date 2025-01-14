﻿/*
 * Home Assistant Client
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnyStatus.Plugins.dotMorten
{
    internal class HAClient
    {
        string token;
        string hostname;
        HttpClient client = new HttpClient();
        public HAClient(string token, string hostname)
        {
            this.token = token;
            this.hostname = hostname;
        }
        /// <summary>
        /// Returns the current configuration
        /// </summary>
        /// <returns></returns>
        public Task<string> GetConfig() => RequestAsync("config");
        /// <summary>
        /// Returns basic information about the Home Assistant instance
        /// </summary>
        /// <returns></returns>
        public Task<string> GetDiscoveryInfo() => RequestAsync("discovery_info");
        /// <summary>
        /// Returns an array of event objects. Each event object contains event name and listener count.
        /// </summary>
        /// <returns></returns>
        public Task<string> GetEvents() => RequestAsync("events");
        /// <summary>
        /// Returns an array of service objects. Each object contains the domain and which services it contains.
        /// </summary>
        /// <returns></returns>
        public Task<string> GetServices() => RequestAsync("services");

        /// <summary>
        /// Returns an array of state changes in the past. Each object contains further details for the entities.
        /// </summary>
        /// <param name="entityId">Entity</param>
        /// <param name="timestamp">The timestamp is optional and defaults to 1 day before the time of the request. It determines the beginning of the period.</param>
        /// <param name="endtime">choose the end of the period in URL encoded format (defaults to 1 day).</param>
        /// <param name="significant_changes_only">Return only signifcant state changes.</param>
        /// <returns>an array of state changes in the past. Each object contains further details for the entities.</returns>
        public Task<string> GetHistory(string entityId, DateTimeOffset? timestamp = null, DateTimeOffset? endtime = null, bool significant_changes_only = false, bool minimal_response = false)
        {
            return GetHistory(new string[] { entityId }, timestamp, endtime, significant_changes_only, minimal_response);
        }

        /// <summary>
        /// Returns an array of state changes in the past. Each object contains further details for the entities.
        /// </summary>
        /// <param name="timestamp">The timestamp is optional and defaults to 1 day before the time of the request. It determines the beginning of the period.</param>
        /// <param name="endtime">choose the end of the period in URL encoded format (defaults to 1 day).</param>
        /// <param name="filterEntityIds">filter on one or more entities</param>
        /// <param name="significant_changes_only">Return only signifcant state changes.</param>
        /// <returns>an array of state changes in the past. Each object contains further details for the entities.</returns>
        public Task<string> GetHistory(IEnumerable<string> filterEntityIds = null, DateTimeOffset? timestamp = null, DateTimeOffset? endtime = null, bool significant_changes_only = false, bool minimal_response = false)
        {
            string url = "history/period";
            if (timestamp.HasValue)
                url += "/" + timestamp.Value.ToUniversalTime().ToString();
            url += "?";
            if (endtime.HasValue)
                url += "endtime=" + timestamp.Value.ToUniversalTime().ToString() + "&";
            if (filterEntityIds?.Any() == true)
            {
                url += "filter_entity_id=" + string.Join(",", filterEntityIds) + "&";
            }
            if (minimal_response)
                url += "minimal_response";
            return RequestAsync(url);
        }

        public Task<string> GetStates() => RequestAsync("states");

        public async Task<SensorState> GetState(string entityId) 
        { 
            string json = await RequestAsync("states/" + entityId).ConfigureAwait(false);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SensorState>(json);
        }

        public Task<string> SetState(string entityId, string state) => PostAsync("states/" + entityId, state);
        public Task<string> SwitchTurnOn(string entityId) => PostAsync("services/switch/turn_on", $"{{\"entity_id\": \"{entityId}\"}}");
        public Task<string> SwitchTurnOff(string entityId) => PostAsync("services/switch/turn_off", $"{{\"entity_id\": \"{entityId}\"}}");


        public class SensorState
        {
            public string entity_id { get; set; }
            public string state { get; set; }
            public Dictionary<string,object> attributes { get; set; }
        }

        public async Task<string> RequestAsync(string path)
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"{hostname}/api/{path}");
            msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(msg);
            string json = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            return json;
        }

        public async Task<string> PostAsync(string path, string content)
        {
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, $"{hostname}/api/{path}");
            msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            msg.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            var response = await client.SendAsync(msg);
            string json = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            return json;
        }
    }
}
