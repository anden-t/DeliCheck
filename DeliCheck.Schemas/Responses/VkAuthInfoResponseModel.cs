using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliCheck.Schemas.Responses
{
    internal class VkAuthInfoResponseModel
    {
        public int AppId { get; set; }
        public string Scope { get; set; }
        public string CodeChallenge { get; set; }
        public string State { get; set; }
        public string RedirectUrl { get; set; }
    }
}
