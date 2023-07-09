namespace Umbraco.Community.CSPManager.Services;

using System.Threading.Tasks;
using Umbraco.Community.CSPManager.Models.Reporting;

public interface ICspReportingService
{
	Task SaveAsync(ReportModel report);
}
