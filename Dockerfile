FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached until a project file changes.
COPY Directory.Build.props WeatherApp.sln ./
COPY src/WeatherApp.Domain/*.csproj src/WeatherApp.Domain/
COPY src/WeatherApp.Application/*.csproj src/WeatherApp.Application/
COPY src/WeatherApp.Infrastructure/*.csproj src/WeatherApp.Infrastructure/
COPY src/WeatherApp.Api/*.csproj src/WeatherApp.Api/
RUN dotnet restore src/WeatherApp.Api/WeatherApp.Api.csproj

COPY src/ src/
RUN dotnet publish src/WeatherApp.Api/WeatherApp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# The aspnet image ships a non-root "app" user.
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Supply the key at runtime: -e OpenWeatherMap__ApiKey=...
ENTRYPOINT ["dotnet", "WeatherApp.Api.dll"]
