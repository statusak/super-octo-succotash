FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем контракты и проекты относительно корня репозитория
COPY ["Contracts/CSCourse.Contracts.csproj", "Contracts/"]
COPY ["Identity.Service/Presentation/Identity.Service.csproj", "Identity.Service/Presentation/"]
COPY ["Identity.Service/Application/Identity.Service.Application.csproj", "Identity.Service/Application/"]
COPY ["Identity.Service/Domain/Identity.Service.Domain.csproj", "Identity.Service/Domain/"]
COPY ["Identity.Service/Infrastructure/Identity.Service.Infrastructure.csproj", "Identity.Service/Infrastructure/"]

RUN dotnet restore "Identity.Service/Presentation/Identity.Service.csproj" --verbosity detailed

COPY .. .
WORKDIR "/src/Identity.Service/Presentation"
RUN dotnet publish "Identity.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identity.Service.dll"]
