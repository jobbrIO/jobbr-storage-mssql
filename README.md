
# Jobbr MSSql Storage Provider [![Develop build status][mssql-badge-build-develop]][mssql-link-build]

This is a storage adapter implementation for the [Jobbr .NET JobServer](http://www.jobbr.io) to store job related information on MS SQL Servers. 
The Jobbr main repository can be found on [JobbrIO/jobbr-server](https://github.com/jobbrIO).

[![Master build status][mssql-badge-build-master]][mssql-link-build] 
[![NuGet-Stable][mssql-badge-nuget]][mssql-link-nuget]
[![Develop build status][mssql-badge-build-develop]][mssql-link-build] 
[![NuGet Pre-Release][mssql-badge-nuget-pre]][mssql-link-nuget] 

## Installation
First of all you'll need a working jobserver by using the usual builder as shown in the demos ([jobbrIO/jobbr-demo](https://github.com/jobbrIO/jobbr-demo)). In addition to that you'll need to install the NuGet Package for this extension.

### NuGet

    Install-Package Jobbr.Storage.MsSql

### Configuration
Since you already have a configured server, the registration of the MsSQL Storage Provider is quite easy. Actually you only need a working Database-Connection (A list of typical ConnectionStrings can be found on [https://www.connectionstrings.com/sql-server/](https://www.connectionstrings.com/sql-server/))

```c#
using Jobbr.Storage.MsSql;

/* ... */

var builder = new JobbrBuilder();

builder.AddMsSqlStorage(config =>
{
    // Your connection string
    config.ConnectionString = @"Server=.\SQLEXPRESS;Integrated Security=true;InitialCatalog=JobbrDemoTest;";

    // Default schema is "Jobbr", change if you want
    config.Schema = "Own";
});

server.Start();
```

There are additional configuration options beside the mentioned ones above.

| Name | Description | Default |
| ---- | ----------- | ------- |
|`ConnectionString` | Standard ConnectionString as available on [https://www.connectionstrings.com/sql-server/](https://www.connectionstrings.com/sql-server/) | **Required** |
|`Schema` | Change the Database Schema if you want | `Jobbr` |
|`AutoUpgrade` | Auto execute upgrade scripts to update the databse to the terget version | `true` |
|`AutoCreateDatabase` | Create database if not existing | `false` |

### Database-Schema
The extension can create the datebase and related schemas and tables on start and does automatically upgrade the database schema on future releases. By default, the Database creation is disabled while auto migrations (based on DbUp) are executed automatically on start.

![Diagram](https://raw.githubusercontent.com/jobbrIO/jobbr-storage-mssql/develop/docs/diagram.png)

# License
This software is licenced under GPLv3. See [LICENSE](LICENSE), and the related licences of 3rd party libraries below.

# Acknowledgements
This extension is built using the following great open source projects

* [Dapper](https://github.com/StackExchange/Dapper) 
  [(Apache-2.0)](https://github.com/StackExchange/Dapper/blob/master/License.txt)
* [DbUp](https://dbup.github.io/) [(MIT)](https://raw.githubusercontent.com/DbUp/DbUp/master/license.txt)
* [LibLog](https://github.com/damianh/LibLog) 
  [(MIT)](https://github.com/damianh/LibLog/blob/master/licence.txt)

# Credits
This application was built by the following awesome developers:
* Michael Schnyder
* Oliver Zürcher

[mssql-link-build]:            https://ci.appveyor.com/project/Jobbr/jobbr-storage-mssql         
[mssql-link-nuget]:            https://www.nuget.org/packages/Jobbr.Storage.MsSql

[mssql-badge-build-develop]:   https://img.shields.io/appveyor/ci/Jobbr/jobbr-storage-mssql/develop.svg?label=develop
[mssql-badge-build-master]:    https://img.shields.io/appveyor/ci/Jobbr/jobbr-storage-mssql/master.svg?label=master
[mssql-badge-nuget]:           https://img.shields.io/nuget/v/Jobbr.Storage.MsSql.svg?label=NuGet%20stable
[mssql-badge-nuget-pre]:       https://img.shields.io/nuget/vpre/Jobbr.Storage.MsSql.svg?label=NuGet%20pre

