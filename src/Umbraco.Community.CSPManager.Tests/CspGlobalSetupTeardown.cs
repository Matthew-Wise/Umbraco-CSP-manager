
// ReSharper disable once CheckNamespace
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

/// <inheritdoc cref="GlobalSetupTeardown"/>
[SuppressMessage("", "S3903: Move '' into a named namespace", Justification = "We want this SetUp fixture to run on the assembly")]
[SetUpFixture]
public class CspGlobalSetupTeardown
{
	private static GlobalSetupTeardown _setupTearDown;

	[OneTimeSetUp]
	public void SetUp()
	{
		_setupTearDown = new GlobalSetupTeardown();
		_setupTearDown.SetUp();

		var importOnFirstBoot = GlobalSetupTeardown.TestConfiguration
			.GetValue<bool>("uSync:Settings:ImportOnFirstBoot");
		TestContext.Progress.WriteLine(
			"******************************************************************************");
		TestContext.Progress.WriteLine("* CSP Test Settings");
		TestContext.Progress.WriteLine("*");
		TestContext.Progress.WriteLine($"* uSync FirstBoot     : {importOnFirstBoot}");
		TestContext.Progress.WriteLine(
			"******************************************************************************");
	}

	[OneTimeTearDown]
	public void TearDown()
	{
		_setupTearDown.TearDown();
	}
}