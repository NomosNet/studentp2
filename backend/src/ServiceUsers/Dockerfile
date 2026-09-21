FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY src/StudentPass.Contracts/StudentPass.Contracts.csproj StudentPass.Contracts/
COPY src/ServiceUsers/ServiceUsers.csproj ServiceUsers/
RUN dotnet restore ServiceUsers/ServiceUsers.csproj --configfile nuget.config
COPY src/ .
RUN dotnet publish ServiceUsers/ServiceUsers.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000
ENTRYPOINT ["dotnet", "ServiceUsers.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000
ENTRYPOINT ["dotnet", "ServiceUsers.dll"]
