using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.AI.Providers.Models
{
    public record AIResponseModel(string Provider, string Model, string Output, DateTime Timestamp);

}
