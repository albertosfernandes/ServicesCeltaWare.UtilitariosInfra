using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServicesCeltaWare.UtilitariosInfra.GoogleApiStandard
{
    public class Security
    {
        private string _credentialFileName;
        public Security(string credentialFileName)
        {
            _credentialFileName = credentialFileName;
        }

        public Google.Apis.Auth.OAuth2.UserCredential Auth(out string status)
        {
            Google.Apis.Auth.OAuth2.UserCredential credential;
            status = null;
            try
            {
                using (var stream = new System.IO.FileStream(_credentialFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    var diretorioAtual = System.IO.Path.GetDirectoryName(Directory.GetCurrentDirectory());
                    var diretorioCredenciais = System.IO.Path.Combine(diretorioAtual, "credential");

                    credential = Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(
                        Google.Apis.Auth.OAuth2.GoogleClientSecrets.Load(stream).Secrets,
                        new[] { Google.Apis.Drive.v3.DriveService.Scope.DriveReadonly },
                        "user",
                        System.Threading.CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(diretorioCredenciais, true)).Result;
                }
                return credential;
            }
            catch (Exception err)
            {
                status = err.Message;
                credential = null;
                return credential;
            }
        }
    }
}
