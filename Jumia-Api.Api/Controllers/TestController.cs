using Jumia_Api.Application.Dtos.ProductDtos.Post;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly JumiaDbContext _dbContext;

        public TestController(JumiaDbContext dbContext)
        {
           _dbContext = dbContext;
        }

    
     

            
    }
}
