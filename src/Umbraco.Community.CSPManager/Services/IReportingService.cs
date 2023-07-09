namespace Umbraco.Community.CSPManager.Services;

using System.Threading.Tasks;
using Umbraco.Community.CSPManager.Models.Reporting;

public interface IReportingService
{
	Task SaveAsync(ReportModel report);
}
