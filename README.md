Self-contained test solution that demonstrates regression in System.ServiceModel.NetNamedPipe 8.1.2 when compared against System.ServiceModel in .NET Framework 4.8.1.

Corresponding issue tracked at https://github.com/dotnet/wcf/issues/5977

The solution contains:

- `ReproLib` — shared service contract/types
- `ReproService` — hosts the `net.pipe` endpoint
- `ReproClient48` — runs concurrent client calls against the named pipe endpoint (net48)
- `ReproClient80` — runs concurrent client calls against the named pipe endpoint (net8.0-windows)

With the same `ReproService` process running, the `net48` client succeeds reliably on my machine, while the `net8.0-windows` client often fails with a `CommunicationException` during concurrent connection/call attempts.
