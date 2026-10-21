namespace ClassificheGproSolution
{
    public interface ILocalizationManager
    {
        string GetString(string key, params object[] args);
        void SetCulture(string cultureCode);
    }
}