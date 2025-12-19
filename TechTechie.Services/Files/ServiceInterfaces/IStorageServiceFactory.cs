using TechTechie.Services.Files.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTechie.Services.Files.ServiceInterfaces
{
    public interface IStorageServiceFactory
    {
        IFileStorageService Get(string provider);
    }

}
