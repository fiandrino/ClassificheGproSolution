using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace ClassificheGproSolution
{
    public class ApiRequestGpro : IApiRequestGpro
    {
        private  HttpClient _httpClient;

        public void Autenticazione(string token)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T> GetApiAsync<T>(string url, string category)
        {
            url = $"{url}?group={category}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                return default;
            }
        }

        public async Task<T> GetApiAsync<T>(string url)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                return default;
            }
        }
    }

}
