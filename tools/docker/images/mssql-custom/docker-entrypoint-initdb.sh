#!/bin/bash

# wait for database to start...
echo "$0: SQL Server startup in progress"
until sqlcmd -C -U sa -P ${SA_PASSWORD} -Q 'SELECT 1;' &> /dev/null; do
  echo -n "."
  sleep 1
done

echo "$0: Initializing database"
for f in /docker-entrypoint-initdb.d/*; do
  case "$f" in
    *.sh)     echo "$0: running $f"; . "$f" ;;
    *.sql)    echo "$0: running $f"; sqlcmd -C -U sa -P ${SA_PASSWORD} -X -i  "$f"; echo ;;
    *)        echo "$0: ignoring $f" ;;
  esac
  echo
done
echo "$0: SQL Server Database ready"