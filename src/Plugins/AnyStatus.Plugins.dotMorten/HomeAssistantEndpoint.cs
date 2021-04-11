using AnyStatus.API.Attributes;
using AnyStatus.API.Endpoints;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AnyStatus.Plugins.dotMorten
{
    [DisplayName("Home Assistant")]
    public class HomeAssistantEndpoint : Endpoint
    {
        public HomeAssistantEndpoint()
        {
            Name = "Home Assistant";
            Address = "http://ip:8123";
        }

        [Required]
        [DisplayName("Personal Access Token")]
        [Description("Personal Access Token (PAT) The PAT is used to create a Home Assistant SmartApp in your SmartThings account during setup of the integration. Log into the personal access tokens page and click ‘ Generate new token.")]
        [Order(30)]
        public string Token { get; set; }
    }
}
