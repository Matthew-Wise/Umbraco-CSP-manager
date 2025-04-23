
// ReSharper disable once CheckNamespace
using System.Diagnostics.CodeAnalysis;

/// <inheritdoc cref="GlobalSetupTeardown"/>
[SuppressMessage("", "S3903: Move '' into a named namespace", Justification = "We want this SetUp fixture to run on the assembly")]
[SetUpFixture]
public class CspGlobalSetupTeardown : GlobalSetupTeardown
{
}