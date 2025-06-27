using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

using Umbraco.Cms.Api.Common.OpenApi;

namespace Umbraco.Community.CSPManager.Configuration;

public class CspCustomOperationIdHandler : IOperationIdHandler
{
    public bool CanHandle(ApiDescription apiDescription)
    {
        if (apiDescription.ActionDescriptor is not
            ControllerActionDescriptor controllerActionDescriptor)
            return false;

        return CanHandle(apiDescription, controllerActionDescriptor);
    }

    public bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
        => controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith("Umbraco.Community.CSPManager") is true;

    public string Handle(ApiDescription apiDescription)
        => $"{apiDescription.ActionDescriptor.RouteValues["action"]}";
}