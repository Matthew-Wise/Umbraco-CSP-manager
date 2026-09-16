using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.HealthChecks;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.HealthChecks;

[TestFixture]
public class CspHeaderConstructionHealthCheckTests
{
	private ICspHealthMonitor _healthMonitor;
	private CspHeaderConstructionHealthCheck _sut;

	[SetUp]
	public void SetUp()
	{
		_healthMonitor = Mock.Of<ICspHealthMonitor>();
		_sut = new CspHeaderConstructionHealthCheck(_healthMonitor);
	}

	[Test]
	public async Task GetStatus_WhenNoFailureRecorded_ReturnsSuccess()
	{
		Mock.Get(_healthMonitor).Setup(x => x.LastFailure).Returns((CspHeaderFailure)null);

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Success));
	}

	[Test]
	public async Task GetStatus_WhenFailureRecorded_ReturnsErrorWithDismissAction()
	{
		var failure = new CspHeaderFailure(DateTimeOffset.UtcNow, "/content/page", "boom");
		Mock.Get(_healthMonitor).Setup(x => x.LastFailure).Returns(failure);

		var statuses = (await _sut.GetStatus()).ToList();

		Assert.That(statuses, Has.Count.EqualTo(1));
		Assert.Multiple(() =>
		{
			Assert.That(statuses[0].ResultType, Is.EqualTo(StatusResultType.Error));
			Assert.That(statuses[0].Message, Does.Contain("/content/page"));
			Assert.That(statuses[0].Message, Does.Contain("boom"));
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
	public void ExecuteAction_WithDismissAlias_ResetsMonitorAndReturnsSuccess()
	{
		var action = new HealthCheckAction("dismissCspHeaderFailure", Guid.NewGuid());

		var result = _sut.ExecuteAction(action);

		Assert.That(result.ResultType, Is.EqualTo(StatusResultType.Success));
		Mock.Get(_healthMonitor).Verify(x => x.Reset(), Times.Once);
	}
}
