using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.HealthChecks;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.HealthChecks;

[TestFixture]
public class CspUnsafeScriptSourceHealthCheckTests
{
	private ICspService _cspService;
	private CspUnsafeScriptSourceHealthCheck _sut;

	[SetUp]
	public void SetUp()
	{
		_cspService = Mock.Of<ICspService>();
		_sut = new CspUnsafeScriptSourceHealthCheck(_cspService);

		// Default: no policy for either context unless overridden below.
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((bool isBackOffice, CancellationToken _) => new CspDefinition
			{
				Id = isBackOffice ? Constants.DefaultBackofficeId : Constants.DefaultFrontEndId,
				IsBackOffice = isBackOffice,
				Enabled = false
			});
	}

	[Test]
	public async Task GetStatus_WhenNoEnforcedUnsafeSources_ReturnsSuccess()
	{
		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Success));
	}

	[Test]
	public async Task GetStatus_WhenEnforcedScriptSrcHasUnsafeInline_ReturnsWarning()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CspDefinition
			{
				Id = Constants.DefaultFrontEndId,
				Enabled = true,
				ReportOnly = false,
				Sources =
				[
					new CspDefinitionSource { Source = "'unsafe-inline'", Directives = [Constants.Directives.ScriptSource] }
				]
			});

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.Multiple(() =>
		{
			Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Warning));
			Assert.That(statuses[0].Message, Does.Contain("'unsafe-inline'"));
		});
	}

	[Test]
	public async Task GetStatus_WhenUnsafeInlineOnlyInReportOnlyPolicy_ReturnsSuccess()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CspDefinition
			{
				Id = Constants.DefaultFrontEndId,
				Enabled = true,
				ReportOnly = true,
				Sources =
				[
					new CspDefinitionSource { Source = "'unsafe-eval'", Directives = [Constants.Directives.ScriptSource] }
				]
			});

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Success));
	}

	[Test]
	public async Task GetStatus_WhenUnsafeInlineOnlyInScriptSrcAttr_ReturnsSuccess()
	{
		// script-src-attr is the documented, supported way to allow inline event handlers alongside a
		// nonce (see CspMiddleware.AddNonceToDirectives), so it must not be flagged here.
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CspDefinition
			{
				Id = Constants.DefaultFrontEndId,
				Enabled = true,
				ReportOnly = false,
				Sources =
				[
					new CspDefinitionSource { Source = "'unsafe-inline'", Directives = [Constants.Directives.ScriptSourceAttribute] }
				]
			});

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Success));
	}

	[Test]
	public void ExecuteAction_Throws()
	{
		var action = new HealthCheckAction("anyAlias", Guid.NewGuid());

		Assert.Throws<InvalidOperationException>(() => _sut.ExecuteAction(action));
	}
}
