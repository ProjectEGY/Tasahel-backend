using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PostexS.Models.Enums;

namespace PostexS.Helper
{
    public static class MediaControl
    {
        public static async Task<string> Upload(FilePath filePath, IFormFile file, IWebHostEnvironment _hostEnvironment)
        {
            var folderName = "";
            switch (filePath)
            {

                case FilePath.Users:
                    folderName = Path.Combine(_hostEnvironment.WebRootPath, "Images", "Users");
                    break;
                case FilePath.OrderReturns:
                    folderName = Path.Combine(_hostEnvironment.WebRootPath, "Images", "OrderReturns");
                    break;

            }
            string extension = Path.GetExtension(file.FileName);
            var UinqueName = Guid.NewGuid().ToString();
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
            using (var filestream = new FileStream(Path.Combine(folderName, UinqueName + extension), FileMode.Create))
            {
                await file.CopyToAsync(filestream);
            }
            return UinqueName + extension;
        }



        public static string Upload(FilePath filePath, byte[] image, MediaType mediaType, IWebHostEnvironment _hostEnvironment)
        {
            string folderPath = string.Empty;
            string fileName = string.Empty;
            if (image != null && image.Length > 0)
            {
                switch (mediaType)
                {
                    case MediaType.Image:
                        fileName = Guid.NewGuid().ToString() + ".jpg";
                        break;
                    case MediaType.Pdf:
                        fileName = Guid.NewGuid().ToString() + ".pdf";
                        break;
                }
                switch (filePath)
                {
                    case FilePath.Users:
                        folderPath = Path.Combine(_hostEnvironment.WebRootPath, "Images", "Users");
                        break;
                    case FilePath.OrderReturns:
                        folderPath = Path.Combine(_hostEnvironment.WebRootPath, "Images", "OrderReturns");
                        break;


                }
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                File.WriteAllBytes(Path.Combine(folderPath, fileName), image);
                return fileName;
            }
            return null;
        }

        public static string GetPath(FilePath filePath)
        {
            switch (filePath)
            {
                case FilePath.Users:
                    return "/Images/Users/";
                case FilePath.OrderReturns:
                    return "/Images/OrderReturns/";


                default:
                    return null;
            }
        }

    }
}
