using System.Text.RegularExpressions;


namespace TechTechie.PostgresRepository
{
    public class StringHelper
    {
        

        public static List<string> EmailAddress(string emailIdsString)
        {
            List<string> emailIds = new List<string>();
            if (!String.IsNullOrEmpty(emailIdsString))
            {
                emailIdsString = Regex.Replace(emailIdsString, @"\t|\n|\r", "");
                emailIdsString = Regex.Replace(emailIdsString, ",", ";");
                if (emailIdsString.Contains(";"))
                {
                    string[] str = emailIdsString.Split(';');
                    for (int i = 0; i < str.Length; i++)
                    {
                        string email = str[i].Trim();
                        if (email != String.Empty)
                        {
                            if (ValidateEmail(email))
                                emailIds.Add(email);
                        }
                    }
                }
                else
                {
                    if (ValidateEmail(emailIdsString))
                        emailIds.Add(emailIdsString);
                }
            }
            return emailIds;
        }

        public static bool ValidateEmail(string email)
        {
            return Regex.IsMatch(email.Trim(), @"\A(?:[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)\Z");
            ;
        }

        public static string GetMimeTypes(string fileName)
        {
            string ext = fileName.Split('.')[1];

            switch (ext)
            {
                case "txt":
                    return "text/plain";
                case "pdf":
                    return "application/pdf";
                case "xls":
                    return "application/vnd.ms-excel";
                case "xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case "csv":
                    return "text/csv";
                default:
                    return "text/plain";
            }
        }

        
    }
}