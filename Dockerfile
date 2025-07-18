# Stage 1: Build and Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0.300 AS build
WORKDIR /src

# Copy nuget.config
COPY nuget.config .

# Copy csproj and restore dependencies
COPY web/web.csproj web/
RUN dotnet restore web/web.csproj --use-current-runtime --configfile nuget.config --force \
    && dotnet nuget locals all --clear \
    && rm -rf /root/.nuget/packages/* \
    && rm -rf /root/.local/share/NuGet/*

# Copy source code
COPY . .
WORKDIR /src/web

# Install dotnet-ef
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Create migration (only during Docker build)
ARG MIGRATION_NAME=InitialCreate
RUN echo "Создаем миграцию: $MIGRATION_NAME"
RUN dotnet ef migrations add $MIGRATION_NAME --verbose

# Build the project
RUN dotnet build web.csproj -c Release --configfile /src/nuget.config

# Publish the application
RUN dotnet publish web.csproj -c Release -o /app/publish \
    --runtime linux-x64 \
    --self-contained false \
    --configfile /src/nuget.config

# Stage 2: Final Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy and configure entrypoint
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Install MSSQL Tools and curl
RUN apt-get update && apt-get install -y ca-certificates curl gnupg iputils-ping \
    && curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - \
    && curl https://packages.microsoft.com/config/debian/11/prod.list > /etc/apt/sources.list.d/mssql-release.list \
    && apt-get update \
    && ACCEPT_EULA=Y apt-get install -y msodbcsql18 mssql-tools18 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Environment variables
ENV SQLSERVER_HOST=sqlserver
ENV SQLSERVER_PORT=1433
ENV SQLSERVER_USER=sa
ENV SQLSERVER_PASSWORD=StrongPass123!
ENV DATABASE_NAME=sabzor04_db
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV PATH="$PATH:/opt/mssql-tools18/bin"

EXPOSE 80

ENTRYPOINT ["/entrypoint.sh"]