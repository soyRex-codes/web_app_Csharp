FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["web_app_Csharp/web_app_Csharp.csproj", "web_app_Csharp/"]
RUN dotnet restore "web_app_Csharp/web_app_Csharp.csproj"
COPY . .
WORKDIR "/src/web_app_Csharp"
RUN dotnet build "./web_app_Csharp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./web_app_Csharp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "web_app_Csharp.dll"]
