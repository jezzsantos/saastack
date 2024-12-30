FROM mcr.microsoft.com/mssql/server:2022-latest

VOLUME /docker-entrypoint-initdb.d
EXPOSE 1433

ENV PATH $PATH:/opt/mssql-tools18/bin

COPY docker-entrypoint.sh /usr/local/bin/
COPY docker-entrypoint-initdb.sh /usr/local/bin/

ENTRYPOINT ["docker-entrypoint.sh"]