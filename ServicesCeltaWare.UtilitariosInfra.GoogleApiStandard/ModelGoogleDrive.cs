using System;
using System.Collections.Generic;
using System.Text;

namespace ServicesCeltaWare.UtilitariosInfra
{
    public class ModelGoogleDrive
    {
        private static ModelGoogleDrive modelGoogleDrive = new ModelGoogleDrive();
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public Double Size { get; set; }

        public static void ExistNewFile()
        {
            //IList<ModelGoogleDrive> listOfFiles = GetFiles();
            //retornar Id de arquivo caso existir arquivo novo
        }

        //public static IList<ModelGoogleDrive> GetFiles()
        //{
        //    IList<ModelGoogleDrive> listOfFiles = null;

        //}

        public static IList<string> GetFolders(string name)
        {
            return CallManager.ListFolders(name);
        }
    }
}
