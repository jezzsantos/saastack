This is a custom mssql Docker image that includes the changes in [this repo](https://github.com/shanegenschaw/mssql-server-linux) by Shane Genschaw.

> It adds functionality to initialize a fresh instance. When a container is started for the first time, it will execute any files with extensions .sh or .sql that are found in /docker-entrypoint-initdb.d. Files will be executed in alphabetical order. You can easily populate your SQL Server services by mounting scripts into that directory and provide custom images with contributed data.

