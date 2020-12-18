using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Plus.v1;
using Google.Apis.Services;

using Google.Apis.Auth.OAuth2;

using HeyRed.Mime;
using MimeKit;
using Newtonsoft.Json;
using RestSharp;
using ServicesCeltaWare.UtilitariosInfra.GoogleApiStandard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace ServicesCeltaWare.UtilitariosInfra.GoogleApiStandard
{
    public class CallManager
    {
        private static ModelGoogleDrive googleDrive = new ModelGoogleDrive();
        private static readonly HttpClient client = new HttpClient();

        public CallManager(ModelGoogleDrive _googleDrive)
        {
            googleDrive = _googleDrive;
            client.Timeout = TimeSpan.FromHours(4);
        }

        private static DriveService InitGDriveComputer(UserCredential credenciais)
        {
            try
            {
                return new Google.Apis.Drive.v3.DriveService(new Google.Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = credenciais
                });
            }
            catch(Exception err)
            {
                throw err;
            }
        }

        //private static Security security = new Security("servicesceltainfra-6f1e21301fbe.p12");        

        private static Google.Apis.Drive.v3.DriveService InitGDriveService(ModelGoogleDrive _googleDrive)
        {
            try
            {
                Security security = new Security(_googleDrive.CredentialFileName);

                var credential = security.AuthenticateServiceAccount(_googleDrive.GoogleDriveAccountMail);
                Google.Apis.Drive.v3.DriveService service = new Google.Apis.Drive.v3.DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "ServicesCeltaInfra"
                });

                return service;
            }
            catch (Exception err)
            {
                HelperLogs.WriteLog(err.Message + "\br" + err.StackTrace);
                throw err;
            }
        }

        private static Google.Apis.Drive.v3.DriveService InitGDriveServiceWhithKey(ModelGoogleDrive _googleDrive)
        {
            try
            {                
                Google.Apis.Drive.v3.DriveService service = new Google.Apis.Drive.v3.DriveService(new BaseClientService.Initializer()
                {
                    ApplicationName = "ServicesCeltaInfra",
                    ApiKey = _googleDrive.ApiKey
                });

                return service;
            }
            catch (Exception err)
            {
                HelperLogs.WriteLog(err.Message + "\br" + err.StackTrace);
                throw err;
            }
        }

        public async Task<string> InitRequestToken(ModelGoogleDrive _googleDrive)
        {
            try
            {
                string postUri = "https://oauth2.googleapis.com/device/code";

                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(postUri)
                };

                request.Properties.Add("client_id", _googleDrive.GoogleAccountId);
                request.Properties.Add("scopes", "email");

                var streamResult = await client.SendAsync(request);
                
                return await streamResult.Content.ReadAsStringAsync();
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public static async Task<string> UploadFull(DriveService _service, string _fileName, string _path, string _folderId)
        {
            try
            {
                Google.Apis.Drive.v3.Data.File response = new Google.Apis.Drive.v3.Data.File();

                using (var service = _service)
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
                    service.HttpClient.Timeout = TimeSpan.FromHours(4);


                    // *** Aqui validar se é para Atualizar ou criar !!! ***                    
                    // 1 - Caso existe verificar se o mesmo id encontra no GDrive
                    var isNew = await FindInGoogleDriveFull(_fileName, _folderId, service);

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
                return err.Message;
            }
        }

        public static async Task<string> Upload(string _fileName, string _path, ModelGoogleDrive _googleDrive, string _folderId)
        {
            try
            {
                Google.Apis.Drive.v3.Data.File response = new Google.Apis.Drive.v3.Data.File();

                using (var service = CallManager.InitGDriveService(_googleDrive))
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
                    var isNew = await FindInGoogleDrive(_googleDrive, _fileName);

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
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                    return "ERRO: CancellationToken is True.";

                return "ERRO CancellationToken is False";
            }
            catch (Exception err)
            {
                HelperLogs.WriteLog(err.Message + "\br" + err.StackTrace);
                return "ERRO: " + err.Message;
            }

        }

        public static async Task<string> Upis(string _fileName, string _path, ModelGoogleDrive _googleDrive, string _folderId)
        {
            try
            {
                var responseToken = await CallManager.GetToken(_googleDrive);
                string _bearerToken = responseToken.Substring(17, 219);
                var client = new RestClient("https://www.googleapis.com/upload/drive/v3/files?uploadType=media");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddParameter("Authorization", "Bearer " + _bearerToken);
                request.AddHeader("Content-Type", "multipart/form-data");
                request.AddHeader("Content-Length", "436490240");
                request.AddFile("CeltaBSEmporioSaborBackupFull.bak", @"C:\Temp\backup\CeltaBSEmporioSaborBackupFull.bak");
                IRestResponse response = client.Execute(request);
                return response.Content;
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public static async Task<string> Upi(string _fileName, string _path, ModelGoogleDrive _googleDrive, string _folderId)
        {
            try
            {
                // 2. Create the url 
                string postUri = "https://www.googleapis.com/drive/v3/files?uploadType=media";
                //string filename = "myFile.png";
                var response = await CallManager.GetToken(_googleDrive);
                string _bearerToken = response.Substring(17, 219);
                // In my case this is the JSON that will be returned from the post
                string result = "";

                using (var formContent = new MultipartFormDataContent("NKdKd9Yk"))
                {
                    formContent.Headers.ContentType.MediaType = "multipart/form-data";
                    // 3. Add the filename C:\\... + fileName is the path your file
                    Stream fileStream = System.IO.File.OpenRead(_path + _fileName);
                    formContent.Add(new StreamContent(fileStream), _fileName, _fileName);

                    using (var client = new HttpClient())
                    {
                        // Bearer Token header if needed
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _bearerToken);
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                        try
                        {
                            // 4.. Execute the MultipartPostMethod
                            var message = await client.PostAsync(postUri, formContent);
                            // 5.a Receive the response
                            result = await message.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            // Do what you want if it fails.
                            throw ex;
                        }
                    }
                }


                return result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }

        public static async Task<string> Up(string _fileName, string _path, ModelGoogleDrive _googleDrive, string _folderId)
        {
            try
            {
                string postUri = "https://www.googleapis.com/drive/v3/files?uploadType=resumable";
                var responseToken = await CallManager.GetToken(_googleDrive);
                string _bearerToken = responseToken.Substring(17, 219);

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _bearerToken);

                using (HttpRequestMessage requestMessage = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(postUri)
                })
                {


                    using (var stream = new System.IO.FileStream(_path + _fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        var length = stream.Length.ToString();
                        StreamContent sc = new StreamContent(stream);
                        sc.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                        sc.Headers.Add("Content-Length", length);

                        requestMessage.Content = sc;

                        var response = client.PostAsync(postUri, requestMessage.Content).Result;
                        return response.Content.ToString();
                    }
                }
                //var request = new HttpRequestMessage()
                //{
                //    Method = HttpMethod.Post,
                //    RequestUri = new Uri(postUri)
                //};




                //using (var stream = new System.IO.FileStream(_path + _fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                //{
                //    var length = stream.Length.ToString();
                //    StreamContent sc = new StreamContent(stream);
                //    sc.Headers.Add("Content-Type", "application/octet-stream");
                //    sc.Headers.Add("Content-Length", length);

                //    request.Content = sc;

                //    var response = new HttpClient().SendAsync(request).Result;

                //    //using (var formData = new MultipartFormDataContent())
                //    //{
                //    //    formData.Add(sc);

                //    //    var response = client.PostAsync(postUri, formData).Result;

                //    //    return response.StatusCode.ToString();
                //    //}
                //    return await response.Content.ReadAsStringAsync();
                //}

            }
            catch (Exception err)
            {
                HelperLogs.WriteLog(err.Message + "\br" + err.StackTrace);
                return "ERRO: " + err.Message;
            }

        }

        public static async Task<string> TesteUp (string _fileName, string _path, ModelGoogleDrive _googleDrive, string _folderId)
        {
            try
            {
                string postUri = "https://www.googleapis.com/upload/drive/v3/files?uploadType=media";
                var responseToken = await CallManager.GetToken(_googleDrive);
                string _bearerToken = responseToken.Substring(17, 219);

                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(postUri)
                };

                var path = _path + _fileName;

                if (System.IO.File.Exists(path))
                {
                    bool r = true;
                }

                using (var filestream = System.IO.File.OpenRead(path))
                {
                    var length = filestream.Length.ToString();
                    var streamContent = new StreamContent(filestream);
                    streamContent.Headers.Add("Content-Type", "application/octet-stream");
                    streamContent.Headers.Add("Content-Length", length);


                    request.Content = streamContent;

                    var response = new HttpClient().SendAsync(request).Result;
                    
                }                

                return " ";
            }
            catch(Exception err)
            {
                throw err;
            }
        }

        public async Task<string> ExemploGoogle(string _fileName, string _path, ModelGoogleDrive _googleDrive, string _folderId)
        {
            try
            {
                string postUri = "https://www.googleapis.com/upload/drive/v3/files";
                var responseToken = await CallManager.GetToken(_googleDrive);
                string _bearerToken = responseToken.Substring(17, 219);

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _bearerToken);

                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(postUri)
                };

                request.Properties.Add("uploadType", "resumable");

                //var fileToUpload = new Google.Apis.Drive.v3.Data.File()
                //{
                //    Name = _fileName,
                //    MimeType = MimeTypesMap.GetMimeType(System.IO.Path.GetExtension(_path)),
                //    Parents = new List<string>
                //        {
                //            _folderId
                //        }
                //};


                byte[] byteArray = System.IO.File.ReadAllBytes(_path + _fileName);
                MemoryStream stream = new MemoryStream(byteArray);

                var length = stream.Length.ToString();
                StreamContent sc = new StreamContent(stream);
                //sc.Headers.Add("Content-Type", "application/octet-stream");
                //sc.Headers.Add("Content-Length", length);

                request.Content = sc;
                var streamResult = await client.SendAsync(request);

                //var response = client.PostAsync(postUri, sc);
                return await streamResult.Content.ReadAsStringAsync();
            }
            catch(Exception err)
            {
                throw err;
            }
            finally
            {
                //client.Dispose();
            }
        }

        public static async Task<IList<string>> ListFilesInFolder(ModelGoogleDrive _googleDrive/*, string folderId*/)
        {
            List<string> lista = new List<string>();

            using (var servico = InitGDriveService(_googleDrive) )
            {
                var request = servico.Files.List();
                request.Fields = "files(id, name, createdTime, modifiedTime, size)";
                request.Q = $"'{_googleDrive.FolderId}' in parents";
                var resultado = await request.ExecuteAsync();
                var arquivos = resultado.Files;

                if (arquivos != null && arquivos.Any())
                {
                    foreach (var arquivo in arquivos)
                    {
                        lista.Add(arquivo.Name + " " + arquivo.Id + " " + arquivo.CreatedTime + " " + arquivo.ModifiedTime + " " + arquivo.Size + "<br>");
                    }
                }
            }


            return lista;
        }

        public static async Task<Google.Apis.Drive.v3.Data.File> FindInGoogleDrive(ModelGoogleDrive _googleDrive, string _fileName)
        {
            try
            {
                List<string> lista = new List<string>();
                Google.Apis.Drive.v3.Data.File gDriveFile = new Google.Apis.Drive.v3.Data.File();

                using (var servico = InitGDriveService(_googleDrive))
                {
                    servico.HttpClient.Timeout = TimeSpan.FromHours(4);
                    var request = servico.Files.List();
                    request.Fields = "files(id, name, createdTime, modifiedTime, size)";
                    request.Q = $"'{_googleDrive.FolderId}' in parents";
                    
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
            catch(Exception err)
            {
                throw err;
            }
        }

        public static async Task<Google.Apis.Drive.v3.Data.File> FindInGoogleDriveFull(string _fileName, string _folderId, DriveService _service)
        {
            try
            {
                List<string> lista = new List<string>();
                Google.Apis.Drive.v3.Data.File gDriveFile = new Google.Apis.Drive.v3.Data.File();

                using (var servico = _service)
                {
                    //servico.HttpClient.Timeout = TimeSpan.FromHours(4);
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
                throw err;
            }
        }


        public static IList<string> ListFolders(ModelGoogleDrive _googleDrive)
        {
            List<string> lista = new List<string>();

            using (var servico = InitGDriveService(_googleDrive) )
            {
                var request = servico.Files.List();
                request.Fields = "files(id, name)";
                //request.Q = "mimeType='application/vnd.google-apps.folder'";
                // request.Q = "'17CYl9ni0xJEERcMZeKalLZ5D3Kp-vjw4' in parents";
                // request.Q = $"name contains '{folderName}'";
                //request.Spaces = "Srv Backup DataBases";
                var resultado = request.Execute();
                var arquivos = resultado.Files;


                if (arquivos != null && arquivos.Any())
                {
                    foreach (var arquivo in arquivos)
                    {
                        lista.Add(arquivo.Name + " " + arquivo.Id);
                    }
                }
                servico.Dispose();
            }


            return lista;
        }

        public static async Task<FileList> GetAllFoldersService(ModelGoogleDrive _googleDrive)
        {
            try
            {
                List<string> lista = new List<string>();

                using (var servico = InitGDriveService(_googleDrive) )
                {
                    var request = servico.Files.List();
                    request.Fields = "files(id, name)";
                    var resultado = await request.ExecuteAsync();
                    // var arquivos = resultado.Files;

                    servico.Dispose();
                    return resultado;
                }
            }
            catch(Exception err)
            {
                throw err;
            }
        }

        public static async Task<string> ListFilesUri(string googleToken)
        {
            try
            {
                string postUri = "https://www.googleapis.com/drive/v3/files";
                var parameters = new Dictionary<string, string>();
                parameters["q"] = "name='BackupCeltaNuvem'";                
                var requestParams = new HttpRequestMessage(HttpMethod.Post,postUri);
                requestParams.Headers.Add("Authorization", "Bearer " + googleToken);                
                requestParams.Properties.Add("q", "name='BackupCeltaNuvem'");

                var streamResult = await client.SendAsync(requestParams);

                var contents = await streamResult.Content.ReadAsStringAsync();


                return "";
                    
            }
            catch (Exception err)
            {
                throw err;
            }

        }

        public static async Task<string> GetToken(ModelGoogleDrive _googleDrive)
        {
            try
            {
                // Erro ao descobrir quem é GoogleToken.GetWithRSA256
                //var personalToken = await GoogleToken.GetWithRSA256(_googleDrive.CredentialFileName);
                string postUri = "https://oauth2.googleapis.com/token";

                var parameters = new Dictionary<string, string>();
                parameters["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";
                // parameters["assertion"] = personalToken;

                //var values = new List<KeyValuePair<string, string>>();

                //values.Add(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"));
                //values.Add(new KeyValuePair<string, string>("assertion", personalToken));

                // var content = new FormUrlEncodedContent(values);

                var streamResult = await client.PostAsync(postUri, new FormUrlEncodedContent(parameters));

                var contents = await streamResult.Content.ReadAsStringAsync();

                return contents;

            }
            catch(Exception err)
            {
                throw err;
            }
        }

       public void TestApiKey(ModelGoogleDrive _googleDrive)
        {
            using (var servico = InitGDriveServiceWhithKey(_googleDrive))
            {
                var request = servico.Files.List();
                request.Fields = "files(id, name)";
                //request.Q = "mimeType='application/vnd.google-apps.folder'";
                // request.Q = "'17CYl9ni0xJEERcMZeKalLZ5D3Kp-vjw4' in parents";
                // request.Q = $"name contains '{folderName}'";
                //request.Spaces = "Srv Backup DataBases";
                var resultado = request.Execute();
                var arquivos = resultado.Files;
                               

                servico.Dispose();
            }
           
        }

        public static Google.Apis.Drive.v3.DriveService TesteService()
        {
            try
            {
                String serviceAccountEmail = "teste-868@servicesceltainfra.iam.gserviceaccount.com";

                var certificate = new X509Certificate2(@"servicesceltainfra-6f1e21301fbe.p12", "notasecret", X509KeyStorageFlags.Exportable);

                ServiceAccountCredential credential = new ServiceAccountCredential(
                   new ServiceAccountCredential.Initializer(serviceAccountEmail)
                   {
                       Scopes = new[] { "https://www.googleapis.com/auth/drive" }
                   }.FromCertificate(certificate));

                // Create the service.
                var service = new Google.Apis.Drive.v3.DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Service infra",
                });

                return service;
            }
            catch(Exception err)
            {
                throw err;
            }
        }
    }
}
