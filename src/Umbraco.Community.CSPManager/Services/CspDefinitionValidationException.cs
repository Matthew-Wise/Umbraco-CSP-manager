namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Exception thrown when a CSP definition violates identity or content validation rules.
/// </summary>
public sealed class CspDefinitionValidationException : InvalidOperationException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CspDefinitionValidationException"/> class.
	/// </summary>
	/// <param name="memberName">The <see cref="Models.CspDefinition"/> member the error relates to.</param>
	/// <param name="message">A message that describes the error.</param>
	public CspDefinitionValidationException(string memberName, string message) : base(message)
	{
		MemberName = memberName;
	}

	/// <summary>
	/// Gets the name of the member that failed validation.
	/// </summary>
	public string MemberName { get; }
}
