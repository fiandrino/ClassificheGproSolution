using System.Collections.Generic;
using System.Threading.Tasks;

namespace DbServices
{
    public interface IDbRepository
    {
        Task<DbCalendar> ReadCalendar(string tipoClassifica);
        Task<DbRaceSummary> ReadLastRace(string category, string tipoClassifica);
        Task SaveCalendar(DbCalendar dbCalendar, string tipoClassifica);
        Task SaveRaceSummary(DbRaceSummary dbrace, List<DbManager> orderedManagers, string tipoClassifica);
        Task<Dictionary<int, int>> SommaPuntiGare(int season, string category, string tipoClassifica);
        public Task<DbOffice> ReadOffice(string tipoClassifica);
        public Task SaveOffice(DbOffice dbOffice, string tipoClassifica);
        Task ClearOfficeData(string tipoClassifica);
        bool IsEndSeason(DbOffice dbOffice);
        void DeleteDatabaseFiles(bool reset);
    }
}