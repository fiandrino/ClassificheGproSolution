using DataLayerServices;
using DbServices;

namespace PrinterServices
{
    public interface IRaceResultPrinter
    {
        string DecodeUnicodeEntities(string input);
        void PrintResults(DbRaceSummary race, string category, Enums.TipoClassifica tipoClassifica, bool isSeasonStandings = false);
    }
}