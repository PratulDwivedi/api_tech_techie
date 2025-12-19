using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TechTechie.PostgresRepository.Dynamic.Entities
{
    public class PageSectionEntity
    {
        public int id { get; set; }
        public int page_id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public int? display_mode_id { get; set; }
        public int display_order { get; set; }
        public int? child_display_mode_id { get; set; }
        public string? binding_name { get; set; }
        public int? binding_type_id { get; set; }
        public int? platform_id { get; set; }
        public Dictionary<string, object> data { get; set; }              // jsonb
        public Dictionary<string, object>? meta { get; set; }
        public AccessControlEntity? access_control { get; set; } 
        

        public List<PageSectionControlEntity>? controls { get; set; } = new List<PageSectionControlEntity>();
    }

    
}
