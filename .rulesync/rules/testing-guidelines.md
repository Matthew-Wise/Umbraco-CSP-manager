---
targets: ["*"]  
description: "Testing standards for both backend and frontend code"
globs: ["**/*test*", "**/*spec*", "**/Tests/**/*"]
---

# Testing Guidelines

## Backend Testing (.NET/XUnit)

### Test Organization
```csharp
[TestFixture]
public class CspServiceTests : UmbracoIntegrationTest
{
    [Test]
    public async Task GetCspDefinition_WithValidBackOfficeRequest_ReturnsBackOfficeDefinition()
    {
        // Arrange
        var expectedDefinition = CspDefinitionBuilder.Create()
            .ForBackOffice()
            .WithDefaultSources()
            .Build();
        await SaveDefinitionToDatabase(expectedDefinition);

        // Act
        var result = _cspService.GetCspDefinition(isBackOfficeRequest: true);

        // Assert
        Assert.That(result.IsBackOffice, Is.True);
        Assert.That(result.Id, Is.EqualTo(expectedDefinition.Id));
    }
}
```

### Test Data Builders
```csharp
public class CspDefinitionBuilder
{
    private CspDefinition _definition = new();

    public static CspDefinitionBuilder Create() => new();
    
    public CspDefinitionBuilder ForBackOffice(bool isBackOffice = true)
    {
        _definition.IsBackOffice = isBackOffice;
        return this;
    }
    
    public CspDefinitionBuilder WithSource(string directive, string source)
    {
        _definition.Sources.Add(new CspDefinitionSource 
        { 
            Directive = directive, 
            Source = source 
        });
        return this;
    }
    
    public CspDefinition Build() => _definition;
}
```

### Snapshot Testing with Verify
```csharp
[Test]
public Task GenerateCspHeader_WithComplexPolicy_CreatesExpectedOutput()
{
    // Arrange
    var definition = CspDefinitionBuilder.Create()
        .WithSource("default-src", "'self'")
        .WithSource("script-src", "'self' 'unsafe-inline'")
        .Build();

    // Act
    var header = CspHeaderBuilder.GenerateHeader(definition, context);

    // Assert
    return Verify(header)
        .UseDirectory("CspHeaders")
        .UseFileName("ComplexPolicy");
}
```

## Frontend Testing (Playwright)

### E2E Test Structure
```typescript
// tests/dashboard.spec.ts
import { test, expect } from '@playwright/test';

test.describe('CSP Management Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/umbraco/#/csp-manager');
    await page.waitForSelector('csp-section-dashboard');
  });

  test('should display policy status overview', async ({ page }) => {
    // Given: User is on the dashboard
    const statusSection = page.locator('.csp-status-section');
    
    // Then: Status information is displayed
    await expect(statusSection).toBeVisible();
    await expect(statusSection.locator('.frontend-status')).toContainText('Frontend CSP');
    await expect(statusSection.locator('.backoffice-status')).toContainText('Backoffice CSP');
  });
});
```

### Component Testing
```typescript
test.describe('CSP Definition Form Component', () => {
  test('should validate CSP sources correctly', async ({ page }) => {
    await page.setContent(`
      <csp-definition-form></csp-definition-form>
      <script type="module" src="/App_Plugins/UmbracoCommunityCSPManager/umbraco-community-csp-manager.js"></script>
    `);

    const form = page.locator('csp-definition-form');
    const sourceInput = form.locator('uui-input[label="Source"]');
    
    // Test invalid input
    await sourceInput.fill('javascript:alert(1)');
    await sourceInput.blur();
    
    const errorMessage = page.locator('.field-error');
    await expect(errorMessage).toContainText('JavaScript protocol not allowed');
  });
});
```

### Page Object Pattern
```typescript
export class CspManagementWorkspacePage {
  constructor(private page: Page) {}

  async navigateTo() {
    await this.page.goto('/umbraco/#/csp-manager/workspace');
    await this.page.waitForSelector('csp-management-workspace');
  }

  async enableCsp() {
    const enabledToggle = this.page.locator('uui-toggle[label="Enabled"]');
    if (!(await enabledToggle.isChecked())) {
      await enabledToggle.click();
    }
  }

  async addSource(directive: string, source: string) {
    await this.page.locator('uui-button[label="Add Source"]').click();
    const newSourceRow = this.page.locator('.source-item').last();
    await newSourceRow.locator('uui-input[label="Directive"]').fill(directive);
    await newSourceRow.locator('uui-input[label="Source"]').fill(source);
  }
}
```

## Test Quality Standards

### Coverage Requirements
- Minimum 80% code coverage for business logic
- 100% coverage for security-critical functions
- All API endpoints must have tests
- All UI components must have component tests

### Performance Testing
```csharp
[Test]
[Category("Performance")]
public async Task GetCspDefinition_Under_HighLoad_PerformsWithinLimits()
{
    const int concurrentRequests = 100;
    var stopwatch = Stopwatch.StartNew();
    
    var tasks = Enumerable.Range(0, concurrentRequests)
        .Select(_ => _cspService.GetCspDefinitionAsync(false));
        
    await Task.WhenAll(tasks);
    stopwatch.Stop();
    
    var averageTime = stopwatch.ElapsedMilliseconds / (double)concurrentRequests;
    Assert.That(averageTime, Is.LessThan(50)); // 50ms per request
}
```

### Security Testing
```csharp
[TestCase("javascript:alert('xss')", false)]
[TestCase("'self'", true)]
[TestCase("<script>alert('xss')</script>", false)]
public void ValidateCspSource_WithVariousInputs_ReturnsExpectedResult(string source, bool expected)
{
    var validator = new CspSourceValidator();
    var result = validator.IsValid(source);
    Assert.That(result, Is.EqualTo(expected));
}
```