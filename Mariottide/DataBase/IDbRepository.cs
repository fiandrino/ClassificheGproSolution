using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mariottide
{
    public interface IDbRepository
    {
        Task<DbCalendar> ReadCalendar(TipoClassifica tipoClassifica);
        Task<DbRaceSummary> ReadLastRace(string category, TipoClassifica tipoClassifica);
        Task SaveCalendar(DbCalendar dbCalendar, TipoClassifica tipoClassifica);
        Task SaveRaceSummary(DbRaceSummary dbrace, List<DbManager> orderedManagers, TipoClassifica tipoClassifica);
        Task<Dictionary<int, int>> SommaPuntiGare(int season, string category, TipoClassifica tipoClassifica);
        public Task<DbOffice> ReadOffice(TipoClassifica tipoClassifica);
        public Task SaveOffice(DbOffice dbOffice, TipoClassifica tipoClassifica);
        Task ClearOfficeData(TipoClassifica tipoClassifica);
    }
}