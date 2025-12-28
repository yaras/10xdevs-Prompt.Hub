# AI Rules for PromptHub

PromptHub is a web applicaton offering storage and sharing of AI prompts in a collaborative environment. This document outlines the specific coding practices, architectural guidelines, and other standards to be followed when contributing to the PromptHub codebase.

## CODING_PRACTICES

### Guidelines for SUPPORT_LEVEL

#### SUPPORT_EXPERT

- Favor elegant, maintainable solutions over verbose code. Assume understanding of language idioms and design patterns.
- Highlight potential performance implications and optimization opportunities in suggested code.
- Frame solutions within broader architectural contexts and suggest design alternatives when appropriate.
- Focus comments on 'why' not 'what' - assume code readability through well-named functions and variables.
- Proactively address edge cases, race conditions, and security considerations without being prompted.
- When debugging, provide targeted diagnostic approaches rather than shotgun solutions.
- Suggest comprehensive testing strategies rather than just example tests, including considerations for mocking, test organization, and coverage.

### Guidelines for DOCUMENTATION

#### DOC_UPDATES

- Update relevant documentation in /docs when modifying features
- Keep README.md in sync with new capabilities
- Maintain changelog entries in CHANGELOG.md

### Guidelines for ARCHITECTURE

#### CLEAN_ARCHITECTURE

- Strictly separate code into layers: entities, features, interfaces, and frameworks
- Ensure dependencies point inward, with inner layers having no knowledge of outer layers
- Implement domain entities that encapsulate business_rules without framework dependencies
- Use IoC isolate external dependencies
- Create features that orchestrate entity interactions for specific business operations
- Implement mappers to transform data between layers to maintain separation of concerns

## BACKEND

### Guidelines for BLAZOR_SERVER

#### Blazor Server Application

- Use Blazor Server Application for the backend and frontend
- Implement proper exception handling to provide consistent error responses
- Use dependency injection with scoped lifetime for request-specific services and singleton for stateless services

## DATABASE

### Guidelines for AZURE_TABLE_STORAGE

#### Core Principles
- Use Azure Table Storage for storing large volumes of structured, non-relational data
- Design tables with partition keys and row keys to optimize query performance
- Implement soft delete by adding an "IsDeleted" boolean property to entities

#### Query Patterns
- Avoid table scans; design partition keys for query access patterns
- Use continuation tokens for pagination (don't load all entities)
- Implement exponential backoff for retry logic on throttling (HTTP 429)

#### Entity Design
- Keep entities under 1MB; use blob storage for large content
- Denormalize data appropriately for read performance
- Version entities with an ETag property for optimistic concurrency

## FRONTEND

### Guidelines for BLAZOR_COMPONENTS

#### Component Structure
- Use MudBlazor for UI components
- Prefer code-behind (.razor.cs) for complex component logic
- Use @inject for dependency injection in components
- Implement IDisposable for components with subscriptions/timers
- Use cascading parameters sparingly; prefer state management patterns

#### State Management
- Use scoped services for component-to-component communication
- Implement a centralized state pattern for complex state using Fluxor
- Avoid static state; leverage DI container lifecycle

#### Rendering Performance
- Use @key directive for dynamic lists to optimize diff algorithm
- Implement ShouldRender() override judiciously for expensive renders
- Prefer EventCallback<T> over Action<T> for component events

## SECURITY

### Guidelines for AUTHENTICATION
- Use Azure Entra ID with Microsoft.Identity.Web
- Implement authorization policies for feature access
- Validate user context in every service/feature method

### Guidelines for DATA_PROTECTION
- Sanitize user input to prevent XSS attacks
- Use parameterized queries/proper SDK methods to prevent injection
- Implement content security policy (CSP) headers

### Guidelines for ERROR_HANDLING
- Use ErrorBoundary component for graceful UI error handling
- Log errors with structured logging (ILogger<T>)
- Never expose stack traces or sensitive data to end users

## DEVOPS

### Guidelines for CI_CD

#### Azure DevOps Pipelines

- Use YAML pipelines for infrastructure as code
- Implement stages for build, test, and deploy

### Guidelines for CLOUD

#### AZURE

- Use Azure Resource Manager (ARM) templates or Bicep for infrastructure as code
- Implement Azure Entra AD for authentication and authorization
- Implement role-based access control (RBAC) for resource management
- Use Azure AppInsights for service monitoring and logging

## TESTING

### Guidelines for UNIT_TESTING

- Use xUnit with FluentAssertions for readable assertions
- Leverage mock functions and spies for isolating units of code
- Implement test setup and teardown
- Use describe blocks for organizing related tests
- Implement code coverage reporting with meaningful targets
- Mock Azure Table Storage with in-memory implementations
- Test Blazor components using bUnit framework
- Aim for >80% code coverage on business logic (features/entities)

### Guidelines for INTEGRATION_TESTING

- Use WebApplicationFactory for end-to-end Blazor tests
- Test against Azure Storage Emulator/Azurite for local dev