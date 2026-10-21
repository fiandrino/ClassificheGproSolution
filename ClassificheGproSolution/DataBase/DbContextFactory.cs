using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ClassificheGproSolution
{

    public static class DbContextFactory
    {
        public static IRacingContext CreateContext(TipoClassifica tipoClassifica)
        {
            return tipoClassifica switch
            {
                TipoClassifica.it => new DbItalianContext(),
                TipoClassifica.all => new DbGlobalContext(),
                _ => throw new ArgumentException("Tipo classifica non supportato")
            };
        }
    }


}
