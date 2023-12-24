// ReSharper disable once CheckNamespace
/// <inheritdoc cref="GlobalSetupTeardown"/>
[SetUpFixture]
public class CspGlobalSetupTeardown
{
	private static GlobalSetupTeardown _setupTearDown;

	[OneTimeSetUp]
	public void SetUp()
	{
		_setupTearDown = new GlobalSetupTeardown();
		_setupTearDown.SetUp();
	}

	[OneTimeTearDown]
	public void TearDown()
	{
		_setupTearDown.TearDown();
	}
}