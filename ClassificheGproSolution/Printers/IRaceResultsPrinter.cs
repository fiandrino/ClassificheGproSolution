using System.Collections.Generic;

namespace ClassificheGproSolution
{
    public interface IRaceResultsPrinter
    {
        string DecodeUnicodeEntities(string input);
        void PrintResults(DbRaceSummary race, string category, TipoClassifica tipoClassifica, int requestRem, bool isSeasonStandings = false);
    }
}