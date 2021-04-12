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
        private static object lockObj = new object();
        private static System.DateTime clientCreationTime = System.DateTime.Now;
        private static HttpClient client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip });

        protected override async Task Handle(StatusRequest<NuGetPackageVersionWidget> request, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                if ((System.DateTime.Now - clientCreationTime).TotalMinutes > 10)
                {
                    // Recycle client every 10 minutes
                    clientCreationTime = System.DateTime.Now;
                    client = new HttpClient(new SocketsHttpHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip });
                }
            }
            request.Context.Status = Status.Running;
            var uri = "https://api.nuget.org/v3-flatcontainer/"+request.Context.PackageId+"/index.json";
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
