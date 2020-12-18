using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ServicesCeltaWare.UtilitariosInfra.GoogleApiStandard
{
    public class GoogleServicesComputerAccount : IGoogleServices
    {
        //private static ModelGoogleDrive googleDrive = new ModelGoogleDrive();
        private static readonly HttpClient client = new HttpClient();

        public GoogleServicesComputerAccount()
        {
            //googleDrive = _googleDrive;
            client.Timeout = TimeSpan.FromHours(4);
        }

        public DriveService InitGDriveComputer(UserCredential credenciais)
        {
            try
            {
                return new Google.Apis.Drive.v3.DriveService(new Google.Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = credenciais
                });
            }
            catch (Exception err)
            {
                throw err;
            }
        }

        public async Task<string> Upload(string _fileName, string _path, string _folderId, UserCredential userCredential)
        {
            try
            {
                Google.Apis.Drive.v3.Data.File response = new Google.Apis.Drive.v3.Data.File();               

                using (var service = InitGDriveComputer(userCredential))
                {
                    service.HttpClient.Timeout = TimeSpan.FromHours(4);
                    var fileToUpload = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = _fileName,
                        MimeType = MimeTypesMap.GetMimeType(System.IO.Path.GetExtension(_path)),
                        Parents = new List<string>
                        {
                            _folderId
                        }
                    };


                    // *** Aqui validar se é para Atualizar ou criar !!! ***                    
                    // 1 - Caso existe verificar se o mesmo id encontra no GDrive
                    var isNew = await FindInGoogleDrive(_folderId, _fileName, userCredential);

                    if (String.IsNullOrEmpty(isNew.DriveId))
                    {
                        using (var stream = new System.IO.FileStream(_path + _fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            var request = service.Files.Create(fileToUpload, stream, fileToUpload.MimeType);
                            var valid = await request.UploadAsync();

                            if (valid.Status != Google.Apis.Upload.UploadStatus.Completed)
                            {
                                HelperLogs.WriteLog(DateTime.Now + "Resposta de Upload ainda nao completa.");

                                for (int i = 0; i < 3; i++)
                                {
                                    await Task.Delay(3000);
                                    if (valid.Status != Google.Apis.Upload.UploadStatus.Completed)
                                        i = 3;
                                }
                            }

                            response = request.ResponseBody;

                            // var _file = new Google.Apis.Drive.v3.Data.File();
                            //_file.Name = System.IO.Path.GetFileName(fullPath);
                            // _file.Id = request.ResponseBody.Id;

                            Google.Apis.Drive.v3.Data.File file = request.ResponseBody;
                            Google.Apis.Drive.v3.Data.Permission newPermission = new Google.Apis.Drive.v3.Data.Permission();
                            newPermission.EmailAddress = "backupcelta@gmail.com";
                            newPermission.Type = "user";
                            newPermission.Role = "writer";

                            Google.Apis.Drive.v3.PermissionsResource.CreateRequest insertRequest = service.Permissions.Create(newPermission, file.Id);
                            insertRequest.Execute();
                        }
                    }
                    else
                    {
                        using (var stream = new System.IO.FileStream(_path + _fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            var request = service.Files.Update(fileToUpload, isNew.Id, stream, fileToUpload.MimeType);
                            await request.UploadAsync();

                            response = request.ResponseBody;

                            var _file = new Google.Apis.Drive.v3.Data.File();
                            //_file.Name = System.IO.Path.GetFileName(fullPath);
                            _file.Id = request.ResponseBody.Id;

                            //Google.Apis.Drive.v3.Data.File file = request.ResponseBody;
                            Google.Apis.Drive.v3.Data.Permission newPermission = new Google.Apis.Drive.v3.Data.Permission();
                            newPermission.EmailAddress = "backupcelta@gmail.com";
                            newPermission.Type = "user";
                            newPermission.Role = "writer";

                            Google.Apis.Drive.v3.PermissionsResource.CreateRequest insertRequest = service.Permissions.Create(newPermission, _file.Id);
                            insertRequest.Execute();
                        }
                    }

                    service.Dispose();
                };

                if (response == null)
                    return String.Empty;

                return response.Id;
            }
           
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return "ERRO: " + err.Message;
            }

        }

        public async Task<Google.Apis.Drive.v3.Data.File> FindInGoogleDrive(string _folderId, string _fileName, UserCredential _userCredential)
        {
            try
            {
                List<string> lista = new List<string>();
                Google.Apis.Drive.v3.Data.File gDriveFile = new Google.Apis.Drive.v3.Data.File();
                
                using (var servico = InitGDriveComputer(_userCredential))
                {
                    servico.HttpClient.Timeout = TimeSpan.FromHours(4);
                    var request = servico.Files.List();
                    request.Fields = "files(id, name, createdTime, modifiedTime, size)";
                    request.Q = $"'{_folderId}' in parents";

                    var resultado = await request.ExecuteAsync();
                    var arquivos = resultado.Files;

                    if (arquivos != null && arquivos.Any())
                    {
                        foreach (var arquivo in arquivos)
                        {
                            if (arquivo.Name.Equals(_fileName))
                                return arquivo;
                        }
                    }

                    //servico.Dispose();
                    return gDriveFile;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                throw err;
            }
        }

        public async Task<FileList> FindFoldersService(UserCredential userCredential, string folderName)
        {
            try
            {
                List<string> lista = new List<string>();
                GoogleServicesComputerAccount servicesComputerAccount = new GoogleServicesComputerAccount();
                using (var servico = servicesComputerAccount.InitGDriveComputer(userCredential))
                {
                    var request = servico.Files.List();
                    request.Fields = "files(id, name)";
                    request.Q = $"name = '{folderName}'";

                    var resultado = await request.ExecuteAsync();
                    // var arquivos = resultado.Files;

                    servico.Dispose();
                    return resultado;
                };
            }
            catch (Exception err)
            {
                throw err;
            }
        }
    }
}
