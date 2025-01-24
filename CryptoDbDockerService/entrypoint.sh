#!/bin/bash

DB_FILES_PATH="/var/opt/mssql/data" # Ścieżka do katalogu z plikami bazy danych
DB_MARKER_FILE="$DB_FILES_PATH/db_initialized" # Flaga inicjalizacji bazy
DB_CONNECTION_STRING="Server=localhost;User Id=sa;Password=YourStrong@Password;" # Connection string do bazy danych

# Funkcja sprawdzająca gotowość SQL Server
wait_for_sqlserver() {
  echo "Czekam na gotowość SQL Server..."
  until /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password" -Q "SELECT 1" > /dev/null 2>&1; do
    echo "SQL Server nie jest gotowy. Próba ponowna za 5 sekund..."
    sleep 5
  done
  echo "SQL Server gotowy do działania!"
}

# Funkcja inicjalizująca bazę
initialize_database() {
  if [ -d "$DB_FILES_PATH" ] && [ "$(ls -A $DB_FILES_PATH/*.mdf 2>/dev/null)" ]; then
    echo "Pliki bazy danych istnieją. Ominięto migrację EF."
  else
    echo "Pliki bazy danych nie istnieją. Rozpoczynam migrację EF..."
    dotnet ef database update # Wykonanie migracji EF
    echo "Migrację zakończono. Tworzę plik flagi..."
    touch "$DB_MARKER_FILE" # Tworzenie pliku flagi
  fi
}

# Start SQL Server w tle
/opt/mssql/bin/sqlservr & # Uruchomienie SQL Server w tle

wait_for_sqlserver # Oczekiwanie na gotowość SQL Server
initialize_database # Inicjalizacja bazy danych (lub wczytanie plików)
wait # Czekaj na zakończenie procesów