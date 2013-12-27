using Autofac;
using Nancy.Bootstrappers.Autofac;

namespace MMBot.Router.Nancy
{
    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private readonly IRouter _router;

        public Bootstrapper(IRouter router)
        {
            _router = router;
        }

        protected override void ConfigureApplicationContainer(ILifetimeScope container)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_router);
            builder.Update(container.ComponentRegistry);
        }
    }
}