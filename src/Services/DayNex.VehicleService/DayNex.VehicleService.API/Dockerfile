FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["global.json", "Directory.Packages.props", "./"]
COPY ["src/Services/DayNex.VehicleService/DayNex.VehicleService.API/DayNex.VehicleService.API.csproj", "src/Services/DayNex.VehicleService/DayNex.VehicleService.API/"]
COPY ["src/Services/DayNex.VehicleService/DayNex.VehicleService.Infrastructure/DayNex.VehicleService.Infrastructure.csproj", "src/Services/DayNex.VehicleService/DayNex.VehicleService.Infrastructure/"]
COPY ["src/Services/DayNex.VehicleService/DayNex.VehicleService.Application/DayNex.VehicleService.Application.csproj", "src/Services/DayNex.VehicleService/DayNex.VehicleService.Application/"]
COPY ["src/Services/DayNex.VehicleService/DayNex.VehicleService.Domain/DayNex.VehicleService.Domain.csproj", "src/Services/DayNex.VehicleService/DayNex.VehicleService.Domain/"]
COPY ["DayNex.Shared.Http/DayNex.Shared.Http.csproj", "DayNex.Shared.Http/"]
COPY ["src/Shared/DayNex.Contracts/DayNex.Contracts.csproj", "src/Shared/DayNex.Contracts/"]
COPY ["src/Shared/DayNex.Domain/DayNex.Domain.csproj", "src/Shared/DayNex.Domain/"]
RUN dotnet restore "src/Services/DayNex.VehicleService/DayNex.VehicleService.API/DayNex.VehicleService.API.csproj"
COPY . .
RUN dotnet publish "src/Services/DayNex.VehicleService/DayNex.VehicleService.API/DayNex.VehicleService.API.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DayNex.VehicleService.API.dll"]