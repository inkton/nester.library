/*
    Copyright (c) 2017 Inkton.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the Software
    is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Resources;
using System.Collections.Generic;
using Xamarin.Forms;
using Newtonsoft.Json;
using Inkton.Nest.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inkton.Nester.ViewModels
{
    public class ServicesViewModel : ViewModel
    {
        private ObservableCollection<AppService> _appServices;

        private string _selectedAppServiceTag = "nest-redbud";
        private ObservableCollection<AppServiceTier> _upgradableAppTiers;
        private AppServiceTier _upgradeAppServiceTier = null;

        private ServiceTableItem _appServiceTableItem;
        private ServiceTableItem _storageServiceTableItem;
        private ServiceTableItem _domainServiceTableItem;
        private ServiceTableItem _monitorServiceTableItem;
        private ServiceTableItem _batchServiceTableItem;
        private ServiceTableItem _trackServiceTableItem;

        public class ServiceTableItem : INotifyPropertyChanged
        {
            decimal _cost = 0M;

            public string Name { get; set; }

            public string ProvidedBy { get; set; }

            public string Period { get; set; }

            public decimal Cost
            {
                get { return _cost; }
                set {
                    _cost = value;
                    NotifyPropertyChanged();
                }
            }

            public string Type { get; set; }            

            public string[] FeaturesAll { get; set; }

            public string[] FeaturesIncluded { get; set; }

            public AppServiceTier Tier { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        public ServicesViewModel(App app) : base(app)
        {
            _appServices = new ObservableCollection<AppService>();
            _upgradableAppTiers = new ObservableCollection<AppServiceTier>();
        }

        public ObservableCollection<AppService> Services
        {
            get
            {
                return _appServices;
            }
            set
            {
                SetProperty(ref _appServices, value);
            }
        }

        public async Task InitAsync()
        {
            await QueryAppSubscriptions();

            CreateServicesTable();
        }

        public void CreateServicesTable()
        {
            AppServiceSubscription subscription;

            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "app");

            if (subscription != null)
            {
                if (_upgradeAppServiceTier != null)
                {
                    // If upgrading show the upgrade tier
                    _appServiceTableItem = ServicesViewModel.CreateServiceItem(
                        _upgradeAppServiceTier);
                }
                else
                {
                    _appServiceTableItem = ServicesViewModel.CreateServiceItem(
                        subscription.ServiceTier);
                }

                _selectedAppServiceTag = (subscription.ServiceTier.OwnedBy as AppService).Tag;
            }

            _storageServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "storage");

            if (subscription != null)
            {
                _storageServiceTableItem = ServicesViewModel.CreateServiceItem(
                    subscription.ServiceTier);
            }

            _domainServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "domain");

            if (subscription != null)
            {
                _domainServiceTableItem = ServicesViewModel.CreateServiceItem(
                    subscription.ServiceTier);
            }

            _monitorServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "monitor");

            if (subscription != null)
            {
                _monitorServiceTableItem = ServicesViewModel.CreateServiceItem(
                    subscription.ServiceTier);
            }

            _batchServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "batch");

            if (subscription != null)
            {
                _batchServiceTableItem = ServicesViewModel.CreateServiceItem(
                    subscription.ServiceTier);
            }

            _trackServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "track");

            if (subscription != null)
            {
                _trackServiceTableItem = ServicesViewModel.CreateServiceItem(
                    subscription.ServiceTier);
            }
        }

        public async Task<Cloud.ResultMultiple<AppService>> QueryServicesAsync(
            bool doCache = true, bool throwIfError = true)
        {
            AppService serviceSeed = new AppService();

            Cloud.ResultMultiple<AppService> result = await Cloud.ResultMultiple<AppService>.WaitForObjectAsync(
                Keeper.Service, throwIfError, serviceSeed, doCache);

            if (result.Code < 0)
            {
                return result;
            }

            _appServices = result.Data.Payload;
            AppServiceTier tierSeed = new AppServiceTier();
            Cloud.ResultMultiple<AppServiceTier> resultTier;

            foreach (AppService service in _appServices)
            {
                tierSeed.OwnedBy = service;

                resultTier = await Cloud.ResultMultiple<AppServiceTier>.WaitForObjectAsync(
                    Keeper.Service, throwIfError, tierSeed, doCache);

                if (result.Code == 0)
                {
                    service.Tiers = resultTier.Data.Payload;
                }
            }

            return result;
        }

        public async Task<Cloud.ResultMultiple<Forest>> QueryAppServiceTierLocationsAsync(AppServiceTier teir,
            bool doCache = true, bool throwIfError = true)
        {
            Forest forestSeeder = new Forest();
            forestSeeder.OwnedBy = teir;

            return await Cloud.ResultMultiple<Forest>.WaitForObjectAsync(
                Keeper.Service, throwIfError, forestSeeder, doCache);
        }

        public async Task<Cloud.ResultSingle<AppServiceSubscription>> CreateSubscription(AppServiceTier tier,
            bool doCache = true, bool throwIfError = true)
        {
            AppServiceSubscription subscription = new AppServiceSubscription();
            subscription.OwnedBy = _editApp;
            subscription.ServiceTier = tier;
            subscription.AppServiceTierId = tier.Id;

            return await Cloud.ResultSingle<AppServiceSubscription>.WaitForObjectAsync(
                throwIfError, subscription, new Cloud.CachedHttpRequest<AppServiceSubscription, Cloud.ResultSingle<AppServiceSubscription>>(
                    Keeper.Service.CreateAsync), doCache);
        }

        public async Task<Cloud.ResultSingle<AppServiceSubscription>> RemoveSubscriptionAsync(AppServiceSubscription subscription,
             bool doCache = false, bool throwIfError = true)
        {
            return await Cloud.ResultSingle<AppServiceSubscription>.WaitForObjectAsync(
                throwIfError, subscription, new Cloud.CachedHttpRequest<AppServiceSubscription, Cloud.ResultSingle<AppServiceSubscription>>(
                    Keeper.Service.RemoveAsync), doCache);
        }

        public async Task<Cloud.ResultMultiple<AppServiceSubscription>> QueryAppSubscriptions(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            if (!_appServices.Any())
            {
                await QueryServicesAsync();
            }

            AppServiceSubscription subSeeder = new AppServiceSubscription();
            subSeeder.OwnedBy = (app == null ? _editApp : app);

            Cloud.ResultMultiple<AppServiceSubscription> result = await Cloud.ResultMultiple<AppServiceSubscription>.WaitForObjectAsync(
                Keeper.Service, throwIfError, subSeeder, doCache);

            if (result.Code >= 0)
            {
                ObservableCollection<AppServiceSubscription> serviceSubscriptions = result.Data.Payload;

                foreach (AppServiceSubscription subscription in serviceSubscriptions)
                {
                    subscription.OwnedBy = subSeeder.OwnedBy;

                    foreach (AppService service in _appServices)
                    {
                        foreach (AppServiceTier tier in service.Tiers)
                        {
                            if (subscription.AppServiceTierId == tier.Id)
                            {
                                subscription.ServiceTier = tier;
                            }
                        }
                    }
                }

                (subSeeder.OwnedBy as App).Subscriptions = serviceSubscriptions;
            }

            return result;
        }

        public async Task<Cloud.ResultMultiple<AppServiceTier>> QueryAppUpgradeServiceTiersAsync(
            AppService service, Deployment deployment = null, bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;
            service.OwnedBy = theDeployment;

            AppServiceTier tierSeed = new AppServiceTier();
            tierSeed.OwnedBy = service;

            Cloud.ResultMultiple<AppServiceTier> result = await Cloud.ResultMultiple<AppServiceTier>.WaitForObjectAsync(
                Keeper.Service, throwIfError, tierSeed, doCache);

            if (result.Code >= 0)
            {
                _upgradableAppTiers = result.Data.Payload;
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppServiceTier>> UpdateAppUpgradeServiceTiersAsync(
            AppService service = null, AppServiceTier tierSeed = null, Deployment deployment = null,
            bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;
            service.OwnedBy = theDeployment;
            _upgradeAppServiceTier.OwnedBy = service;

            return await Cloud.ResultSingle<AppServiceTier>.WaitForObjectAsync(
                throwIfError, _upgradeAppServiceTier, new Cloud.CachedHttpRequest<AppServiceTier, Cloud.ResultSingle<AppServiceTier>>(
                    Keeper.Service.UpdateAsync), doCache);
        }

        public static ServiceTableItem CreateServiceItem(AppServiceTier tier)
        {
            ServiceTableItem item = new ServiceTableItem();

            item.Name = tier.Name;
            item.ProvidedBy = (tier.OwnedBy as AppService).Name;
            item.Period = tier.Period;
            item.Cost = tier.ItemCost;
            item.Type = tier.Type;
            item.FeaturesAll = TranslateFeaturesAll(tier.OwnedBy as AppService);
            item.FeaturesIncluded = TranslateFeaturesIncluded(tier);
            item.Tier = tier;

            return item;
        }

        public static ObservableCollection<ServiceTableItem> CreateServicesTable(
            ObservableCollection<AppServiceTier> tiers)
        {
            ObservableCollection<ServiceTableItem> table = 
                new ObservableCollection<ServiceTableItem>();

            foreach (var tier in tiers)
            {
                table.Add(CreateServiceItem(tier));
            }

            return table;
        }

        public static string[] TranslateFeaturesAll(AppService service)
        {
            List<string> values = JsonConvert.DeserializeObject<List<string>>(
                service.FeaturesAll);
                
            ResourceManager resmgr = (Application.Current as INesterControl)
                .GetResourceManager();

            List<string> TranslatedValues = new List<string>();
            foreach (string value in values)
            {
                var translation = resmgr.GetString(value,
                    System.Globalization.CultureInfo.CurrentUICulture);
                TranslatedValues.Add(translation);
            }

            return TranslatedValues.ToArray<string>();
        }

        public static string[] TranslateFeaturesIncluded(AppServiceTier tier)
        {
            List<string> values = JsonConvert.DeserializeObject<List<string>>(
                tier.FeaturesIncluded);
            return values.ToArray<string>();
        }

        #region App Tier

        public ObservableCollection<AppServiceTier> UpgradableAppTiers
        {
            get
            {
                return _upgradableAppTiers;
            }
            set
            {
                SetProperty(ref _upgradableAppTiers, value);
            }
        }

        public AppServiceTier UpgradeAppServiceTier
        {
            get
            {
                return _upgradeAppServiceTier;
            }
            set
            {
                SetProperty(ref _upgradeAppServiceTier, value);
            }
        }

        public string SelectedAppserviceTag
        {
            get
            {
                return _selectedAppServiceTag;
            }
            set
            {
                SetProperty(ref _selectedAppServiceTag, value);
                OnPropertyChanged("AppFeaturesTable");
            }
        }

        public ObservableCollection<AppServiceTier> AppServiceTiers
        {
            get
            {
                AppService service = Services.FirstOrDefault(
                    x => x.Tag == _selectedAppServiceTag);

                if (service != null)
                {
                    return service.Tiers;
                }

                return null;
            }
        }

        public ObservableCollection<ServicesViewModel.ServiceTableItem> AppFeaturesTable
        {
            get
            {
                if (_upgradeAppServiceTier != null)
                {
                    return CreateServicesTable(_upgradableAppTiers);
                }
                else
                {
                    return CreateServicesTable(AppServiceTiers);
                }
            }
        }

        public ServiceTableItem SelectedAppService
        {
            get
            {
                return _appServiceTableItem;
            }
        }

        public string[] SelectedAppServiceFeatures
        {
            get
            {
                return TranslateFeaturesAll(
                    (SelectedAppService.Tier.OwnedBy as AppService));
            }
        }

        public string[] SelectedAppServiceTierIncludes
        {
            get
            {
                return TranslateFeaturesIncluded(
                    SelectedAppService.Tier);
            }
        }

        public async Task SwitchAppServiceTierAsync(AppServiceTier newTier)
        {
            AppServiceSubscription subscription = EditApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "app");

            if (subscription != null)
            {
                await RemoveSubscriptionAsync(subscription);
            }

            await CreateSubscription(newTier);
            await QueryAppSubscriptions();
            CreateServicesTable();
        }

        #endregion  

        #region Storage Service Tier

        public bool StorageServiceEnabled
        {
            get
            {
                return SelectedStorageService != null;
            }
        }

        public ObservableCollection<AppServiceTier> StorageServiceTiers
        {
            get
            {
                AppService service = Services.FirstOrDefault(
                    x => x.Type == "storage");

                if (service != null)
                {
                    return service.Tiers;
                }

                return null;
            }
        }

        public ServiceTableItem SelectedStorageService
        {
            get
            {
                return _storageServiceTableItem;
            }
        }

        public async Task CreateDefaultStorageServiceAsync()
        {
            // Only one tier available at present
            AppServiceTier defaultTier = StorageServiceTiers.First();
            await CreateSubscription(defaultTier);
            await QueryAppSubscriptions();
            CreateServicesTable();
        }

        public async Task RemoveDefaultStorageServiceAsync()
        {
            AppServiceSubscription subscription = EditApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "storage");

            if (subscription != null)
            {
                await RemoveSubscriptionAsync(subscription);
            }

            await QueryAppSubscriptions();
            CreateServicesTable();
        }

        #endregion

        #region Domain Service Tier

        public ServiceTableItem SelectedDomainService
        {
            get
            {
                return _domainServiceTableItem;
            }
        }

        #endregion

        #region Monitor Service Tier

        public ServiceTableItem SelectedMonitorService
        {
            get
            {
                return _monitorServiceTableItem;
            }
        }

        #endregion

        #region Batch Service Tier

        public ServiceTableItem SelectedBatchService
        {
            get
            {
                return _batchServiceTableItem;
            }
        }

        #endregion

        #region Track Service Tier

        public ServiceTableItem SelectedTrackService
        {
            get
            {
                return _trackServiceTableItem;
            }
        }

        #endregion
    }
}
