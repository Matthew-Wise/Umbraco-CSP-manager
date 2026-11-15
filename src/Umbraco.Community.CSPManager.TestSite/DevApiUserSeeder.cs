using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.CSPManager.TestSite;

/// <summary>
/// Development only: makes sure the API user the Umbraco CLI (`.env.csp-manager`) logs in with
/// exists, so a fresh clone or cloud session can use the CLI without clicking through the
/// backoffice. Driven by the <c>TestSite:ApiUser</c> section, which only
/// <c>appsettings.Development.json</c> sets.
/// </summary>
public sealed class DevApiUserComposer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.Services.Configure<DevApiUserOptions>(builder.Config.GetSection("TestSite:ApiUser"));
		builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DevApiUserSeeder>();
	}
}

public sealed class DevApiUserOptions
{
	public string? ClientId { get; set; }

	public string? ClientSecret { get; set; }

	public string Email { get; set; } = "cli-api-user@example.com";
}

public sealed class DevApiUserSeeder(
	IRuntimeState runtimeState,
	IUserService userService,
	IBackOfficeUserClientCredentialsManager clientCredentialsManager,
	IOptions<DevApiUserOptions> options,
	IHostEnvironment hostEnvironment,
	ILogger<DevApiUserSeeder> logger)
	: INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
	public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
	{
		DevApiUserOptions settings = options.Value;
		if (!hostEnvironment.IsDevelopment()
			|| runtimeState.Level != RuntimeLevel.Run
			|| string.IsNullOrWhiteSpace(settings.ClientId)
			|| string.IsNullOrWhiteSpace(settings.ClientSecret))
		{
			return;
		}

		if (await clientCredentialsManager.FindUserAsync(settings.ClientId) is not null)
		{
			return;
		}

		IUser? user = userService.GetByEmail(settings.Email);
		if (user is null)
		{
			var create = new UserCreateModel
			{
				Email = settings.Email,
				UserName = settings.Email,
				Name = "Umbraco CLI",
				Kind = UserKind.Api,
				UserGroupKeys = new HashSet<Guid> { Umbraco.Cms.Core.Constants.Security.AdminGroupKey },
			};

			var created = await userService.CreateAsync(Umbraco.Cms.Core.Constants.Security.SuperUserKey, create, approveUser: true);
			if (!created.Success || created.Result.CreatedUser is null)
			{
				logger.LogWarning("Could not create the Umbraco CLI API user: {Status}", created.Status);
				return;
			}

			user = created.Result.CreatedUser;
		}

		var saved = await clientCredentialsManager.SaveAsync(user.Key, settings.ClientId, settings.ClientSecret);
		if (saved.Success)
		{
			logger.LogInformation("Created the Umbraco CLI API user {ClientId}", settings.ClientId);
		}
		else
		{
			logger.LogWarning("Could not save the Umbraco CLI client credentials: {Status}", saved.Result);
		}
	}
}
