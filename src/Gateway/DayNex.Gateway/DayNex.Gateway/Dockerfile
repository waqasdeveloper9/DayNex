FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["global.json", "Directory.Packages.props", "./"]
COPY ["src/Gateway/DayNex.Gateway/DayNex.Gateway/DayNex.Gateway.csproj", "src/Gateway/DayNex.Gateway/DayNex.Gateway/"]
COPY ["DayNex.Shared.Http/DayNex.Shared.Http.csproj", "DayNex.Shared.Http/"]
COPY ["src/Shared/DayNex.Contracts/DayNex.Contracts.csproj", "src/Shared/DayNex.Contracts/"]
RUN dotnet restore "src/Gateway/DayNex.Gateway/DayNex.Gateway/DayNex.Gateway.csproj"
COPY . .
RUN dotnet publish "src/Gateway/DayNex.Gateway/DayNex.Gateway/DayNex.Gateway.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DayNex.Gateway.dll"]