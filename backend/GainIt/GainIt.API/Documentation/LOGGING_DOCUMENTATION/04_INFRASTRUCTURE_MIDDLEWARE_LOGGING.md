# Infrastructure and Middleware Logging Implementation

## Overview
Comprehensive logging has been implemented across the infrastructure layer including database operations, middleware components, health checks, and application lifecycle events. This logging provides visibility into system health, performance, and operational status.

## Files with Logging

### 1. `Data/GainItDbContext.cs`

#### **Database Operation Logging**
**Methods with Logging:**
- `SaveChangesAsync()`
- `SaveChanges()`
- `OnConfiguring()`
- `Dispose()`
- `DisposeAsync()`
- `OnModelCreating()`

**Logging Examples:**
```csharp
// SaveChangesAsync
r_logger.LogInformation("Starting database save operation: ChangeCount={ChangeCount}", changeCount);
r_logger.LogInformation("Database save completed successfully: ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);
r_logger.LogError(ex, "Database save failed: ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);

// SaveChanges (sync)
r_logger.LogInformation("Starting database save operation (sync): ChangeCount={ChangeCount}", changeCount);
r_logger.LogInformation("Database save completed successfully (sync): ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);
r_logger.LogError(ex, "Database save failed (sync): ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);

// Configuration
r_logger.LogWarning("DbContext not configured, using default configuration");

// Disposal
r_logger.LogDebug("Disposing GainItDbContext");
r_logger.LogDebug("Disposing GainItDbContext asynchronously");

// Model creation
r_logger.LogDebug("Configuring database model");
```

**Key Log Fields:**
- `ChangeCount`: Number of entities being saved/modified
- `Duration`: Time taken for database operations
- `State`: Entity state (Added, Modified, Deleted)

### 2. `Middleware/RequestLoggingMiddleware.cs`

#### **Request Lifecycle Logging**
**Methods with Logging:**
- `InvokeAsync()`

**Logging Examples:**
```csharp
// Request start
r_logger.LogInformation("Request started: {RequestId} {Method} {Path} from {RemoteIP}", 
    requestId, context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);

// Request completion
r_logger.LogInformation("Request completed: {RequestId} {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
    requestId, context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);

// Request failure
r_logger.LogError(ex, "Request failed: {RequestId} {Method} {Path} - {ErrorMessage}",
    requestId, context.Request.Method, context.Request.Path, ex.Message);
```

**Key Log Fields:**
- `RequestId`: Unique identifier for request tracing
- `Method`: HTTP method (GET, POST, PUT, DELETE)
- `Path`: Request path
- `RemoteIP`: Client IP address
- `StatusCode`: HTTP response status code
- `Duration`: Request processing time in milliseconds

### 3. `Middleware/CorrelationIdMiddleware.cs`

#### **Correlation ID Management**
**Purpose:** Ensures every request has a unique correlation ID for tracing across components.

**Implementation:**
- Generates correlation ID if not present
- Adds correlation ID to response headers
- Enables end-to-end request tracing

### 4. `Middleware/PerformanceMonitoringMiddleware.cs`

#### **Performance Monitoring**
**Purpose:** Tracks request performance and identifies bottlenecks.

**Implementation:**
- Measures request processing time
- Logs performance metrics
- Identifies slow requests

### 5. `HealthChecks/DatabaseHealthCheck.cs`

#### **Database Health Monitoring**
**Methods with Logging:**
- `CheckHealthAsync()`

**Logging Examples:**
```csharp
// Health check start
r_logger.LogDebug("Starting database health check");

// Health check success
r_logger.LogDebug("Database health check completed successfully");

// Health check failure
r_logger.LogError(ex, "Database health check failed");
```

### 6. `Program.cs`

#### **Application Lifecycle Logging**
**Logging Examples:**
```csharp
// Application startup
Log.Information("Starting GainIt.API application...");

// Configuration
Log.Information("Configuring services...");
Log.Information("Configuring middleware...");
Log.Information("Application configured successfully");

// Application shutdown
Log.Information("Shutting down GainIt.API application...");
```

