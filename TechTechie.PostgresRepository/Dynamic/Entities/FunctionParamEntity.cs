using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.PostgresRepository.Dynamic.Entities
{
    public class FunctionParamEntity
    {
        public required string Name { get; set; }
        public required string DataType { get; set; }
        public required string FunctionReturnType { get; set; }
        public required bool HasDefault { get; set; }
    }
}
