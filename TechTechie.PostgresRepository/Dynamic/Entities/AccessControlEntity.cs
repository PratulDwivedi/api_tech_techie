using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace TechTechie.PostgresRepository.Dynamic.Entities
{
    public class AccessControlEntity
    {
        public int[] role_id { get; set; }
        public int[] user_id { get; set; }
    }
}
