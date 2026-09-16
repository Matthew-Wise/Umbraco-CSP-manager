using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.Services;

[TestFixture]
public class CspHealthMonitorTests
{
	[Test]
	public void LastFailure_WhenNoFailureRecorded_IsNull()
	{
		var monitor = new CspHealthMonitor();

		Assert.That(monitor.LastFailure, Is.Null);
	}

	[Test]
	public void RecordFailure_SetsLastFailureFromPathAndException()
	{
		var monitor = new CspHealthMonitor();
		var exception = new InvalidOperationException("boom");

		monitor.RecordFailure(new PathString("/content/page"), exception);

		var failure = monitor.LastFailure;
		Assert.That(failure, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(failure!.Path, Is.EqualTo("/content/page"));
			Assert.That(failure.Message, Is.EqualTo("boom"));
		});
	}

	[Test]
	public void RecordFailure_CalledAgain_OverwritesPreviousFailure()
	{
		var monitor = new CspHealthMonitor();
		monitor.RecordFailure(new PathString("/first"), new InvalidOperationException("first"));

		monitor.RecordFailure(new PathString("/second"), new InvalidOperationException("second"));

		Assert.That(monitor.LastFailure!.Path, Is.EqualTo("/second"));
	}

	[Test]
	public void Reset_ClearsRecordedFailure()
	{
		var monitor = new CspHealthMonitor();
		monitor.RecordFailure(new PathString("/content/page"), new InvalidOperationException("boom"));

		monitor.Reset();

		Assert.That(monitor.LastFailure, Is.Null);
	}
}
