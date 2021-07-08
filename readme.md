# DSR Hangfire example
This repository contains an example application developed for a Hangfire-related training session for DSR

## Prerequisites
### Docker
You need [docker](https://www.docker.com) with `Linux` containers to run the example.
Please, dedicate at least `2 GB` of RAM to Docker.

### Native dependencies
You can run the example without docker using packages and services installed in your system. Here's a list of requirements:

- .NET 5
- [MS SQL](https://www.microsoft.com/en-us/sql-server/sql-server-2019) (to run with MS SQL storage)
- [Postgre SQL](https://www.postgresql.org) (to run with Postgre storage)
- [ElasticSearch](https://www.elastic.co/guide/en/elasticsearch/reference/current/install-elasticsearch.html) – *optional*
- [Kibana](https://www.elastic.co/guide/en/kibana/current/install.html) - *optional*


## Running the example
### Docker
1. `cd <repo_folder>/DSR_HangfireExample`
2. `docker compose up`

It will start all database services, Kibana and the Web application.

Running apps are available at:
- http://localhost:5000/hangfire — Hangfire Dashboard
- http://0.0.0.0:5601/app/discover — Kibana

### Native dependencies
1. Start your SQL Server or Postgre SQL instance
2. Create an empty database (e.g. `hangfire`), update connection strings in `appsettings.json` file
4. (Optional) start ElasticSearch and Kibana instances, update URL in `appsettings.json`
5. `cd <repo_folder>/DSR_HangfireExample`
6. `dotnet run`
 

## Switch between SqlServer and Postgre storages
Open `appsettings.json` and change the following line:
```c#
  "HangfireStorageType": "<StorageType>"
```

- Use `SqlServer` value for MS SQL storage
- Use `Postgre` value for Postgre storage