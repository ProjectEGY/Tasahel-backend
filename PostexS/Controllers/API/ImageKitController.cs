using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using ZXing;
using System.IO;
using ZXing.Common;
using System.Drawing.Imaging;
using Imagekit.Sdk;

namespace PostexS.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageKitController : ControllerBase
    {
        private readonly BaseResponse baseResponse;

        public ImageKitController()
        {
            baseResponse = new BaseResponse();

        }

        [HttpPost("UploadImage")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadImage(ImageDetailsVM model)
        {
            ImagekitClient _imagekit = new ImagekitClient(model.PublicKey,
                               model.PrivateKey,
                                model.ImageUrl);
            FileCreateRequest request = new FileCreateRequest
            {
                file = model.file,
                fileName = model.fileName,
                folder = model.folder,
            };
            Result? result;

            try
            {
                result = _imagekit.Upload(request);
                ImageDetails dto = new ImageDetails()
                {
                    name = result.name,
                    fileId = result.fileId,
                };
                baseResponse.Data = dto;
                return Ok(baseResponse);
            }
            catch (Exception ex)
            {
                baseResponse.Data = null;
                return Ok(baseResponse);
            }

        }

        public class ImageDetails
        {
            public string name { get; set; }
            public string fileId { get; set; }

        }
        public class ImageDetailsVM
        {
            public string file { get; set; }
            public string fileName { get; set; }
            public string folder { get; set; }
            public string PrivateKey { get; set; }
            public string ImageUrl { get; set; }
            public string PublicKey { get; set; }

        }

    }
}
