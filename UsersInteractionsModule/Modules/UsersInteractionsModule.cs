using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsersInteractionsModule.ViewModels;
using UsersInteractionsModule.Views;

namespace UsersInteractionsModule.Modules
{
    public class UsersInteractionsModule : IModule
    {
        private readonly IEventAggregator _eventAggregator;
        public UsersInteractionsModule(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = ContainerLocator.Container.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion("UsersInteractionsRegion", () => new UsersInteractionsView());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<UsersInteractionsViewModel>();
        }
    }
}
