using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Plus.v1;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ServicesCeltaWare.UtilitariosInfra.GoogleApiStandard
{
    public class Security
    {
        private string _credentialFileName;
        public Security(string credentialFileName)
        {
            _credentialFileName = credentialFileName;
        }

        public ServiceAccountCredential AuthenticateServiceAccount(string serviceAccountEmail)
        {
            try
            {
                string[] scopes = new string[] { DriveService.Scope.Drive };

                var certificate = new X509Certificate2(_credentialFileName, "notasecret", X509KeyStorageFlags.Exportable);

                ServiceAccountCredential credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(serviceAccountEmail)
                    {
                        Scopes = scopes
                    }.FromCertificate(certificate));

                //DriveService service = new DriveService(new BaseClientService.Initializer()
                //{
                //    HttpClientInitializer = credential,
                //    ApplicationName = "ServicesCeltaInfra"
                //});

                return credential;
            }
            catch (Exception err)
            {
                HelperLogs.WriteLog(err.Message + "\br" + err.StackTrace);
                throw err;
            }
        }      
    }
}
