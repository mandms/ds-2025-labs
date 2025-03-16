# Используем официальный образ .NET для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем всё остальное и собираем приложение
COPY . .
RUN dotnet publish -c Release -o /app

# Используем официальный образ .NET для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Копируем собранное приложение
COPY --from=build /app .

# Указываем порты, которые будет использовать приложение
EXPOSE 5001 5002

# Устанавливаем переменную среды для указания URL
ENV ASPNETCORE_URLS=http://0.0.0.0:5001;http://0.0.0.0:5002

# Запускаем приложение
ENTRYPOINT ["dotnet", "Valuator.dll"]