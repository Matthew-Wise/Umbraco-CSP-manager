namespace Umbraco.Community.CSPManager.Tests.Controllers;

using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

[TestFixture]
public class CSPManagerApiControllerTests
{
	private ICspService _cspService;

	private CSPManagerApiController _sud;

	[OneTimeSetUp]
	public void SetUp()
	{
		_cspService = Mock.Of<ICspService>();
		_sud = new CSPManagerApiController(_cspService);
	}
	
	[Test]
	public async Task SaveDefinition_ThrowsOn_Default_DefinitionId()
	{
		Func<Task> act = async () =>
		{
			await _sud.SaveDefinition(Mock.Of<CspDefinition>());
		};

		await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
	}

	[Test]
	public async Task SaveDefinition_CallsSaveAsync_WithValidDefinition()
	{
		await _sud.SaveDefinition(Mock.Of<CspDefinition>(x => x.Id == Guid.NewGuid()));

		Mock.Get(_cspService).Verify(x => x.SaveCspDefinitionAsync(It.IsAny<CspDefinition>()), Times.Once);
	}
}
