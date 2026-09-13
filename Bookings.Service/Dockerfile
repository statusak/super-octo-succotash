FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем контракты и проекты относительно корня репозитория
COPY ["Contracts/CSCourse.Contracts.csproj", "Contracts/"]
COPY ["Bookings.Service/Presentation/Bookings.Service.csproj", "Bookings.Service/Presentation/"]
COPY ["Bookings.Service/Application/Bookings.Service.Application.csproj", "Bookings.Service/Application/"]
COPY ["Bookings.Service/Domain/Bookings.Service.Domain.csproj", "Bookings.Service/Domain/"]
COPY ["Bookings.Service/Infrastructure/Bookings.Service.Infrastructure.csproj", "Bookings.Service/Infrastructure/"]

RUN dotnet restore "Bookings.Service/Presentation/Bookings.Service.csproj" --verbosity detailed

COPY .. .
WORKDIR "/src/Bookings.Service/Presentation"
RUN dotnet publish "Bookings.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bookings.Service.dll"]
