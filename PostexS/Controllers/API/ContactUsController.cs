using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;

namespace PostexS.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUsController : ControllerBase
    {
        private readonly IGeneric<ContactUs> _contactUs;
        private readonly IGeneric<Branch> _branch;
        private readonly BaseResponse baseResponse;
        public ContactUsController(IGeneric<ContactUs> contactUs, IGeneric<Branch> branch)
        {
            _branch = branch;
            _contactUs = contactUs;
            baseResponse = new BaseResponse();
        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var branchs = _branch.Get(x => !x.IsDeleted).ToList();
            var contactus = await _contactUs.GetObj(x => !x.IsDeleted);
            var dto = new ContactUsDto();
            dto.FaceBook = contactus.FaceBook;
            dto.Twitter = contactus.Twitter;
            dto.Instgram = contactus.Instgram;
            foreach(var item in branchs)
            {
                dto.Branchs.Add(new BranchsDto()
                {
                    ID = item.Id,
                    Address = item.Address,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    Name = item.Name,
                    PhoneNumber = item.PhoneNumber,
                    Whatsapp = item.Whatsapp
                });
            }
            baseResponse.Data = dto;
            return Ok(baseResponse);
            
        }
    }
}
