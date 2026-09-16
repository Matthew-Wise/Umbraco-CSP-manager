using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.TagHelpers;

namespace Umbraco.Community.CSPManager.Tests.TagHelpers;

[TestFixture]
public class CspHashTagHelperTests
{
	private Mock<ICspService> _cspService;
	private CspHashTagHelper _tagHelper;
	private const string TestHash = "'sha256-testHashValue'";

	[SetUp]
	public void SetUp()
	{
		_cspService = new Mock<ICspService>();
		_cspService.Setup(s => s.AddCspHash(It.IsAny<HttpContext>(), It.IsAny<CspHashTarget>(), It.IsAny<string>())).Returns(TestHash);
		_tagHelper = new CspHashTagHelper(_cspService.Object, NullLogger<CspHashTagHelper>.Instance);
	}

	private static ViewContext CreateViewContext(HttpContext httpContext = null)
		=> new() { HttpContext = httpContext ?? new DefaultHttpContext() };

	private static TagHelperOutput CreateOutput(string tagName, string content = "", TagHelperAttributeList attributes = null) =>
		new(tagName, attributes ?? [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent(content)));

	private static TagHelperContext CreateContext(string tagName = "script") =>
		new(tagName, [], new Dictionary<object, object>(), Guid.NewGuid().ToString());

	[Test]
	public async Task ProcessAsync_WhenUseCspHashIsFalse_DoesNotCallService()
	{
		_tagHelper.UseCspHash = false;
		_tagHelper.ViewContext = CreateViewContext();
		var output = CreateOutput(Constants.TagHelper.ScriptTag, "doWhatever();");

		await _tagHelper.ProcessAsync(CreateContext(), output);

		_cspService.Verify(s => s.AddCspHash(It.IsAny<HttpContext>(), It.IsAny<CspHashTarget>(), It.IsAny<string>()), Times.Never);
	}

	[Test]
	public async Task ProcessAsync_ScriptTag_PassesContentAndScriptTargetToService()
	{
		var httpContext = new DefaultHttpContext();
		_tagHelper.ViewContext = CreateViewContext(httpContext);
		_tagHelper.UseCspHash = true;
		var output = CreateOutput(Constants.TagHelper.ScriptTag, "doWhatever();");

		await _tagHelper.ProcessAsync(CreateContext(Constants.TagHelper.ScriptTag), output);

		_cspService.Verify(s => s.AddCspHash(httpContext, CspHashTarget.Script, "doWhatever();"), Times.Once);
	}

	[Test]
	public async Task ProcessAsync_StyleTag_PassesContentAndStyleTargetToService()
	{
		var httpContext = new DefaultHttpContext();
		_tagHelper.ViewContext = CreateViewContext(httpContext);
		_tagHelper.UseCspHash = true;
		var output = CreateOutput(Constants.TagHelper.StyleTag, ".alert { color: red; }");

		await _tagHelper.ProcessAsync(CreateContext(Constants.TagHelper.StyleTag), output);

		_cspService.Verify(s => s.AddCspHash(httpContext, CspHashTarget.Style, ".alert { color: red; }"), Times.Once);
	}

	[Test]
	public async Task ProcessAsync_TagWithSrcAttribute_DoesNotCallService()
	{
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspHash = true;
		var attributes = new TagHelperAttributeList { new TagHelperAttribute("src", "/script.js") };
		var output = CreateOutput(Constants.TagHelper.ScriptTag, "", attributes);

		await _tagHelper.ProcessAsync(CreateContext(Constants.TagHelper.ScriptTag), output);

		_cspService.Verify(s => s.AddCspHash(It.IsAny<HttpContext>(), It.IsAny<CspHashTarget>(), It.IsAny<string>()), Times.Never);
	}

	[Test]
	public async Task ProcessAsync_WithIncludeDataAttributeTrue_AddsDataCspHashAttributeWithoutQuotes()
	{
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspHash = true;
		_tagHelper.IncludeDataAttribute = true;
		var output = CreateOutput(Constants.TagHelper.ScriptTag, "doWhatever();");

		await _tagHelper.ProcessAsync(CreateContext(), output);

		Assert.That(output.Attributes["data-csp-hash"]?.Value?.ToString(), Is.EqualTo("sha256-testHashValue"));
	}

	[Test]
	public async Task ProcessAsync_WithIncludeDataAttributeFalse_DoesNotAddDataCspHashAttribute()
	{
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspHash = true;
		_tagHelper.IncludeDataAttribute = false;
		var output = CreateOutput(Constants.TagHelper.ScriptTag, "doWhatever();");

		await _tagHelper.ProcessAsync(CreateContext(), output);

		Assert.That(output.Attributes.ContainsName("data-csp-hash"), Is.False);
	}

	[Test]
	public async Task ProcessAsync_UnknownTag_DoesNotCallService()
	{
		_tagHelper.ViewContext = CreateViewContext();
		_tagHelper.UseCspHash = true;
		var output = CreateOutput("div", "content");

		await _tagHelper.ProcessAsync(CreateContext("div"), output);

		_cspService.Verify(s => s.AddCspHash(It.IsAny<HttpContext>(), It.IsAny<CspHashTarget>(), It.IsAny<string>()), Times.Never);
	}
}
