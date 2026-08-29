FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AIPoweredDevOps.sln ./
COPY src/DevOpsAutomation.Core/*.csproj src/DevOpsAutomation.Core/
COPY src/DevOpsAutomation.Infrastructure/*.csproj src/DevOpsAutomation.Infrastructure/
COPY src/DevOpsAutomation.Testing/*.csproj src/DevOpsAutomation.Testing/
COPY src/DevOpsAutomation.Api/*.csproj src/DevOpsAutomation.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/DevOpsAutomation.Api/DevOpsAutomation.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DevOpsAutomation.Api.dll"]
