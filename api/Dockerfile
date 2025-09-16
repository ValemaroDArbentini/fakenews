# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .

# Находим веб-проект (Sdk="Microsoft.NET.Sdk.Web") и публикуем именно его
# Если проектов несколько, берём первый найденный — при желании можно зафиксировать именем.
RUN set -e; \
    PROJECT=$(grep -Rl 'Sdk="Microsoft.NET.Sdk.Web"' --include=*.csproj . | head -n1); \
    echo "Publishing project: $PROJECT"; \
    dotnet restore "$PROJECT"; \
    dotnet publish "$PROJECT" -c Release -o /out /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
ENV ASPNETCORE_URLS=http://0.0.0.0:8081
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .

# Старт: выбираем DLL по наличию парного .runtimeconfig.json.
# Если по какой-то причине не найдём — fallback к AspNetCore в deps.json, затем к первому .dll.
RUN printf '%s\n' \
  '#!/bin/sh' \
  'set -e' \
  'cd /app' \
  'APP=$(ls *.runtimeconfig.json 2>/dev/null | head -n1 | sed "s/.runtimeconfig.json$/.dll/")' \
  'if [ -z "$APP" ]; then' \
  '  APP=$(ls *.deps.json 2>/dev/null | xargs -I{} sh -c "grep -q \"Microsoft.AspNetCore.App\" {} && echo {}" | head -n1 | sed \"s/.deps.json$/.dll/\")' \
  'fi' \
  'if [ -z "$APP" ]; then APP=$(ls -1 *.dll | head -n1); fi' \
  'echo "Starting: $APP $*"' \
  'exec dotnet "$APP" "$@"' > /app/start.sh \
  && chmod +x /app/start.sh

EXPOSE 8081
ENTRYPOINT ["/app/start.sh"]
