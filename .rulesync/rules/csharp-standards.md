---
targets: ["*"]
description: "C# coding standards and best practices for CSP Manager backend"
globs: ["src/**/*.cs", "!src/**/Client/**/*"]
---

# C# Coding Standards

## Naming Conventions

### Classes and Methods
- Use PascalCase for public members
- Use camelCase with underscore prefix for private fields
- Use descriptive, intention-revealing names

```csharp
public class CspDefinitionService : ICspDefinitionService
{
    private readonly ICspRepository _cspRepository;
    private readonly ILogger<CspDefinitionService> _logger;
    
    public async Task<CspDefinition> GetCspDefinitionAsync(bool isBackOffice)
    {
        return await _cspRepository.GetByTypeAsync(isBackOffice);
    }
}
```

## Umbraco Integration Patterns

### Composer Pattern
```csharp
public class CspManagerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ICspService, CspService>();
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter("CspManager")
            {
                Endpoints = app => app.UseMiddleware<CspMiddleware>()
            });
        });
    }
}
```

### Database Entities
```csharp
[TableName("CspDefinition")]
[PrimaryKey(nameof(Id), AutoIncrement = false)]
public class CspDefinition
{
    [PrimaryKeyColumn(AutoIncrement = false)]
    public Guid Id { get; set; }
    
    public bool Enabled { get; set; }
    public bool ReportOnly { get; set; }
    
    [ResultColumn]
    [Reference(ReferenceType.Many)]
    public List<CspDefinitionSource> Sources { get; set; } = [];
}
```

## Security Best Practices

### Input Validation
- Validate all CSP directives against known standards
- Sanitize user-provided sources
- Use parameterized queries for database access

```csharp
public ValidationResult ValidateDirective(string directive, string source)
{
    if (!ValidDirectives.Contains(directive))
        return ValidationResult.Error($"Invalid CSP directive: {directive}");
    
    if (source.Contains("javascript:"))
        return ValidationResult.Error("JavaScript protocol not allowed");
    
    return ValidationResult.Success();
}
```

### Nonce Generation
```csharp
public class SecureNonceGenerator
{
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    
    public string GenerateNonce()
    {
        var bytes = new byte[32];
        _rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
```

## Error Handling

### Exception Patterns
```csharp
public class CspServiceException : Exception
{
    public string? ErrorCode { get; }
    
    public CspServiceException(string message, string? errorCode = null) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

### API Error Responses
```csharp
[HttpPost("definitions")]
public async Task<IActionResult> SaveDefinition([FromBody] CspApiDefinition definition)
{
    if (!ModelState.IsValid)
        return BadRequest(new ValidationProblemDetails(ModelState));
    
    try
    {
        var saved = await _cspService.SaveCspDefinitionAsync(definition.ToCspDefinition());
        return Ok(CspApiDefinition.FromCspDefinition(saved));
    }
    catch (CspValidationException ex)
    {
        return BadRequest(new ProblemDetails 
        { 
            Title = "Validation Error",
            Detail = string.Join(", ", ex.ValidationErrors)
        });
    }
}
```