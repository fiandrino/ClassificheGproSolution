using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace ClassificheGproSolution
{
    public class LocalizationManager : ILocalizationManager
    {
        private ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        public LocalizationManager()
        {
            // Namespace + nome base dei file di risorse
            string baseName = "ClassificheGproSolution.Resources.Strings";
            _resourceManager = new ResourceManager(baseName, Assembly.GetExecutingAssembly());

            // Imposta cultura predefinita (italiano)
            _currentCulture = new CultureInfo("it");
        }
        public void SetCulture(string cultureCode)
        {
            try
            {
                _currentCulture = new CultureInfo(cultureCode);
            }
            catch
            {
                _currentCulture = CultureInfo.InvariantCulture;
            }
        }
        public string GetString(string key, params object[] args)
        {
            try
            {
                string value = _resourceManager.GetString(key, _currentCulture);
                if (value == null) return $"[{key}]"; // Key mancante

                return args.Length > 0
                    ? string.Format(value, args)
                    : value;
            }
            catch
            {
                return $"[{key}]";
            }
        }

    }
}
