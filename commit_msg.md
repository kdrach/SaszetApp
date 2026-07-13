## Summary
- Reordered try-catch blocks in ScanController to begin immediately after checking usage quota.
- Enforced CancellationToken.None in all relevant DbContext operations (ScanController, AdminProviderController, AdminSettingsController).
- Hardcoded CancellationToken.None in RefundUsageAsync critical sections inside ScanQuotaService.

## Scope
v13/v14 concurrency fixes and Database Idempotency

## Validation
- dotnet test
