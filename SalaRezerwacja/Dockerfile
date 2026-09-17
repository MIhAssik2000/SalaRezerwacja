# Этап 1: сборка проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала копируем только csproj — так Docker закэширует restore,
# и повторные сборки после правки кода будут быстрее.
COPY ["SalaRezerwacja/SalaRezerwacja.csproj", "SalaRezerwacja/"]
RUN dotnet restore "SalaRezerwacja/SalaRezerwacja.csproj"

# Теперь копируем всё остальное и собираем.
COPY . .
WORKDIR "/src/SalaRezerwacja"
RUN dotnet publish "SalaRezerwacja.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 2: финальный образ — только рантайм, без SDK (он весит меньше)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SalaRezerwacja.dll"]
