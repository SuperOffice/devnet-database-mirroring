# Overview

A repository containing a sample database mirroring project.

This project is a sample based on the article from the docs site [Getting Started With Database Mirroring](https://docs.superoffice.com/online/mirroring/getting-started/index.html).

Database mirroring will not replicate all tables. The service skips unnecessary tables containing only configuration and/or sensitive information. A complete list of skipped tables is discussed in the article mentioned above.

To include more tables to skip, the table names must be added to a SkipTable user preference. To read more about that, please read the [skip mirroring tables](https://docs.superoffice.com/online/mirroring/skip-tables.html) article.

## Requirements

* Must be an online customer with an active tenant in the production environment, or have a sandbox tenant in the SuperOffice online development environment. To get a sandbox environment, [register a SuperOffice online developer account](https://docs.superoffice.com/apps/getting-started/developer-registration-form.html).

* Must have a [registered application](https://docs.superoffice.com/developer-portal/create-app/index.html) in SuperOffice Developer Portal.

* Destination database server must be __SQL Server__.

* Application must be authorized to access tenant resources. You can do this by signing into our [helper application](https://devnet-tools.superoffice.com/account/signin) and approve your application.

* This Server hosting IIS must have installed [SuperOffice online certificates](https://docs.superoffice.com/authentication/online/certificates/index.html) in accordance with the [installation procedures](https://docs.superoffice.com/authentication/online/certificates/add-certificate-snap-in.html). Use the correct certificates based on the target environment (development, stage and production).

## Configuration

| Name               | Value                                                |
|--------------------|------------------------------------------------------|
|SoAppId             | ApplicationID / ClientId                             |
|SoFederationGateway | SuperID login URL                                    |
|SuperIdCertificate  | Thumbprint of certificate (different per environment)|
|MirrorDatabaseName  | Name of destination database                         |
|ConnectionBase      | Database connection string                           |
|PrivateKeyFile      | Filename containing applications  private XML RSA key|
|EnableLogging       | Determines if a log file is written to the Temp dir. |

Questions should be asked on the [community developer forums](https://community.superoffice.com/en/technical/forums/api-forums/client-libraries-and-tools/).

Any bugs or issues observed should be added here under the [issues tab](https://github.com/SuperOffice/devnet-database-mirroring/issues).
