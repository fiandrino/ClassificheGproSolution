using System;
using System.Threading.Tasks;
using Autofac;

namespace Mariottide
{
    class Program
    {
        static async Task Main()
        {
            var container = ContainerConfig.Configure();
            using (var scope = container.BeginLifetimeScope())
            {
                var app = scope.Resolve<IApplication>();
                await app.Run();
            }
        }
 
    }
}