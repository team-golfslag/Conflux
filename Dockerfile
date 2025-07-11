FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Conflux.API/Conflux.API.csproj", "Conflux.API/"]
RUN dotnet restore "Conflux.API/Conflux.API.csproj"
COPY . .
WORKDIR "/src/Conflux.API"
RUN dotnet build "Conflux.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Conflux.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create Models directory and copy embedding model files
RUN mkdir -p /app/Models
COPY Models/ /app/Models/

# Ensure the app user has read access to the models
USER root
RUN chown -R $APP_UID:$APP_UID /app/Models && chmod -R 755 /app/Models
USER $APP_UID

ENTRYPOINT ["dotnet", "Conflux.API.dll"]