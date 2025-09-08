### Middleware

Cross-cutting concerns executed in the ASP.NET Core pipeline.

Files
- `CorrelationIdMiddleware.cs`: Ensures every request has a correlation ID for tracing.
- `PerformanceMonitoringMiddleware.cs`: Measures request duration and emits metrics/logs.
- `RequestLoggingMiddleware.cs`: Structured request/response logging with correlation.


