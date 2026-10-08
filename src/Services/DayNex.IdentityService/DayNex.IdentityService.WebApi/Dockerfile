FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["global.json", "Directory.Packages.props", "./"]
COPY ["src/Services/DayNex.IdentityService/DayNex.IdentityService.WebApi/DayNex.IdentityService.WebApi.csproj", "src/Services/DayNex.IdentityService/DayNex.IdentityService.WebApi/"]
COPY ["src/Services/DayNex.IdentityService/DayNex.IdentityService.Infrastructure/DayNex.IdentityService.Infrastructure.csproj", "src/Services/DayNex.IdentityService/DayNex.IdentityService.Infrastructure/"]
COPY ["src/Services/DayNex.IdentityService/DayNex.IdentityService.Application/DayNex.IdentityService.Application.csproj", "src/Services/DayNex.IdentityService/DayNex.IdentityService.Application/"]
COPY ["src/Services/DayNex.IdentityService/DayNex.IdentityService.Domain/DayNex.IdentityService.Domain.csproj", "src/Services/DayNex.IdentityService/DayNex.IdentityService.Domain/"]
COPY ["DayNex.Shared.Http/DayNex.Shared.Http.csproj", "DayNex.Shared.Http/"]
COPY ["src/Shared/DayNex.Contracts/DayNex.Contracts.csproj", "src/Shared/DayNex.Contracts/"]
COPY ["src/Shared/DayNex.Domain/DayNex.Domain.csproj", "src/Shared/DayNex.Domain/"]
RUN dotnet restore "src/Services/DayNex.IdentityService/DayNex.IdentityService.WebApi/DayNex.IdentityService.WebApi.csproj"
COPY . .
RUN dotnet publish "src/Services/DayNex.IdentityService/DayNex.IdentityService.WebApi/DayNex.IdentityService.WebApi.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DayNex.IdentityService.WebApi.dll"]