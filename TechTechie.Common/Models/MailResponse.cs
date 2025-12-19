using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Common.Models
{
    public class MailResponse
    {
        public required bool IsSuccess { get; set; }
        public required string Message { get; set; }

    };
}
