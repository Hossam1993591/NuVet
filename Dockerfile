# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["NuVet.Core/NuVet.Core.csproj", "NuVet.Core/"]
COPY ["NuVet.Tests/NuVet.Tests.csproj", "NuVet.Tests/"]
COPY ["Directory.Build.props", "./"]

# Restore dependencies
RUN dotnet restore "NuVet.Core/NuVet.Core.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/NuVet.Core"
RUN dotnet build "NuVet.Core.csproj" -c Release -o /app/build

# Test stage
FROM build AS test
WORKDIR /src
RUN dotnet test "NuVet.Tests/NuVet.Tests.csproj" -c Release --logger trx --results-directory /testresults

# Publish stage
FROM build AS publish
RUN dotnet publish "NuVet.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r nuvet && useradd -r -g nuvet nuvet

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R nuvet:nuvet /app
USER nuvet

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD echo "Health check not implemented yet"

# Default command
ENTRYPOINT ["dotnet", "NuVet.Core.dll"]