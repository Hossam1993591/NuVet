# NuVet Demo

This directory contains example projects to demonstrate NuVet's capabilities.

## Running the Demo

1. **Build NuVet**:
   ```bash
   cd ..
   dotnet build
   ```

2. **Run a scan**:
   ```bash
   dotnet run --project ../NuVet.CLI -- scan .
   ```

3. **Try the update command** (dry run):
   ```bash
   dotnet run --project ../NuVet.CLI -- update . --dry-run
   ```

4. **Analyze dependencies**:
   ```bash
   dotnet run --project ../NuVet.CLI -- analyze .
   ```

## Expected Results

This demo project intentionally uses older, potentially vulnerable packages to demonstrate NuVet's scanning capabilities. You should see:

- Vulnerability warnings for outdated packages
- Suggestions for safe update versions
- Detailed dependency analysis

## Example Output

```
────────────────── NuVet - Vulnerability Scanner ──────────────────

Scanning: ./DemoApp.csproj
Min Severity: Low
Include Transitive: True

┌───────────────────────┬───────┐
│ Metric                │ Count │
├───────────────────────┼───────┤
│ Projects Scanned      │ 1     │
│ Packages Analyzed     │ 5     │
│ Vulnerable Packages   │ 2     │
│ Total Vulnerabilities │ 3     │
└───────────────────────┴───────┘
```