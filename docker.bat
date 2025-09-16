@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

\:: === КОНФИГУРАЦИЯ ===
set PROJECT\_NAME=fakenews
set COMPOSE\_FILE=docker-compose.yml
set ENV\_FILE=.env

\:menu
cls
echo ===========================================
echo    Docker-меню управления проектом: %PROJECT\_NAME%
echo ===========================================
echo.
echo    \[1] Build          - Сборка образов

echo    \[2] Up             - Запуск контейнеров

echo    \[3] Down           - Остановка и удаление

echo    \[4] Stop           - Только остановка

echo    \[5] Restart        - Перезапуск

echo    \[6] Logs           - Просмотр логов

echo    \[8] Restart UI     - Перезапуск только UI

echo    \[9] Status         - Статус контейнеров

echo    \[0] Выход

echo.
set /p choice=Ваш выбор:

if "!choice!"=="1" goto build
if "!choice!"=="2" goto up
if "!choice!"=="3" goto down
if "!choice!"=="4" goto stop
if "!choice!"=="5" goto restart
if "!choice!"=="6" goto logs
if "!choice!"=="8" goto restart\_ui
if "!choice!"=="9" goto status
if "!choice!"=="0" goto end

echo Неверный ввод. Попробуйте снова.
pause
goto menu

\:build
echo \[Docker] Сборка образов...
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% build
pause
goto menu

\:up
echo \[Docker] Запуск контейнеров...
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% up -d
pause
goto menu

\:down
echo \[Docker] Остановка и удаление контейнеров...
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% down
pause
goto menu

\:stop
echo \[Docker] Остановка контейнеров...
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% stop
pause
goto menu

\:restart
echo \[Docker] Перезапуск контейнеров...
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% down
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% up -d
pause
goto menu

\:logs
echo \[Docker] Логи контейнеров (нажмите Ctrl+C для выхода)...
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% logs -f
goto menu

\:restart\_ui
echo \[Docker] Перезапуск контейнера UI...
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% stop ui
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% rm -f ui
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% build ui
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% up -d ui
pause
goto menu

\:status
echo \[Docker] Статус контейнеров:
docker-compose --env-file %ENV\_FILE% -p %PROJECT\_NAME% -f %COMPOSE\_FILE% ps
pause
goto menu

\:end
echo До встречи, Ваше Превосходительство.
exit /b
