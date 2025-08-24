---
targets: ["*"]
description: "Security practices specific to CSP management and web security"
globs: ["src/**/*", "!**/node_modules/**/*"]
---

# Security Practices

## CSP Security Fundamentals

### Default Secure Policies
Always start with restrictive policies and loosen as needed:

```csharp
public static CspDefinition CreateSecureDefault(bool isBackOffice)
{
    return new CspDefinition
    {
        Enabled = true,
        ReportOnly = true, // Start in report-only mode
        IsBackOffice = isBackOffice,
        Sources = new List<CspDefinitionSource>
        {
            new() { Directive = "default-src", Source = "'self'" },
            new() { Directive = "script-src", Source = "'self'" },
            new() { Directive = "style-src", Source = "'self' 'unsafe-inline'" },
            new() { Directive = "img-src", Source = "'self' data: https:" },
            new() { Directive = "frame-ancestors", Source = "'none'" }
        }
    };
}
```

### Input Validation
Validate all CSP directives and sources:

```csharp
public class CspDirectiveValidator
{
    private static readonly HashSet<string> ValidDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "default-src", "script-src", "style-src", "img-src", "font-src",
        "connect-src", "media-src", "object-src", "frame-src", "worker-src",
        "frame-ancestors", "form-action", "upgrade-insecure-requests"
    };

    private static readonly Regex SourcePattern = new(@"^('self'|'unsafe-inline'|'unsafe-eval'|'none'|'strict-dynamic'|'nonce-[\w\-]+|'sha\d+-[\w+/=]+|https?://[\w\-\.]+(:\d+)?(/.*)?|\*\.[\w\-\.]+|\*)$");

    public ValidationResult ValidateDirective(string directive, string source)
    {
        if (!ValidDirectives.Contains(directive))
            return ValidationResult.Error($"Invalid CSP directive: {directive}");

        if (!SourcePattern.IsMatch(source))
            return ValidationResult.Error($"Invalid CSP source format: {source}");

        // Block dangerous patterns
        if (source.Contains("javascript:"))
            return ValidationResult.Error("JavaScript protocol not allowed in CSP sources");

        if (source.Contains("'unsafe-eval'") && directive == "script-src")
            return ValidationResult.Warning("'unsafe-eval' weakens XSS protection");

        return ValidationResult.Success();
    }
}
```

## Nonce Security

### Cryptographically Secure Generation
```csharp
public class SecureNonceGenerator : IDisposable
{
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public string GenerateNonce()
    {
        var bytes = new byte[32]; // 256 bits of entropy
        _rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('='); // URL-safe base64
    }

    public void Dispose() => _rng?.Dispose();
}
```

### Nonce Management Rules
- Generate fresh nonces for each request
- Never persist nonces to database or cache  
- Clear nonces from memory after response
- Use minimum 128 bits of entropy

```csharp
public string GetOrCreateScriptNonce(HttpContext context)
{
    const string key = "csp-script-nonce";
    
    if (context.Items.TryGetValue(key, out var existingNonce))
        return (string)existingNonce;

    var nonce = _nonceGenerator.GenerateNonce();
    context.Items[key] = nonce;
    
    // Cleanup after response
    context.Response.OnCompleted(() =>
    {
        context.Items.Remove(key);
        return Task.CompletedTask;
    });

    return nonce;
}
```

## Authorization & Access Control

### Role-Based CSP Management
```csharp
[Authorize(Policy = "CspManagerPolicy")]
public class DefinitionsController : CspManagerControllerBase
{
    [HttpPost("definitions/save")]
    public async Task<IActionResult> SaveDefinition([FromBody] CspApiDefinition definition)
    {
        // Additional authorization check
        if (!await CanUserModifyCspAsync())
        {
            return Forbid("Insufficient permissions to modify CSP");
        }

        return Ok(await _cspService.SaveCspDefinitionAsync(definition.ToCspDefinition()));
    }

    private async Task<bool> CanUserModifyCspAsync()
    {
        var user = User;
        return user.IsInRole("admin") || 
               user.HasClaim("csp", "manage") ||
               await _userService.IsInUserGroupAsync(user.Identity.Name, "CSPManagers");
    }
}
```

## Database Security

