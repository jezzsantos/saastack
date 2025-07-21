When generating unit tests for C# code, always use xUnit framework with the following patterns:
- Use [Fact] attribute for simple tests
- Do not use [Theory] and [InlineData] for parameterized tests
- Use FluentAssertions for assertions
- Use Moq for mocking any dependencies
- Follow Arrange-Act-Assert pattern, but do not insert comments in the code
- Only test methods that are public or internal

When generating code, always use the latest C# features and best practices.
- Avoid using outdated patterns or libraries.
- Ensure that the code is clean, well-structured, and follows SOLID principles.
- Use meaningful variable and method names, and keep methods short and focused on a single responsibility.
- Include examples of code found in the /docs directory of the repository.