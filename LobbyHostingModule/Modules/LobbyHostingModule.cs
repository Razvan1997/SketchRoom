using LobbyHostingModule.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobbyHostingModule.Modules
{
    public class LobbyHostingModule : IModule
    {
        private readonly IEventAggregator _eventAggregator;

        public LobbyHostingModule(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = ContainerLocator.Container.Resolve<IRegionManager>();
            //regionManager.RegisterViewWithRegion("LobbyRegion", () => new LobbyView());
            regionManager.RegisterViewWithRegion("ContentRegion", typeof(Views.LobbyView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.LobbyView>();
        }
    }
}
