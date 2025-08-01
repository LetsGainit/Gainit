# Connection String Management

## Local Development

- The real PostgreSQL connection string is **not** stored in `appsettings.json` to keep secrets out of source control.
- For local development, add your actual connection string to `appsettings.Development.json`.
- This file is excluded from Git via `.gitignore`, so your secrets remain private.

## Production / Azure Deployment

- In production (e.g., Azure), set the connection string in the Azure Portal under **Configuration > Connection strings**.
- The application will automatically use the correct connection string based on the environment.

## Summary

- **Never commit real connection strings to the repository.**
- Use `appsettings.Development.json` for local development (not tracked by Git).
- Use environment configuration for production.
