# Dev-friendly Dockerfile (used with docker-compose bind mount)
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# Cache restore with separate copy of csproj when building image
COPY Api.csproj ./
RUN dotnet restore

# Copy the rest (will be overridden by bind mount in dev)
COPY . .

EXPOSE 5000

# Default command (compose overrides with `dotnet watch run`)
CMD ["dotnet", "run", "--urls", "http://0.0.0.0:5000"]

