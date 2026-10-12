# OpenEhr

OpenEhr is a .NET 10 NuGet client for this EHR deployment's REST and AQL APIs. It retains the existing `EhrConnection` facade for current consumers and adds a typed lifecycle state query used by CaseRepair.

## Changes in 4.1 preview

- AQL request failures raise `EhrApiException`. A response without `resultSet` raises `EhrContractException`.
- AQL literal binding leaves source queries unchanged, rejects missing or unused parameters, quotes strings, and converts offset-aware times to UTC.
- A result reaching the server's 10,000-row AQL limit raises an error so callers cannot treat a truncated page as complete.
- `EhrConnection` accepts an externally owned `HttpClient` and provides cancellation-aware overloads for delete, restore, tags, and lifecycle status.
- Corrected the composition-children parameter name and the `Link` mapping.

The server's AQL parameter transport, deleted-state visibility, and restore/version behavior must be checked against the target EHR deployment before applying repairs to live cases. CaseRepair includes HTTP contract and interruption tests for the client.

Package ID: `OpenEhr`. Build with `dotnet pack -c Release`. The source package and release notes should be tagged after integration checks.
