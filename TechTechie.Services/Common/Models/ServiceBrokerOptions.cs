using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Common.Models
{
    public class ServiceBrokerOptions
    {
        public bool MsSqlEnabled { get; set; }
        public bool PostgreSqlEnabled { get; set; }
        public int PollingIntervalSeconds { get; set; } = 5;
        public int MaxRetryAttempts { get; set; } = 5;
        public int RetryDelaySeconds { get; set; } = 10;
        public int MaxWorkers { get; set; } = 10;
        public string LogFilePath { get; set; } = "C:\\TechTechieServiceBroker\\logs.txt";

    }
}