### SQL Injection Prevention
Always use parameterized queries:

```csharp
public async Task<CspDefinition?> GetByTypeAsync(bool isBackOffice)
{
    var sql = _database.SqlContext.Sql()
        .Select<CspDefinition>()
        .From<CspDefinition>()
        .Where<CspDefinition>(x => x.IsBackOffice == isBackOffice); // Parameterized

    return await _database.FirstOrDefaultAsync<CspDefinition>(sql);
}

// NEVER do this:
// var sql = $"SELECT * FROM CspDefinition WHERE IsBackOffice = {isBackOffice}";
```

### Data Sanitization
```csharp
public async Task SaveAsync(CspDefinition definition)
{
    // Validate ID
    if (definition.Id == Guid.Empty)
        throw new ArgumentException("Invalid definition ID");

    // Sanitize and validate sources
    foreach (var source in definition.Sources)
    {
        source.Source = SanitizeCspSource(source.Source);
        
        var validation = ValidateDirective(source.Directive, source.Source);
        if (!validation.IsValid)
            throw new CspValidationException(validation.Errors);
    }

    await _database.SaveAsync(definition);
}
```

## Frontend Security

### XSS Prevention in UI
```typescript
// Safe HTML rendering with Lit
render() {
  return html`
    <div class="source-display">
      <!-- Safe: Lit automatically escapes text content -->
      <span>${this.source.directive}</span>: 
      <span>${this.source.source}</span>
      
      <!-- NEVER do this: -->
      <!-- <span .innerHTML="${this.source.source}"></span> -->
    </div>
  `;
}
```

### API Input Validation (Frontend)
```typescript
export class CspDefinitionRepository {
  async saveCspDefinition(definition: CspDefinition): Promise<CspDefinition> {
    // Client-side validation (defense in depth)
    this.validateDefinition(definition);
    
    try {
      const response = await this.apiClient.post<CspDefinition>('/definitions', definition);
      return response.data;
    } catch (error) {
      if (error instanceof ValidationError) {
        throw new CspValidationError("Invalid CSP definition", error.validationErrors);
      }
      throw new CspError("Failed to save CSP definition", error);
    }
  }

  private validateDefinition(definition: CspDefinition): void {
    if (!definition.id || definition.sources.length === 0) {
      throw new CspValidationError("Definition must have ID and sources", []);
    }

    // Validate each source
    for (const source of definition.sources) {
      if (source.source.includes("javascript:")) {
        throw new CspValidationError("JavaScript protocol not allowed", []);
      }
    }
  }
}
```

## Security Logging

### Security Event Logging
```csharp
public class SecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public void LogCspPolicyChanged(string userId, CspDefinition oldPolicy, CspDefinition newPolicy)
    {
        _logger.LogWarning("CSP policy modified by user {UserId}. Context: {Context}, Sources: {SourceCount}", 
            userId, 
            newPolicy.IsBackOffice ? "BackOffice" : "Frontend",
            newPolicy.Sources.Count);
    }

    public void LogSuspiciousCspConfiguration(string directive, string source, string reason)
    {
        _logger.LogWarning("Suspicious CSP configuration detected. Directive: {Directive}, Source: {Source}, Reason: {Reason}",
            directive, 
            SanitizeForLogging(source), // Never log full user input
            reason);
    }

    public void LogUnauthorizedCspAccess(string userId, string action, string resource)
    {
        _logger.LogError("Unauthorized CSP access attempt by user {UserId} for action {Action} on {Resource}", 
            userId, action, resource);
    }
}
```

## Emergency Security Controls

### Kill Switch Implementation
```csharp
public class CspMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Emergency kill switch
        if (_options.EmergencyDisableAll)
        {
            _logger.LogWarning("CSP is emergency disabled - no headers will be applied");
            await _next(context);
            return;
        }

        // Context-specific disable
        var isBackOfficeRequest = context.Request.IsBackOfficeRequest();
        if (isBackOfficeRequest && _options.DisableBackOfficeHeader)
        {
            _logger.LogInformation("BackOffice CSP disabled via configuration");
            await _next(context);
            return;
        }

        // Normal CSP processing
        await ApplyCspHeaders(context);
        await _next(context);
    }
}