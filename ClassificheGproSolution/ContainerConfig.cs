

using Autofac;

namespace ClassificheGproSolution
{
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<BusinessLogic>().As<IBusinessLogic>();
            builder.RegisterType<ApiRequestGpro>().As<IApiRequestGpro>();
            builder.RegisterType<Registrazione>().As <IRegistrazione>();
            builder.RegisterType<LocalizationManager>().As<ILocalizationManager>().SingleInstance();
            builder.RegisterType<DbRepository>().As<IDbRepository>();
            builder.RegisterType<RaceResultsPrinter>().As<IRaceResultsPrinter>();
            return builder.Build();
        }
    }
}
