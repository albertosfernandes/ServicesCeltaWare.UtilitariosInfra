using System;
using System.Threading.Tasks;

namespace ServicesCeltaWare.UtilitariosInfra.ConsoleGoogleApi
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                int value = Convert.ToInt32(args[0]);
                string credentialFileName = args[1];
                string name = args[2];
                string path = args[3];
                string folderId = args[4];
                string resp = string.Empty;
                if(value < 1 || string.IsNullOrEmpty(credentialFileName) || string.IsNullOrEmpty(name))
                {
                    LogConsole();
                }
                GoogleApiStandard.GoogleServicesComputerAccount servicesComputerAccount = new GoogleApiStandard.GoogleServicesComputerAccount();           
                GoogleApiStandard.Security security = new GoogleApiStandard.Security(credentialFileName);
                var user = security.AuthenticateComputerAccount();
                switch (value)
                {
                    case 1:
                        {
                            resp = await servicesComputerAccount.Upload(name, path, folderId, user);
                            Console.WriteLine(resp);
                            break;
                        }
                    case 2:
                        {
                            var fileList = await servicesComputerAccount.FindFoldersService(user, folderId);
                            Console.WriteLine(fileList);
                            break;
                        }
                }
            }
            catch(Exception err)
            {
               
                Console.WriteLine(err.Message);
            }            
        }

        private static void LogConsole()
        {
            Console.WriteLine("Parametros: (1)=Tipo da chamada: 1-Upload; 2-Find;");
            Console.WriteLine("(2)=nome do arquivo credencial;");
            Console.WriteLine("(3)=nome do arquivo;");
            Console.WriteLine("(4)=caminho do backup;");
            Console.WriteLine("(5)=folderId do Google Drive;");
        }
    }
}
