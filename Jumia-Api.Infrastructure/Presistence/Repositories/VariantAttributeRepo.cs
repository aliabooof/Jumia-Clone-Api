using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Infrastructure.Presistence.Repositories
{
    public class VariantAttributeRepo : GenericRepo<VariantAttribute>, IVariantAttributeRepo
    {
        public VariantAttributeRepo(JumiaDbContext context) : base(context)
        {
        }
    }
}