## Log Levels Used

- **Information**: Application lifecycle, request processing, database operations
- **Warning**: Configuration issues, performance warnings
- **Error**: Exceptions, failures, health check failures
- **Debug**: Detailed technical information, health check details

## Key Log Fields

### **Request Identification**
- `RequestId`: Unique identifier for request tracing
- `CorrelationId`: Correlation ID for cross-component tracing
- `Method`: HTTP method
- `Path`: Request path
- `RemoteIP`: Client IP address

### **Performance Metrics**
- `Duration`: Time taken for operations
- `ChangeCount`: Number of database changes
- `StatusCode`: HTTP response status
- `ElapsedMilliseconds`: Request processing time

### **Database Context**
- `ChangeCount`: Number of entities being saved
- `EntityState`: State of entities (Added, Modified, Deleted)
- `ConnectionString`: Database connection information

### **System Context**
- `Environment`: Application environment (Development, Staging, Production)
- `Configuration`: Configuration source and values
- `ServiceName`: Name of the service being configured

## Azure App Insights Benefits

### 1. **System Health Monitoring**
- Track database operation performance
- Monitor request processing times
- Identify system bottlenecks
- Monitor health check status

### 2. **Performance Analytics**
- Request duration analysis
- Database operation timing
- Middleware performance impact
- System resource utilization

### 3. **Operational Intelligence**
- Request volume patterns
- Error rate monitoring
- Performance trend analysis
- System availability tracking

### 4. **Infrastructure Monitoring**
- Database connection health
- Middleware component status
- Configuration validation
- Service dependency health

### 5. **Troubleshooting Support**
- Request tracing across components
- Performance bottleneck identification
- Error correlation and context
- System state monitoring

## Example Log Output

### Successful Request Processing
```
[Information] Request started: req-12345 GET /api/users/gainer/12345/profile from 192.168.1.100
[Information] Request completed: req-12345 GET /api/users/gainer/12345/profile - Status: 200 - Duration: 156ms
```

### Database Operation
```
[Information] Starting database save operation: ChangeCount=3
[Information] Database save completed successfully: ChangeCount=3, Duration=45.2ms
```

### Health Check
```
[Debug] Starting database health check
[Debug] Database health check completed successfully
```

### Application Startup
```
[Information] Starting GainIt.API application...
[Information] Configuring services...
[Information] Configuring middleware...
[Information] Application configured successfully
```

## Monitoring Queries for Azure App Insights

### Request Performance Monitoring
```
requests
| summarize avg(duration), count() by bin(timestamp, 1h)
```

### Database Performance Monitoring
```
traces
| where message contains "Database save completed successfully"
| summarize avg(customDimensions.Duration), avg(customDimensions.ChangeCount) by bin(timestamp, 1h)
```

### Error Rate Monitoring
```
exceptions
| summarize count() by bin(timestamp, 1h)
```

### Health Check Monitoring
```
traces
| where message contains "health check"
| summarize count() by bin(timestamp, 1h)
```

### Request Volume Monitoring
```
requests
| summarize count() by bin(timestamp, 1h)
```

## Best Practices Implemented

1. **Structured Logging**: All logs use structured parameters for better querying
2. **Request Tracing**: Correlation IDs enable end-to-end request tracking
3. **Performance Metrics**: Comprehensive timing information for all operations
4. **Health Monitoring**: Regular health checks for system components
5. **Error Context**: Full context for debugging and troubleshooting
6. **Lifecycle Tracking**: Application startup, configuration, and shutdown logging
7. **Middleware Integration**: Consistent logging across all middleware components

## Configuration Recommendations

### **Production Environment**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "GainIt.API": "Information"
    }
  }
}
```

### **Development Environment**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "GainIt.API": "Debug"
    }
  }
}
```

### **Health Check Configuration**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "health" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "self" });
```

This logging implementation provides comprehensive visibility into infrastructure operations, enabling effective monitoring, debugging, and operational intelligence when deployed to Azure Web App with Application Insights. 