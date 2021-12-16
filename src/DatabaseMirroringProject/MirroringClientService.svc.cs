using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Web.Configuration;
using System.Web.Hosting;
using SuperOffice.Online.Mirroring.Data;
using SuperOffice.Online.Mirroring;
using SuperOffice.SuperID.Client.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Selectors;
using SuperOffice.SuperID.Contracts.SystemUser.V1;

namespace DatabaseMirroringProject.SuperOfficeMirror
{
	/// <summary>
	/// Demo Implementation of IMirrorAdmin, and possibility of overriding main implementation as well
	/// </summary>
	public class MirroringClientService : MirroringClientImplementation
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

		/// <summary>
		/// Get application identifier from web.config file; the original comes from OC and was created when the App was registered. It varies between environments (SOD, Stage, Prod)!
		/// </summary>
		/// <returns></returns>
		protected override string GetApplicationIdentifier()
		{
			return WebConfigurationManager.AppSettings["SoAppId"];
		}


		/// <summary>
		/// Get private key from the keyfile that is specified in web.config
		/// </summary>
		/// <returns></returns>
		protected override string GetPartnerKey()
		{
			var fileName = WebConfigurationManager.AppSettings["PrivateKeyFile"];
			if (!Path.IsPathRooted(fileName))
				fileName = Path.Combine(HostingEnvironment.MapPath(@"~"), fileName);
			return File.ReadAllText(fileName);
		}




        protected override SuperIdToken ValidateSuperOfficeSignedToken(string token)
        {
			string issuer;

			// extract the ValidIssuer claim value
			var SecurityTokenHandler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
			var securityToken = SecurityTokenHandler.ReadJsonWebToken(token);
			if (!securityToken.TryGetPayloadValue<string>("iss", out issuer))
			{
				throw new Microsoft.IdentityModel.Tokens.SecurityTokenException("Unable to read ValidIssuer from AuthenticationRequest SignedToken.");
			}

			var tokenHandler = new SuperIdTokenHandler();
			tokenHandler.ValidIssuer = issuer;
			tokenHandler.ValidateAudience = false;
            tokenHandler.CertificateValidator = X509CertificateValidator.None;
            tokenHandler.JwtIssuerSigningCertificate = new X509Certificate2(
               HostingEnvironment.MapPath("~/App_Data/") + "SuperOfficeFederatedLogin.crt"
            );

            return tokenHandler.ValidateToken(token, TokenType.Jwt);
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

		/// <summary>
		/// Hook method, default implementation is to do nothing. In any case, <b>DO NOT</b> do anything here
		/// that takes a long time or is likely to fail; the SuperOffice Online Mirroring Agent is waiting.
		/// </summary>
		public override void OnReplicationCompleted(string contextIdentifier, ReplicationSummary[] summary, string clientState)
		{
		}

		/// <summary>
		/// Hook method, default implementation is to do nothing. In any case, <b>DO NOT</b> do anything here
		/// that takes a long time or is likely to fail; the SuperOffice Online Mirroring Agent is waiting.
		/// </summary>
		public override void OnWipe(string contextIdentifier, string tableName, string clientState)
		{
		}


		/// <summary>
		/// Hook method, default implementation is to do nothing. In any case, <b>DO NOT</b> do anything here
		/// that takes a long time or is likely to fail; the SuperOffice Online Mirroring Agent is waiting.
		/// </summary>
		public override void OnBeforeReplicateTable(string contextIdentifier, string tableName, SchemaChangeType schemaChange, string clientState)
		{
		}

		/// <summary>
		/// Hook method; some kind of logging is strongly recommended. But <b>DO NOT</b> do anything here
		/// that takes a long time or is likely to fail; the SuperOffice Online Mirroring Agent is waiting.
		/// </summary>
		/// <remarks>
		/// This example implementation writes events to a log, with a new file each day, located in the system TEMP directory.
		/// A minimal attempt is made to keep the formatting roughly legible, and a timestamp is prefixed to each event.
		/// <br/>
		/// Pedantic developers might consider using UTC times - no messing around with summer time and such - but there
		/// is some human inconvenience attached to that.
		/// <br/>
		/// Note the universal catch; you <b>DO NOT</b> want your logging code to itself be the cause of crashes.
		/// Every developer learns this lesson at some point in their carreer :-).
		/// </remarks>
		public override void LogEvent(string contextIdentifier, string operation, ReplicationEventType type, string message, string additionalInfo)
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
}
