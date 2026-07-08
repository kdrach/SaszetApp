## Summary
- Split Keycloak into admin and customer realms.
- Implemented custom Tailwind theme for customer realm.
- Added generic SMTP environment variables.
- Updated backend API to validate JWTs from both realms with specific Auth policies.

## Scope
Closes #57
Closes #58
Closes #59
Closes #60
Closes #61

## Validation
- `dotnet build backend\SaszetApp.Api\SaszetApp.Api.csproj`
