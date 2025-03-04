using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SuperOffice.Online.Mirroring;
using SuperOffice.Online.Mirroring.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Web.Configuration;
using System.Web.Hosting;

namespace DatabaseMirroringApplication
{
    public class DatabaseMirroringService : SuperOffice.Online.Mirroring.MirroringClientImplementation
    {
        public override string ResolveConnection(string contextIdentifier, string clientState)
        {
            var connectionBase = ConfigurationManager.AppSettings["ConnectionBase"];

            var masterConnection = connectionBase + ";Database=master";
            var replicaName = WebConfigurationManager.AppSettings["MirrorDatabaseName"].Replace("[@contextId]", contextIdentifier);

            using (var dbcM = new DbCmd(masterConnection))
            {
                dbcM.SetText("if exists (select * from sys.databases where name = @dbname) select 1 else select 0");
                dbcM.AddParam("@dbname", replicaName);

                if (dbcM.ExecuteScalar<int>() == 0)
                {
                    dbcM.SetText("create database [{0}]", replicaName);
                    dbcM.ExecuteNonQuery();

                    // the CREATE DATABASE statement returns **before the database creation completes**, so we have to poll to make sure the database is ok
                    // (learned from hard experience, take my word for it)
                    bool dbAvailable = false;
                    do
                    {
                        try
                        {
                            Thread.Sleep(1000);
                            dbcM.SetText("use [{0}]; select top 1 * from sys.tables", replicaName);
                            dbcM.ExecuteNonQuery();
                            dbAvailable = true;
                        }
                        catch { }
                    } while (!dbAvailable);
                }
            }

            return masterConnection + ";Database=" + replicaName;
        }

        protected override string GetApplicationIdentifier()
        {
            return WebConfigurationManager.AppSettings["SoAppId"];
        }

        protected override string GetPartnerKey()
        {
            var fileName = WebConfigurationManager.AppSettings["PrivateKeyFile"];
            if (!Path.IsPathRooted(fileName))
                fileName = Path.Combine(HostingEnvironment.MapPath(@"~"), fileName);
            return File.ReadAllText(fileName);
        }

        protected override ClaimsIdentity ValidateSuperOfficeSignedToken(string token)
        {
            string ValidIssuer = "SuperOffice AS";

            var environment = WebConfigurationManager.AppSettings["Environment"];
            var certificatePath = $"~/App_Data/SuperOfficeFederatedLogin.{environment.ToLower()}.crt";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters();
            tokenValidationParameters.ValidateAudience = false;
            tokenValidationParameters.ValidIssuer = ValidIssuer;
            tokenValidationParameters.IssuerSigningKey = new X509SecurityKey(new X509Certificate2(HostingEnvironment.MapPath(certificatePath)));

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            return principal.Identities.OfType<ClaimsIdentity>().FirstOrDefault();
        }

        protected override string GetAdditionalAuthResponseInfo()
        {
            // Uncomment this to return a "please do not run replication to me right now"
            // as part of the authentication response. Doing so provides breathing space while
            // the rest of your site is down, or you are moving the customer database to you
            // backup datacenter in Turkmenistan or whatever.

            //return "{ \"doNotReplicate\": \"true\" }";

            return string.Empty;
        }

        public override void LogEvent(string contextIdentifier, string operation, ReplicationEventType type, string message, string additionalInfo)
        {
            if (Convert.ToBoolean(WebConfigurationManager.AppSettings["EnableLogging"]))
            {
                try
                {
                    var fileName = Path.Combine(Path.GetTempPath(), "Mirroring-" + DateTime.Today.ToString("yyyyMMdd") + ".log");
                    var infoLine = additionalInfo.Replace("\n", "\n\t").Replace("\r", "").TrimEnd('\t', '\n');

                    var logItem = string.Format("{0:HH\\:mm\\:ss} {1} - [{2}] in {3}: {4}{5}\n",
                        DateTime.Now,
                        contextIdentifier,
                        type,
                        operation,
                        message.Replace("\n", " ").Replace("\r", "").Trim('\t'),
                        string.IsNullOrWhiteSpace(infoLine) ? "" : "\n\t" + infoLine
                        );

                    File.AppendAllText(fileName, logItem.Replace("\n", "\r\n"));
                }
                catch { /* just... don't crash, ok? */ }
            }
        }

        public override void OnBeforeReplicateTable(string contextIdentifier, string tableName, SchemaChangeType schemaChange, string clientState)
        {
        }

        public override void OnReplicationCompleted(string contextIdentifier, ReplicationSummary[] summary, string clientState)
        {
        }

        public override void OnWipe(string contextIdentifier, string tableName, string clientState)
        {
        }
    }
}
