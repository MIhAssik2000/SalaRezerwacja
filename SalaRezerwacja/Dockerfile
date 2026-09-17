# Этап 1: сборка проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала только csproj — кэшируем restore отдельным слоем
COPY ["SalaRezerwacja.csproj", "./"]
RUN dotnet restore "SalaRezerwacja.csproj"

# Теперь весь остальной код
COPY . .
RUN dotnet publish "SalaRezerwacja.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 2: финальный образ — только рантайм
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SalaRezerwacja.dll"]
