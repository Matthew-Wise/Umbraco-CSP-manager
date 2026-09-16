using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.HealthChecks;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.HealthChecks;

[TestFixture]
public class CspDeprecatedDirectivesHealthCheckTests
{
	private ICspService _cspService;
	private CspDeprecatedDirectivesHealthCheck _sut;

	[SetUp]
	public void SetUp()
	{
		_cspService = Mock.Of<ICspService>();
		_sut = new CspDeprecatedDirectivesHealthCheck(_cspService);

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
	public async Task GetStatus_WhenNoPolicyUsesReportUri_ReturnsSuccess()
	{
		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Success));
	}

	[Test]
	public async Task GetStatus_WhenEnabledPolicyUsesReportUri_ReturnsWarning()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(true, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CspDefinition
			{
				Id = Constants.DefaultBackofficeId,
				IsBackOffice = true,
				Enabled = true,
				ReportingDirective = Constants.ReportingDirectives.ReportUri,
				ReportUri = "https://example.com/report"
			});

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.Multiple(() =>
		{
			Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Warning));
			Assert.That(statuses[0].Message, Does.Contain("back-office"));
			Assert.That(statuses[0].Message, Does.Contain("report-uri"));
		});
	}

	[Test]
	public async Task GetStatus_WhenPolicyUsesReportTo_ReturnsSuccess()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CspDefinition
			{
				Id = Constants.DefaultFrontEndId,
				Enabled = true,
				ReportingDirective = Constants.ReportingDirectives.ReportTo,
				ReportUri = "csp-endpoint"
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
