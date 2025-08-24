---
targets: ["*"]
description: "Guidelines for generating test files"
globs: ["**/*test*", "**/*spec*"]
---

# Test Generation Guidelines

## Backend Test Generation

### XUnit Test Class Template

```csharp
[TestFixture]
public class [ClassName]Tests : UmbracoIntegrationTest
{
    private I[ServiceName] _service;
    private Mock<I[DependencyName]> _mockDependency;

    [SetUp]
    public void SetUp()
    {
        _mockDependency = new Mock<I[DependencyName]>();
        _service = new [ServiceName](_mockDependency.Object);
    }

    [Test]
    public async Task [MethodName]_With[Scenario]_Returns[ExpectedResult]()
    {
        // Arrange
        var input = CreateTest[InputType]();
        _mockDependency.Setup(x => x.[Method](It.IsAny<[Type]>()))
                      .ReturnsAsync([expected]);

        // Act
        var result = await _service.[MethodName](input);

        // Assert
        Assert.That(result, Is.[Assertion]);
        _mockDependency.Verify(x => x.[Method](input), Times.Once);
    }

    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
    }

    private [InputType] CreateTest[InputType]()
    {
        return [TestDataBuilderPattern];
    }
}
```

### Integration Test Template

```csharp
[TestFixture]
public class [ClassName]IntegrationTests : UmbracoIntegrationTest
{
    [Test]
    public async Task [MethodName]_WithRealDatabase_PersistsCorrectly()
    {
        // Arrange
        var repository = GetRequiredService<I[Repository]>();
        var entity = [TestDataBuilder].Create()
            .With[Property]([value])
            .Build();

        // Act
        await repository.SaveAsync(entity);
        var retrieved = await repository.GetByIdAsync(entity.Id);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.[Property], Is.EqualTo([value]));
    }
}
```

### Verification Test Template

```csharp
[TestFixture]
public class [ClassName]VerificationTests
{
    [Test]
    public Task [MethodName]_WithComplexData_MatchesSnapshot()
    {
        // Arrange
        var input = CreateComplexTestData();

        // Act
        var result = _service.[MethodName](input);

        // Assert
        return Verify(result)
            .UseDirectory("Snapshots")
            .UseFileName("[TestName]");
    }
}
```

## Frontend Test Generation

### Playwright E2E Test Template

```typescript
// tests/[feature].spec.ts
import { test, expect } from '@playwright/test';

test.describe('[Feature] Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/umbraco/#/csp-manager/[route]');
    await page.waitForSelector('[main-selector]');
  });

  test('should [expected behavior] when [scenario]', async ({ page }) => {
    // Given: [setup condition]
    const [element] = page.locator('[selector]');

    // When: [action]
    await [element].[action]();

    // Then: [verification]
    await expect([element]).to[assertion]();
  });

  test('should handle [error scenario] gracefully', async ({ page }) => {
    // Mock error response
    await page.route('**/api/[endpoint]', route =>
      route.fulfill({ status: 500, body: 'Server Error' }));

    // Trigger action that causes error
    await page.locator('[trigger-selector]').click();

    // Verify error handling
    const errorMessage = page.locator('.error-message');
    await expect(errorMessage).toBeVisible();
    await expect(errorMessage).toContainText('[expected-error-text]');
  });
});
```

### Component Test Template

```typescript
// tests/components/[component-name].spec.ts
import { test, expect } from "@playwright/test";

test.describe("[ComponentName] Component", () => {
  test.beforeEach(async ({ page }) => {
    await page.setContent(`
      <[component-name] [attributes]></[component-name]>
      <script type="module" src="/App_Plugins/UmbracoCommunityCSPManager/umbraco-community-csp-manager.js"></script>
    `);
    await page.waitForSelector("[component-name]");
  });

  test("should render correctly with default props", async ({ page }) => {
    const component = page.locator("[component-name]");

    await expect(component).toBeVisible();
    await expect(component).toHaveAttribute("[attribute]", "[expected-value]");
  });

  test("should emit [event] when [action] occurs", async ({ page }) => {
    const component = page.locator("[component-name]");

    // Set up event listener
    const eventPromise = page.waitForEvent("console", (msg) => msg.text().includes("[event-name]"));

    // Trigger action
    await component.locator("[trigger-selector]").click();

    // Verify event
    await eventPromise;
  });

  test("should validate input correctly", async ({ page }) => {
    const component = page.locator("[component-name]");
    const input = component.locator("uui-input");

    // Test invalid input
    await input.fill("[invalid-value]");
    await input.blur();

    const errorMessage = page.locator(".field-error");
    await expect(errorMessage).toContainText("[expected-error]");
  });
});
```

## Security Test Generation

### Backend Security Tests

```csharp
[TestFixture]
[Category("Security")]
public class [ClassName]SecurityTests
{
    [TestCase("[malicious-input]", false)]
    [TestCase("[safe-input]", true)]
    public void [Method]_WithVariousInputs_ValidatesCorrectly(string input, bool expectedValid)
    {
        var validator = new [ValidatorName]();
        var result = validator.IsValid(input);
        Assert.That(result, Is.EqualTo(expectedValid));
    }

    [Test]
    public async Task [Method]_WithoutAuthorization_ThrowsUnauthorizedException()
    {
        // Arrange - unauthorized context
        var unauthorizedContext = CreateUnauthorizedHttpContext();
        var controller = new [ControllerName](_service)
        {
            ControllerContext = new ControllerContext { HttpContext = unauthorizedContext }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => controller.[Method]([parameters]));
    }

    [Test]
    public void [Method]_WithSqlInjectionAttempt_DoesNotExecuteInjection()
    {
        var maliciousInput = "'; DROP TABLE Users; --";

        Assert.DoesNotThrowAsync(async () =>
            await _service.[Method](maliciousInput));

        // Verify table still exists
        Assert.That(TableExists("Users"), Is.True);
    }
}
```

### Frontend Security Tests

```typescript
test.describe("Security Tests", () => {
  test("should prevent XSS in user inputs", async ({ page }) => {
    const maliciousScript = "<script>window.xssExecuted = true;</script>";

    const input = page.locator("uui-input");
    await input.fill(maliciousScript);
    await input.blur();

    // Verify script was not executed
    const xssExecuted = await page.evaluate(() => window.xssExecuted);
    expect(xssExecuted).toBeUndefined();

    // Verify content is properly escaped
    const displayedValue = await page.locator(".display-value").textContent();
    expect(displayedValue).toBe(maliciousScript); // Should be displayed as text
  });

  test("should validate CSP sources against dangerous patterns", async ({ page }) => {
    const dangerousSource = "javascript:alert('xss')";

    const sourceInput = page.locator('uui-input[label="Source"]');
    await sourceInput.fill(dangerousSource);
    await sourceInput.blur();

    const errorMessage = page.locator(".field-error");
    await expect(errorMessage).toContainText("not allowed");
  });
});
```

## Performance Test Generation

### Backend Performance Tests

```csharp
[TestFixture]
[Category("Performance")]
public class [ClassName]PerformanceTests
{
    [Test]
    public async Task [Method]_Under_HighLoad_PerformsWithinLimits()
    {
        // Arrange
        const int concurrentRequests = 100;
        var tasks = new List<Task<[ReturnType]>>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_service.[Method]([parameters]));
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var averageTime = stopwatch.ElapsedMilliseconds / (double)concurrentRequests;
        Assert.That(averageTime, Is.LessThan([threshold])); // ms per request
        Assert.That(tasks.All(t => t.Result != null), Is.True);
    }
}
```
