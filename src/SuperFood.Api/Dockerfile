# Build context is the repo root (docker-compose.yml passes `context: .`)
# because this project references sibling projects (Domain, Contracts,
# Infrastructure) that live outside src/SuperFood.Api itself.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SuperFood.Api/SuperFood.Api.csproj src/SuperFood.Api/
COPY src/SuperFood.Domain/SuperFood.Domain.csproj src/SuperFood.Domain/
COPY src/SuperFood.Contracts/SuperFood.Contracts.csproj src/SuperFood.Contracts/
COPY src/SuperFood.Infrastructure/SuperFood.Infrastructure.csproj src/SuperFood.Infrastructure/
RUN dotnet restore src/SuperFood.Api/SuperFood.Api.csproj

COPY src/SuperFood.Api/ src/SuperFood.Api/
COPY src/SuperFood.Domain/ src/SuperFood.Domain/
COPY src/SuperFood.Contracts/ src/SuperFood.Contracts/
COPY src/SuperFood.Infrastructure/ src/SuperFood.Infrastructure/

RUN dotnet publish src/SuperFood.Api/SuperFood.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SuperFood.Api.dll"]
