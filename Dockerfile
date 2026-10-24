# syntax=docker/dockerfile:1
#
# Restore needs the private Nur-tricenter feed (shared kernel). The credentials are BuildKit
# secrets, so they exist only while the restore step runs and never land in an image layer:
#
#   docker build --secret id=nuget_username,env=NUGET_USERNAME \
#                --secret id=nuget_token,env=NUGET_TOKEN \
#                -t aguerreroc/nurtricenter-ms5:0.2.0 .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.config                                                      ./
COPY contracts/Nurtricenter.Contracts.csproj                          contracts/
COPY src/Nurtricenter.Api/Nurtricenter.Api.csproj                      src/Nurtricenter.Api/
COPY src/Nurtricenter.Application/Nurtricenter.Application.csproj      src/Nurtricenter.Application/
COPY src/Nurtricenter.Core/Nurtricenter.Core.csproj                    src/Nurtricenter.Core/
COPY src/Nurtricenter.Infrastructure/Nurtricenter.Infrastructure.csproj src/Nurtricenter.Infrastructure/
COPY src/Nurtricenter.WorkerService/Nurtricenter.WorkerService.csproj  src/Nurtricenter.WorkerService/

RUN --mount=type=secret,id=nuget_username,env=NUGET_USERNAME \
    --mount=type=secret,id=nuget_token,env=NUGET_TOKEN \
    dotnet restore src/Nurtricenter.Api/Nurtricenter.Api.csproj \
 && dotnet restore src/Nurtricenter.WorkerService/Nurtricenter.WorkerService.csproj

COPY contracts/ contracts/
COPY src/ src/

RUN dotnet publish src/Nurtricenter.Api/Nurtricenter.Api.csproj \
    --configuration Release \
    --output /app/publish/api \
    --no-restore \
    /p:UseAppHost=false

RUN dotnet publish src/Nurtricenter.WorkerService/Nurtricenter.WorkerService.csproj \
    --configuration Release \
    --output /app/publish/worker \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish/api .
COPY --from=build /app/publish/worker worker/

# The worker is started with:
#   command: ["dotnet", "worker/Nurtricenter.WorkerService.dll"]

# Required at run time (pass via `docker run -e` or compose `environment:`):
#   ConnectionStrings__DefaultConnection            -> PostgreSQL connection string
#   ClinicService__BaseUrl                          -> external clinic API base URL
#   branchCoordinates__latitude / __longitude       -> starting point coordinates
#   ServiceRegistration__ServiceDiscoveryAddress    -> Consul agent URL (blank = no registration)
#   ServiceRegistration__ServiceName / ServiceId / Host / Port -> Consul registration identity
#   RabbitMqSettings__Host / UserName / Password / VirtualHost   -> broker (host port 5672)

ENTRYPOINT ["dotnet", "Nurtricenter.Api.dll"]
