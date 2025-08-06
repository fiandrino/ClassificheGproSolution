using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbServices
{

    public static class DbContextFactory
    {
        public static IRacingContext CreateContext(string tipoClassifica)
        {
            return tipoClassifica switch
            {
               "it" => new DbItalianContext(),
               "fr" => new DbFranceseContext(),
               "ar" => new DbArgentinaContext(),
               "all" => new DbGlobalContext(),
                _ => throw new ArgumentException("Tipo classifica non supportato")
            };
        }
    }


}
