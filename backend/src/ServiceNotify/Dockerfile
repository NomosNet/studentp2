FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY src/StudentPass.Contracts/StudentPass.Contracts.csproj StudentPass.Contracts/
COPY src/ServiceNotify/ServiceNotify.csproj ServiceNotify/
RUN dotnet restore ServiceNotify/ServiceNotify.csproj --configfile nuget.config
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
