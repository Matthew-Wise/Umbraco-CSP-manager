using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.HealthChecks;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.HealthChecks;

[TestFixture]
public class CspFrontendEnabledHealthCheckTests
{
	private ICspService _cspService;
	private CspFrontendEnabledHealthCheck _sut;

	[SetUp]
	public void SetUp()
	{
		_cspService = Mock.Of<ICspService>();
		_sut = new CspFrontendEnabledHealthCheck(_cspService);
	}

	[Test]
	public async Task GetStatus_WhenFrontendEnabled_ReturnsSuccess()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CspDefinition { Id = Constants.DefaultFrontEndId, Enabled = true });

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Success));
	}

	[Test]
	public async Task GetStatus_WhenFrontendDisabled_ReturnsWarningWithEnableAction()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CspDefinition { Id = Constants.DefaultFrontEndId, Enabled = false });

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.Multiple(() =>
		{
			Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Warning));
			Assert.That(statuses[0].Actions, Is.Not.Null.And.Count.EqualTo(1));
		});
	}

	[Test]
	public void ExecuteAction_WithUnknownAlias_Throws()
	{
		var action = new HealthCheckAction("someOtherAction", Guid.NewGuid());

		Assert.Throws<InvalidOperationException>(() => _sut.ExecuteAction(action));
	}

	[Test]
	public void ExecuteAction_WithEnableAlias_EnablesAndSavesDefinition()
	{
		var definition = new CspDefinition { Id = Constants.DefaultFrontEndId, Enabled = false };
		Mock.Get(_cspService)
			.Setup(x => x.GetCspDefinitionAsync(false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(definition);
		Mock.Get(_cspService)
			.Setup(x => x.SaveCspDefinitionAsync(It.IsAny<CspDefinition>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CspDefinition d, CancellationToken _) => d);

		var action = new HealthCheckAction("enableFrontendCsp", Guid.NewGuid());
		var result = _sut.ExecuteAction(action);

		Assert.That(result.ResultType, Is.EqualTo(StatusResultType.Success));
		Mock.Get(_cspService).Verify(
			x => x.SaveCspDefinitionAsync(It.Is<CspDefinition>(d => d.Enabled), It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
