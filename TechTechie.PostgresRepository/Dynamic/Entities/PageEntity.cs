using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace TechTechie.PostgresRepository.Dynamic.Entities
{
    public class PageEntity
    {
        public int id { get; set; }
        public int? module_id { get; set; }
        public string? name { get; set; }
        public string? descr { get; set; }
        public string? RouteName { get; set; }
        public int? parent_page_id { get; set; }
        public int display_location_id { get; set; }
        public int display_order { get; set; }
        public int? page_type_id { get; set; }
        public string? binding_name_post { get; set; }
        public string? binding_name_get { get; set; }
        public string? binding_name_delete { get; set; }
        public string? binding_id_name { get; set; }
        public int binding_type_id { get; set; }
        public int platform_id { get; set; }
        public Dictionary<string, object>? data { get; set; } 
        public Dictionary<string, object>? meta { get; set; }
        public AccessControlEntity? access_control { get; set; }
        public List<PageSectionEntity>? sections { get; set; } = new List<PageSectionEntity>();
    }
   
}
