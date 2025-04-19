using FooterModule.ViewModels;
using FooterModule.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooterModule.Modules
{
    public class FooterModule : IModule
    {
        private readonly IEventAggregator _eventAggregator;
        public FooterModule(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = ContainerLocator.Container.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion("FooterRegion", () => new FooterView());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<FooterViewModel>();
        }
    }
}
