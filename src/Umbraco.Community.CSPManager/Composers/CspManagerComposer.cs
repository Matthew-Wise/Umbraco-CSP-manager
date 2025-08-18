using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.CSPManager.Extensions;

namespace Umbraco.Community.CSPManager.Composers;

public sealed class Composer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.AddCspManager();		
    }
}
