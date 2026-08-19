FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["SocialNetworkAPI/SocialNetworkAPI.csproj", "SocialNetworkAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

RUN dotnet restore "./SocialNetworkAPI/SocialNetworkAPI.csproj"

COPY . .

WORKDIR "/src/SocialNetworkAPI"

RUN dotnet build \
    "./SocialNetworkAPI.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build


FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish \
    "./SocialNetworkAPI.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

RUN mkdir -p /app/wwwroot/media/avatars /app/Logs \
    && chown -R $APP_UID:$APP_UID /app/wwwroot/media /app/Logs

USER $APP_UID

ENTRYPOINT ["dotnet", "SocialNetworkAPI.dll"]