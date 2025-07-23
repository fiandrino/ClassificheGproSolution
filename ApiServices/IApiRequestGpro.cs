using System.Threading.Tasks;

namespace ApiServices
{
    public interface IApiRequestGpro
    {
        void Autenticazione(string token);
        Task<T> GetApiAsync<T>(string url);
        Task<T> GetApiAsync<T>(string url, string category);

    }
}