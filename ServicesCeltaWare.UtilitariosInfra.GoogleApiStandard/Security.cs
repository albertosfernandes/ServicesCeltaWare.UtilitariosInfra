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

        public UserCredential AuthenticateComputerAccount()
        {
            Google.Apis.Auth.OAuth2.UserCredential credenciais;

            using (var stream = new System.IO.FileStream(_credentialFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var diretorioAtual = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var diretorioCredenciais = System.IO.Path.Combine(diretorioAtual, "credential");

                credenciais = Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(
                    Google.Apis.Auth.OAuth2.GoogleClientSecrets.Load(stream).Secrets,
                    new[] { Google.Apis.Drive.v3.DriveService.Scope.Drive },
                    "user",
                    System.Threading.CancellationToken.None,
                    new Google.Apis.Util.Store.FileDataStore(diretorioCredenciais, true)).Result;
            }

            return credenciais;
        }

        public Google.Apis.Drive.v3.DriveService LoginComputerAccount(Google.Apis.Auth.OAuth2.UserCredential credenciais)
        {
            return new Google.Apis.Drive.v3.DriveService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credenciais
            });
        }
    }
}
