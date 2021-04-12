using AnyStatus.API.Widgets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AnyStatus.Plugins.dotMorten
{
    public class NuGetPackageQuery : AsyncStatusCheck<NuGetPackageVersionWidget>
    {

        protected override async Task Handle(StatusRequest<NuGetPackageVersionWidget> request, CancellationToken cancellationToken)
        {
            using HttpClient client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip }, true);
            request.Context.Status = Status.Running;
            var uri = "https://api.nuget.org/v3-flatcontainer/" + request.Context.PackageId.ToLowerInvariant() + "/index.json";
            var result = await client.GetStringAsync(uri);
            VersionInfo versionInfo = JsonConvert.DeserializeObject<VersionInfo>(result);
            var version = versionInfo.versions.LastOrDefault();
            request.Context.Text = version;
            request.Context.Status = Status.OK;
        }

        private class VersionInfo
        {
            public List<string> versions { get; set; }
        }
    }
}
