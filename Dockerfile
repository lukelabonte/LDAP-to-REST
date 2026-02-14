# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY LdapToRest.sln .
COPY src/LdapToRest/LdapToRest.csproj src/LdapToRest/
COPY tests/LdapToRest.Tests/LdapToRest.Tests.csproj tests/LdapToRest.Tests/
RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish src/LdapToRest/LdapToRest.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install OpenLDAP client library (required by System.DirectoryServices.Protocols)
RUN apt-get update && \
    apt-get install -y --no-install-recommends libldap-2.5-0 && \
    rm -rf /var/lib/apt/lists/*

# Run as non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "LdapToRest.dll"]
