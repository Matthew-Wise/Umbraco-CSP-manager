using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.TagHelpers;

namespace Umbraco.Community.CSPManager.Tests.TagHelpers;

[TestFixture]
public class CspNonceTagHelperTests
{
	private Mock<ICspService> _cspService;
	private CspNonceTagHelper _tagHelper;
	private const string TestNonce = "abc123testNonce456";

	[SetUp]
	public void SetUp()
	{
		_cspService = new Mock<ICspService>();
		_cspService.Setup(s => s.GetOrCreateCspNonce(It.IsAny<HttpContext>())).Returns(TestNonce);
		_tagHelper = new CspNonceTagHelper(_cspService.Object, NullLogger<CspNonceTagHelper>.Instance);
	}

	private static ViewContext CreateViewContext(HttpContext httpContext = null)
		=> new() { HttpContext = httpContext ?? new DefaultHttpContext() };

	private static TagHelperOutput CreateOutput(string tagName) =>
		new(tagName, [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

	private static TagHelperContext CreateContext(string tagName = "script") =>
		new(tagName, [], new Dictionary<object, object>(), Guid.NewGuid().ToString());

	[Test]
	public void Process_WhenUseCspNonceIsFalse_DoesNotAddAttributesOrCallService()
	{
		_tagHelper.UseCspNonce = false;
		_tagHelper.ViewContext = CreateViewContext();
		var output = CreateOutput(Constants.TagHelper.ScriptTag);

		_tagHelper.Process(CreateContext(), output);

		Assert.That(output.Attributes, Is.Empty);
		_cspService.Verify(s => s.GetOrCreateCspNonce(It.IsAny<HttpContext>()), Times.Never);
	}

	[Test]
	public void Process_ScriptTag_AddsNonceAndSetsScriptContextFlag()
	{
		var httpContext = new DefaultHttpContext();
		_tagHelper.ViewContext = CreateViewContext(httpContext);
		_tagHelper.UseCspNonce = true;
		var output = CreateOutput(Constants.TagHelper.ScriptTag);

		_tagHelper.Process(CreateContext(Constants.TagHelper.ScriptTag), output);

		Assert.Multiple(() =>
		{
			Assert.That(output.Attributes["nonce"]?.Value?.ToString(), Is.EqualTo(TestNonce));
			Assert.That(httpContext.Items[Constants.TagHelper.CspManagerScriptNonceSet], Is.True);
			Assert.That(httpContext.Items.ContainsKey(Constants.TagHelper.CspManagerStyleNonceSet), Is.False);
		});
	}

	[Test]
	public void Process_StyleTag_AddsNonceAndSetsStyleContextFlag()
	{
		var httpContext = new DefaultHttpContext();
		_tagHelper.ViewContext = CreateViewContext(httpContext);
		_tagHelper.UseCspNonce = true;
		var output = CreateOutput(Constants.TagHelper.StyleTag);

		_tagHelper.Process(CreateContext(Constants.TagHelper.StyleTag), output);

		Assert.Multiple(() =>
		{
			Assert.That(output.Attributes["nonce"]?.Value?.ToString(), Is.EqualTo(TestNonce));
			Assert.That(httpContext.Items[Constants.TagHelper.CspManagerStyleNonceSet], Is.True);
			Assert.That(httpContext.Items.ContainsKey(Constants.TagHelper.CspManagerScriptNonceSet), Is.False);
		});
	}

	[Test]
	public void Process_LinkTag_AddsNonceAndSetsStyleContextFlag()
	{
		var httpContext = new DefaultHttpContext();
		_tagHelper.ViewContext = CreateViewContext(httpContext);
		_tagHelper.UseCspNonce = true;
		var output = CreateOutput(Constants.TagHelper.LinkTag);

		_tagHelper.Process(CreateContext(Constants.TagHelper.LinkTag), output);

		Assert.Multiple(() =>
		{
			Assert.That(output.Attributes["nonce"]?.Value?.ToString(), Is.EqualTo(TestNonce));
			Assert.That(httpContext.Items[Constants.TagHelper.CspManagerStyleNonceSet], Is.True);
		});
	}

	[Test]
	public void Process_WithIncludeDataAttributeTrue_AddsDataNonceAttribute()
	{
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspNonce = true;
		_tagHelper.IncludeDataAttribute = true;
		var output = CreateOutput(Constants.TagHelper.ScriptTag);

		_tagHelper.Process(CreateContext(), output);

		Assert.Multiple(() =>
		{
			Assert.That(output.Attributes["nonce"]?.Value?.ToString(), Is.EqualTo(TestNonce));
			Assert.That(output.Attributes["data-nonce"]?.Value?.ToString(), Is.EqualTo(TestNonce));
		});
	}

	[Test]
	public void Process_WithIncludeDataAttributeFalse_DoesNotAddDataNonceAttribute()
	{
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspNonce = true;
		_tagHelper.IncludeDataAttribute = false;
		var output = CreateOutput(Constants.TagHelper.ScriptTag);

		_tagHelper.Process(CreateContext(), output);

		Assert.Multiple(() =>
		{
			Assert.That(output.Attributes["nonce"]?.Value?.ToString(), Is.EqualTo(TestNonce));
			Assert.That(output.Attributes.ContainsName("data-nonce"), Is.False);
		});
	}

	[Test]
	public void Process_UnknownTag_DoesNotAddNonceAndDoesNotCallService()
	{
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspNonce = true;
		var output = CreateOutput("div");

		_tagHelper.Process(CreateContext("div"), output);

		Assert.That(output.Attributes.ContainsName("nonce"), Is.False);
		_cspService.Verify(s => s.GetOrCreateCspNonce(It.IsAny<HttpContext>()), Times.Never);
	}

	[Test]
	public void Process_NonceValue_MatchesServiceReturn()
	{
		const string uniqueNonce = "unique-nonce-xyz-789";
		_cspService.Setup(s => s.GetOrCreateCspNonce(It.IsAny<HttpContext>())).Returns(uniqueNonce);
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspNonce = true;
		var output = CreateOutput(Constants.TagHelper.ScriptTag);

		_tagHelper.Process(CreateContext(), output);

		Assert.That(output.Attributes["nonce"]?.Value?.ToString(), Is.EqualTo(uniqueNonce));
	}
}
