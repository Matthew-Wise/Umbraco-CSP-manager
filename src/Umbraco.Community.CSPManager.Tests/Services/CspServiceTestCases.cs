namespace Umbraco.Community.CSPManager.Tests.Services;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Community.CSPManager.Models;

internal class CspServiceTestCases
{
	public static IEnumerable<TestCaseData> SaveCspDefinitionSource
	{
		get
		{
			var oneLessSource = new CspDefinition
			{
				Id = CspConstants.DefaultBackofficeId,
				Enabled = true,
				IsBackOffice = true,
				Sources = CspConstants.DefaultBackOfficeCsp.GetRange(0, CspConstants.DefaultBackOfficeCsp.Count - 1)
			};

			yield return new TestCaseData(oneLessSource) { TestName = "Remove a CSP Source from a Definition" };

			var additionalSource = CspConstants.DefaultBackOfficeCsp.ToList();
			additionalSource.Add(new CspDefinitionSource
			{
				DefinitionId = CspConstants.DefaultBackofficeId,
				Directives = new() { CspConstants.Directives.BaseUri },
				Source = "test"
			});

			yield return new TestCaseData(new CspDefinition
			{
				Id = CspConstants.DefaultBackofficeId,
				Enabled = true,
				IsBackOffice = true,
				Sources = additionalSource
			})
			{ TestName = "Add a CSP Source to a Definition" };


			var longSource = CspConstants.DefaultBackOfficeCsp.ToList();
			longSource.Add(new CspDefinitionSource
			{
				DefinitionId = CspConstants.DefaultBackofficeId,
				Directives = new() { CspConstants.Directives.BaseUri },
				Source = new string('a', 300)
			});


			yield return new TestCaseData(new CspDefinition
			{
				Id = CspConstants.DefaultBackofficeId,
				Enabled = true,
				IsBackOffice = true,
				Sources = additionalSource
			})
			{ TestName = "Add a CSP Long Source to a Definition" };
		}
	}
}
