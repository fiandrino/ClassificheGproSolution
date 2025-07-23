

using System.Threading.Tasks;

namespace Mariottide
{
    public class Application : IApplication
    {
        IBusinessLogic _businessLogic;

        public Application(IBusinessLogic businessLogic)
        {
            _businessLogic = businessLogic;
        }

        public async Task Run()
        {
            await _businessLogic.ProcessData();
        }
    }
}

