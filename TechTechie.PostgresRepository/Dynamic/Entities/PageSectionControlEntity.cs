using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.PostgresRepository.Dynamic.Entities
{
    public class PageSectionControlEntity
    {
        public int id { get; set; }
        public int section_id { get; set; }
        public int control_type_id { get; set; }
        public string? name { get; set; }
        public string? binding_name { get; set; }
        public int? binding_list_page_id { get; set; }
        public int? display_mode_id { get; set; }
        public int display_order { get; set; }
        public string? cascade_from_binding_name { get; set; }
        public int? platform_id { get; set; }
        public Dictionary<string, object>? data { get; set; }
        public Dictionary<string, object>? meta { get; set; }
        public AccessControlEntity? access_control { get; set; }
    }
   


}
