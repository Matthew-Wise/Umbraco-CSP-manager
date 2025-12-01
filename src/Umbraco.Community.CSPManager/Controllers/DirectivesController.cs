using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Umbraco.Community.CSPManager.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Directives")]
public class DirectivesController : CspManagerControllerBase
{
	[HttpGet("Directives")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(ICollection<string>), 200)]
#pragma warning disable CA1822 // Mark members as static
	public ICollection<string> GetCspDirectiveOptions(CancellationToken cancellationToken = default) => Constants.AllDirectives.ToArray();
#pragma warning restore CA1822 // Mark members as static
}