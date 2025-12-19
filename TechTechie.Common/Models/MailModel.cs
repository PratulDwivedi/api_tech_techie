

namespace TechTechie.Common.Models
{
    public class MailModel
    {
        public string? ToRecipients { get; set; } // comma or semicolon seperated
        public string? CcRecipients { get; set; } // comma or semicolon seperated
        public string? BccRecipients { get; set; } // comma or semicolon seperated

        public required string FromEMail { get; set; }
        public required string FromEMailDisplayName { get; set; }
        public required string Subject { get; set; }
        public required string HtmlBody { get; set; }

        public List<MailAttachmentModel>? Attachments { get; set; }
    }

    public class MailAttachmentModel
    {
        public required string Name { get; set; }
        public required string ContentBytes { get; set; } // base64, byte array string array
        public string? ContentType { get; set; } = "application/pdf";// "application/octet-stream";
    }

}
