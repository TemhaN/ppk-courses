#!/bin/bash
set -e

echo "Проверяем зависимости..."
which sqlcmd || { echo "sqlcmd не найден, пиздец"; exit 1; }
ls -l /app/web.dll || { echo "web.dll не найден, пиздец"; exit 1; }

echo "Проверяем DNS..."
cat /etc/resolv.conf
ping -c 4 $SQLSERVER_HOST || { echo "$SQLSERVER_HOST не резолвится, пиздец"; exit 1; }

echo "Ждем SQL Server 60 секунд..."
sleep 60

echo "Проверяем готовность SQL Server..."
for i in $(seq 1 60); do
    echo "Попытка $i/60..."
    if /opt/mssql-tools18/bin/sqlcmd -S $SQLSERVER_HOST,$SQLSERVER_PORT -U $SQLSERVER_USER -P "$SQLSERVER_PASSWORD" -C -Q "SELECT 1"
    then
        echo "SQL Server готов!"
        break
    else
        echo "Ошибка подключения: $?"
        /opt/mssql-tools18/bin/sqlcmd -S $SQLSERVER_HOST,$SQLSERVER_PORT -U $SQLSERVER_USER -P "$SQLSERVER_PASSWORD" -C -Q "SELECT 1" || true
    fi
    if [ $i -eq 60 ]; then
        echo "Ошибка: SQL Server не стал готов после 60 попыток"
        exit 1
    fi
    sleep 5
done

echo "Создаем базу если не существует..."
/opt/mssql-tools18/bin/sqlcmd -S $SQLSERVER_HOST,$SQLSERVER_PORT -U $SQLSERVER_USER -P "$SQLSERVER_PASSWORD" -C -Q "IF DB_ID('$DATABASE_NAME') IS NULL CREATE DATABASE $DATABASE_NAME" || { echo "Ошибка создания базы: $?"; exit 1; }

echo "Проверяем существование базы..."
/opt/mssql-tools18/bin/sqlcmd -S $SQLSERVER_HOST,$SQLSERVER_PORT -U $SQLSERVER_USER -P "$SQLSERVER_PASSWORD" -C -Q "SELECT name FROM sys.databases WHERE name = '$DATABASE_NAME'" || { echo "База $DATABASE_NAME не найдена после попытки создания: $?"; exit 1; }

# echo "Применяем миграции..."
# dotnet ef database update --verbose || { echo "Пиздец с миграциями: $?"; exit 1; }

echo "Запускаем приложение..."
exec dotnet web.dll