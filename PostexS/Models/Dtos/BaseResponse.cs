using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostexS.Models.Enums;

namespace PostexS.Models.Dtos
{
    public class BaseResponse
    {

        private Errors Error = Errors.Success;
        public Errors ErrorCode
        {
            get
            {
                return Error;
            }
            set
            {
                Error = value;
                ErrorMessage = value.ToString();
            }
        }
        public string ErrorMessage { get; set; } = Errors.Success.ToString();
        public object Data { get; set; }
    }
}
