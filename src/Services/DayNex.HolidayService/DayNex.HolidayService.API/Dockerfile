FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["global.json", "Directory.Packages.props", "./"]
COPY ["src/Services/DayNex.HolidayService/DayNex.HolidayService.API/DayNex.HolidayService.API.csproj", "src/Services/DayNex.HolidayService/DayNex.HolidayService.API/"]
COPY ["src/Services/DayNex.HolidayService/DayNex.HolidayService.Infrastructure/DayNex.HolidayService.Infrastructure.csproj", "src/Services/DayNex.HolidayService/DayNex.HolidayService.Infrastructure/"]
COPY ["src/Services/DayNex.HolidayService/DayNex.HolidayService.Application/DayNex.HolidayService.Application.csproj", "src/Services/DayNex.HolidayService/DayNex.HolidayService.Application/"]
COPY ["src/Services/DayNex.HolidayService/DayNex.HolidayService.Domain/DayNex.HolidayService.Domain.csproj", "src/Services/DayNex.HolidayService/DayNex.HolidayService.Domain/"]
COPY ["DayNex.Shared.Http/DayNex.Shared.Http.csproj", "DayNex.Shared.Http/"]
COPY ["src/Shared/DayNex.Contracts/DayNex.Contracts.csproj", "src/Shared/DayNex.Contracts/"]
COPY ["src/Shared/DayNex.Domain/DayNex.Domain.csproj", "src/Shared/DayNex.Domain/"]
RUN dotnet restore "src/Services/DayNex.HolidayService/DayNex.HolidayService.API/DayNex.HolidayService.API.csproj"
COPY . .
RUN dotnet publish "src/Services/DayNex.HolidayService/DayNex.HolidayService.API/DayNex.HolidayService.API.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DayNex.HolidayService.API.dll"]