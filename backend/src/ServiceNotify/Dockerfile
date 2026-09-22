FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY src/StudentPass.Contracts/StudentPass.Contracts.csproj StudentPass.Contracts/
COPY src/ServiceNotify/ServiceNotify.csproj ServiceNotify/
ARG GITHUB_USERNAME=""
ARG GITHUB_PAT=""
RUN if [ -z "$GITHUB_PAT" ]; then \
      echo "Set GITHUB_USERNAME and GITHUB_PAT (GitHub PAT with read:packages) so the Mailtrap SDK can be restored from GitHub Packages."; \
      exit 1; \
    fi \
    && dotnet nuget update source github-mailtrap \
      --username "$GITHUB_USERNAME" \
      --password "$GITHUB_PAT" \
      --store-password-in-clear-text \
      --configfile nuget.config \
    && dotnet restore ServiceNotify/ServiceNotify.csproj --configfile nuget.config
COPY src/ .
RUN dotnet publish ServiceNotify/ServiceNotify.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ServiceNotify.dll"]

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ServiceNotify.dll"]
