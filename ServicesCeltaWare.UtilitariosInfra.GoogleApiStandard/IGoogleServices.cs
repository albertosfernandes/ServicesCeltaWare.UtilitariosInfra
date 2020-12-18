using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServicesCeltaWare.UtilitariosInfra.GoogleApiStandard
{
    public interface IGoogleServices
    {
        DriveService InitGDriveComputer(UserCredential credenciais);
        Task<string> Upload(string _fileName, string _path, string _folderId, UserCredential userCredential);
        Task<Google.Apis.Drive.v3.Data.File> FindInGoogleDrive(string _folderId, string _fileName, UserCredential _userCredential);
    }
}
