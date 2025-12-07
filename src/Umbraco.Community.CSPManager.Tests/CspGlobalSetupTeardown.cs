// ReSharper disable once CheckNamespace
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.Nodes;
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

		// Configure JsonDiffPatcher globally for all tests
		JsonDiffPatcher.DefaultOptions = () => new JsonDiffOptions()
		{
			PropertyFilter = (path, _) =>
			{
				return path is not "traceId";
			},
			ValueComparer = new GuidIgnoringValueComparer(),
			JsonElementComparison = JsonElementComparison.Semantic
		};

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

	private class GuidIgnoringValueComparer : IEqualityComparer<JsonValue>
	{
		public bool Equals(JsonValue left, JsonValue right)
		{
			// If both values are strings, try to parse them as GUIDs
			var leftStr = left?.ToString();
			var rightStr = right?.ToString();

			if (!string.IsNullOrEmpty(leftStr) && !string.IsNullOrEmpty(rightStr)
				&& Guid.TryParse(leftStr, out _) && Guid.TryParse(rightStr, out _))
			{
				return true;
			}

			return JsonValueComparer.Compare(left, right) == 0;
		}

		public int GetHashCode(JsonValue value)
		{
			var stringValue = value?.ToString();

			// If the value is a GUID, return a consistent hash (all GUIDs hash the same)
			if (!string.IsNullOrEmpty(stringValue) && Guid.TryParse(stringValue, out _))
			{
				return Guid.Empty.GetHashCode();
			}

			return stringValue?.GetHashCode() ?? 0;
		}
	}
}