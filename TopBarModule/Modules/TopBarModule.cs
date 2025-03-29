using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopBarModule.ViewModels;
using TopBarModule.Views;

namespace TopBarModule.Modules
{
    public class TopBarModule : IModule
    {
        private readonly IEventAggregator _eventAggregator;

        public TopBarModule(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = ContainerLocator.Container.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion("TopBarRegion", () => new TopBarView());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
