FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем контракты и проекты относительно корня репозитория
COPY ["Contracts/CSCourse.Contracts.csproj", "Contracts/"]
COPY ["Events.Service/Presentation/Events.Service.csproj", "Events.Service/Presentation/"]
COPY ["Events.Service/Application/Events.Service.Application.csproj", "Events.Service/Application/"]
COPY ["Events.Service/Domain/Events.Service.Domain.csproj", "Events.Service/Domain/"]
COPY ["Events.Service/Infrastructure/Events.Service.Infrastructure.csproj", "Events.Service/Infrastructure/"]

RUN dotnet restore "Events.Service/Presentation/Events.Service.csproj" --verbosity detailed

COPY .. .
WORKDIR "/src/Events.Service/Presentation"
# RUN dotnet publish "Events.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish "Events.Service.csproj" -c Debug -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Events.Service.dll"]
