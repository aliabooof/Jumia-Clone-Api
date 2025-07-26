using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
   
        public interface IFileService
        {
            Task<string> SaveFileAsync(IFormFile file, string folder);
            bool IsValidImage(IFormFile file);
            bool IsValidDocument(IFormFile file);
            bool IsValidVoice(IFormFile file);
            void DeleteFile(string filePath);
    }
    
}
