using System.Security.Cryptography;
using System.Text;

namespace Umbraco.Community.CSPManager.uSync;

/// <summary>
/// Provides the uSync keys of node policies.
/// </summary>
/// <remarks>
/// A node policy's definition id is random per environment, so it can't be the uSync key: two
/// environments that each create a policy for the same node would never match. The key is a
/// name-based (RFC 4122 version 5) GUID of the content key instead, the same on every environment.
/// </remarks>
internal static class CspItemKeys
{
	// Never change this: every node policy file holds a key derived from it.
	private static readonly Guid NodePolicyNamespace = new("89929f96-e960-477c-94f5-e485cc5cade3");

	/// <summary>
	/// Gets the uSync key of the policy of a content node.
	/// </summary>
	/// <param name="contentKey">The key of the content node the policy belongs to.</param>
	public static Guid NodePolicy(Guid contentKey)
		=> NameBased(NodePolicyNamespace, contentKey.ToString("D"));

	private static Guid NameBased(Guid @namespace, string name)
	{
		var nameBytes = Encoding.UTF8.GetBytes(name);
		var input = new byte[16 + nameBytes.Length];
		@namespace.TryWriteBytes(input, bigEndian: true, out _);
		nameBytes.CopyTo(input, 16);

		var hash = SHA1.HashData(input);
		var bytes = hash.AsSpan(0, 16);
		bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50); // version 5
		bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 4122 variant
		return new Guid(bytes, bigEndian: true);
	}
}
