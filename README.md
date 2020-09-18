# Overview

A repository containing a sample database mirroring project.

This project is a sample based on the article from the SuperOffice community web site [Getting Started With Database Mirroring](https://community.superoffice.com/en/content/content/online/database-mirroring/).

## Requirements

* Must be an online customer with an active tenant in the production environment, or have a sandbox tenant in the SuperOffice online development environment. To get a sandbox environment, [register a SuperOffice online developer account](https://community.superoffice.com/en/developer/create-apps/resources/developer-registration/).

* Must have a [registered application](https://community.superoffice.com/en/developer/create-apps/resources/application-registration/) in SuperOffice Online.

* Destination database server must be __SQL Server__.

* Application must be authorized to access tenant resources. You can do this by signing into our [help application](https://devnet-tokens.azurewebsites.net/account/signin) and approve your application.

* Server hosting IIS must have installed [SuperOffice online certificates](https://community.superoffice.com/en/developer/create-apps/resources/downloads/) in accordance with the [installation procedures](https://community.superoffice.com/en/developer/create-apps/how-to/set-up/configure-certificates/). Use the correct certificates based on the target environment (development, stage and production).

## Configuration

| Name               | Value                                                |
|--------------------|------------------------------------------------------|
|SoAppId             | ApplicationID / ClientId                             |
|SoFederationGateway | SuperID login URL                                    |
|SuperIdCertificate  | Thumbprint of certificate (different per environment)|
|MirrorDatabaseName  | Name of destination database                         |
|ConnectionBase      | Database connection string                           |
|PrivateKeyFile      | Filename containing applications  private XML RSA key|

Questions should be asked on the [community developer forums](https://community.superoffice.com/en/Developer/Forum/Rooms/superoffice-product-api-group/crm-online-application/).
