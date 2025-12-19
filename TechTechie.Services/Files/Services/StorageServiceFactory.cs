using TechTechie.Services.Files.Models;
using TechTechie.Services.Files.ServiceInterfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Files.Services
{
    public class StorageServiceFactory : IStorageServiceFactory
    {
        private readonly IServiceProvider _provider;

        public StorageServiceFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IFileStorageService Get(string provider)
        {
            if (provider == "microsoft-azure-blob")
            {
                return new AzureBlobStorageService();
            }
            else if (provider == "local-storage")
            {
                return new LocalStorageService();
            }
            else
            {
                throw new NotImplementedException($"Storage provider {provider} is not implemented.");

            }
        }

    }
}