FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5286

ENV ASPNETCORE_URLS=http://+:5286

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/MaintainOps.Domain/MaintainOps.Domain.csproj", "src/MaintainOps.Domain/"]
COPY ["src/MaintainOps.Application/MaintainOps.Application.csproj", "src/MaintainOps.Application/"]
COPY ["src/MaintainOps.Infrastructure/MaintainOps.Infrastructure.csproj", "src/MaintainOps.Infrastructure/"]
COPY ["src/MaintainOps.Api/MaintainOps.Api.csproj", "src/MaintainOps.Api/"]

RUN dotnet restore "src/MaintainOps.Api/MaintainOps.Api.csproj"
COPY . .
WORKDIR "/src/src/MaintainOps.Api"
RUN dotnet build "MaintainOps.Api.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "MaintainOps.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MaintainOps.Api.dll"]
