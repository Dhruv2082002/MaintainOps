FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/MaintainOps.Domain/MaintainOps.Domain.csproj src/MaintainOps.Domain/
COPY src/MaintainOps.Application/MaintainOps.Application.csproj src/MaintainOps.Application/
COPY src/MaintainOps.Infrastructure/MaintainOps.Infrastructure.csproj src/MaintainOps.Infrastructure/
COPY src/MaintainOps.Api/MaintainOps.Api.csproj src/MaintainOps.Api/
RUN dotnet restore src/MaintainOps.Api/MaintainOps.Api.csproj

COPY src/ src/
RUN dotnet publish src/MaintainOps.Api/MaintainOps.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MaintainOps.Api.dll"]
