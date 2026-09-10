# Build context is the repo root (docker-compose.yml passes `context: .`)
# because this project references SuperFood.Contracts.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SuperFood.Client/SuperFood.Client.csproj src/SuperFood.Client/
COPY src/SuperFood.Contracts/SuperFood.Contracts.csproj src/SuperFood.Contracts/
RUN dotnet restore src/SuperFood.Client/SuperFood.Client.csproj

COPY src/SuperFood.Client/ src/SuperFood.Client/
COPY src/SuperFood.Contracts/ src/SuperFood.Contracts/

RUN dotnet publish src/SuperFood.Client/SuperFood.Client.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine AS runtime

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/SuperFood.Client/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/SuperFood.Client/docker/appsettings.template.json /usr/share/nginx/html/appsettings.template.json
COPY src/SuperFood.Client/docker/generate-appsettings.sh /docker-entrypoint.d/40-generate-appsettings.sh
RUN chmod +x /docker-entrypoint.d/40-generate-appsettings.sh

ENV API_BASE_URL=http://localhost:5080
EXPOSE 80
