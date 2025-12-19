
namespace TechTechie.Services.Common.Models
{
    public class TemplateModel
    {
        public int id { get; set; }
        public int template_type_id { get; set; }
        public int page_id { get; set; }
        public string? name { get; set; }
        public int language_id { get; set; }
        public int? page_orientation_id { get; set; }
        public Dictionary<string, object>? mail_controls { get; set; }
        public Dictionary<string, object>? mail_cc_user_ids { get; set; }
        public bool? is_enabled { get; set; }
        public string? page_header { get; set; }
        public string? page_body { get; set; }

        public string? page_footer { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public string merge_pdf_url { get; set; }

        public string water_mark_text { get; set; }

    }
}
