using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DbServices;

namespace DataLayerServices
{
    public interface IDataLayer
    {
        void DeleteDatabaseFiles(bool reset);
        Task<DbRaceSummary> GetRaceData(string category, Enums.TipoClassifica tipoClassifica, DateTime? proxRaceDate, DateTime? prevRaceDate, string token);
        (DateTime? proxRaceDate, DateTime? prevRaceDate) GetRaceDates(List<DbEvent> events);
        Task<DbCalendar> InitializeCalendar(string tipoClassifica, string token);
        Task<DbOffice> InitializeOffice(string tipoClassifica, string token);
        bool IsEndSeason(DbOffice dbOffice);
    }
}