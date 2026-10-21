using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ClassificheGproSolution
{

    public class ApiRepository<T>
    {
        IApiRequestGpro _apiRequestGpro;
        IRegistrazione _registrazione;
        public ApiRepository(IApiRequestGpro apiRequestGpro, IRegistrazione registrazione)
        {
            _apiRequestGpro = apiRequestGpro;
            _registrazione = registrazione;
            _apiRequestGpro.Autenticazione(_registrazione.ReadToken());
        }
 
        public async Task<T> GetApi(string urlBase, string category)
        {
            return await _apiRequestGpro.GetApiAsync<T>(urlBase, category);
        }

        public async Task<T> GetApi(string urlBase)
        {
            return await _apiRequestGpro.GetApiAsync<T>(urlBase);
        }

    }
}
