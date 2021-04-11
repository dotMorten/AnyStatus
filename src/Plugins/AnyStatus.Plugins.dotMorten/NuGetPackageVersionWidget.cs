using AnyStatus.API.Attributes;
using AnyStatus.API.Widgets;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AnyStatus.Plugins.dotMorten
{
    [Category("Morten")]
    [DisplayName("NuGet Package Version")]
    [Description("NuGet package version number")]
    public class NuGetPackageVersionWidget : TextLabelWidget, IPollable, IStandardWidget
    {
        [Order(10)]
        [Required]
        [DisplayName("NuGet Package")]
        [Description("The NuGet package id. For example: AnyStatus.API")]
        public string PackageId { get; set; }
    }
}
