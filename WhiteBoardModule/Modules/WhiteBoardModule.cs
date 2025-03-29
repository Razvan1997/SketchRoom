using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoardModule.Views;

namespace WhiteBoardModule.Modules
{
    public class WhiteBoardModule : IModule
    {
        private readonly IEventAggregator _eventAggregator;
        public WhiteBoardModule(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = ContainerLocator.Container.Resolve<IRegionManager>();
            //regionManager.RegisterViewWithRegion("WhiteBoardRegion", () => new WhiteBoardView());
            regionManager.RegisterViewWithRegion("ContentRegion", typeof(Views.WhiteBoardView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.WhiteBoardView>();
        }
    }
}
