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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Newtonsoft.Json;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class ServicesViewModel<UserT> : ViewModel<UserT>
        where UserT : User, new()
    {
        private static ObservableCollection<AppService> _appServices;
        public const string DefaultAppServiceTag = "nest-redbud";
        private ObservableCollection<AppServiceTier> _upgradableAppTiers;
        private AppServiceTier _upgradeAppServiceTier;

        public class ServiceTableItem : INotifyPropertyChanged
        {
            /* 
             * A table for presenting (UX) service tiers
             */

            decimal _cost;

            public string Tag { get; set; }

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

        public ServicesViewModel(BackendService<UserT> backend, App app) : base(backend, app)
        {
            _upgradableAppTiers = new ObservableCollection<AppServiceTier>();
            AppTierTable = new ObservableCollection<ServiceTableItem>();
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
            if (_appServices == null)
            {
                // Only needed once
                await QueryServicesAsync(Backend);
            }

            await QueryAppSubscriptions();

            CreateServicesTables();
        }

        public void CreateServicesTables()
        {
            ResetAppServiceTable();

            AppServiceSubscription subscription;

            SelectedStorageServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "storage");

            if (subscription != null)
            {
                SelectedStorageServiceTableItem = ServicesViewModel<UserT>.CreateServiceTableItem(
                    subscription.ServiceTier);
            }

            SelectedDomainServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "domain");

            if (subscription != null)
            {
                SelectedDomainServiceTableItem = ServicesViewModel<UserT>.CreateServiceTableItem(
                    subscription.ServiceTier);
            }

            SelectedMonitorServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "monitor");

            if (subscription != null)
            {
                SelectedMonitorServiceTableItem = ServicesViewModel<UserT>.CreateServiceTableItem(
                    subscription.ServiceTier);
            }

            SelectedBatchServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "batch");

            if (subscription != null)
            {
                SelectedBatchServiceTableItem = ServicesViewModel<UserT>.CreateServiceTableItem(
                    subscription.ServiceTier);
            }

            SelectedTrackServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "track");

            if (subscription != null)
            {
                SelectedTrackServiceTableItem = ServicesViewModel<UserT>.CreateServiceTableItem(
                    subscription.ServiceTier);
            }

            SelectedBuildServiceTableItem = null;
            subscription = _editApp.Subscriptions.FirstOrDefault(
                x => (x.ServiceTier.OwnedBy as AppService).Type == "build");

            if (subscription != null)
            {
                SelectedBuildServiceTableItem = ServicesViewModel<UserT>.CreateServiceTableItem(
                    subscription.ServiceTier);
            }
        }

        public static async Task QueryServicesAsync(BackendService<UserT> backend)
        {
            AppService serviceSeed = new AppService();

            ResultMultiple<AppService> result = await ResultMultipleUI<AppService>.WaitForObjectsAsync(
                true, serviceSeed, new CachedHttpRequest<AppService, ResultMultiple<AppService>>(
                    backend.QueryAsyncListAsync), true);

            if (result.Code < 0)
            {
                return;
            }

            _appServices = result.Data.Payload;
            AppServiceTier tierSeed = new AppServiceTier();
            ResultMultiple<AppServiceTier> resultTier;

            foreach (AppService service in _appServices)
            {
                tierSeed.OwnedBy = service;

                resultTier = await ResultMultipleUI<AppServiceTier>.WaitForObjectsAsync(
                    true, tierSeed, new CachedHttpRequest<AppServiceTier, ResultMultiple<AppServiceTier>>(
                        backend.QueryAsyncListAsync), true);

                if (result.Code == 0)
                {
                    service.Tiers = resultTier.Data.Payload;
                }
            }
        }

        public async Task<ResultMultiple<Forest>> QueryAppServiceTierLocationsAsync(AppServiceTier teir,
            bool doCache = true, bool throwIfError = true)
        {
            Forest forestSeeder = new Forest();
            forestSeeder.OwnedBy = teir;

            return await ResultMultipleUI<Forest>.WaitForObjectsAsync(
                true, forestSeeder, new CachedHttpRequest<Forest, ResultMultiple<Forest>>(
                    Backend.QueryAsyncListAsync), true);
        }

        public async Task<ResultSingle<AppServiceSubscription>> CreateSubscription(AppServiceTier tier,
            bool doCache = true, bool throwIfError = true)
        {
            AppServiceSubscription subscription = new AppServiceSubscription();
            subscription.OwnedBy = _editApp;
            subscription.ServiceTier = tier;
            subscription.AppServiceTierId = tier.Id;

            return await ResultSingleUI<AppServiceSubscription>.WaitForObjectAsync(
                throwIfError, subscription, new CachedHttpRequest<AppServiceSubscription, ResultSingle<AppServiceSubscription>>(
                    Backend.CreateAsync), doCache);
        }

        public async Task<ResultSingle<AppServiceSubscription>> RemoveSubscriptionAsync(AppServiceSubscription subscription,
             bool doCache = false, bool throwIfError = true)
        {
            return await ResultSingleUI<AppServiceSubscription>.WaitForObjectAsync(
                throwIfError, subscription, new CachedHttpRequest<AppServiceSubscription, ResultSingle<AppServiceSubscription>>(
                    Backend.RemoveAsync), doCache);
        }

        public async Task<ResultMultiple<AppServiceSubscription>> QueryAppSubscriptions(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppServiceSubscription subSeeder = new AppServiceSubscription();
            subSeeder.OwnedBy = (app == null ? _editApp : app);

            ResultMultiple<AppServiceSubscription> result = await ResultMultipleUI<AppServiceSubscription>.WaitForObjectsAsync(
                true, subSeeder, new CachedHttpRequest<AppServiceSubscription, ResultMultiple<AppServiceSubscription>>(
                    Backend.QueryAsyncListAsync), true);

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

        public async Task SetAppUpgradingAsync(bool upgrading = true)
        {
            _upgradeAppServiceTier = null;

            if (upgrading)
            {
                await QueryAppUpgradeServiceTiersAsync();
            }
            else
            {
                _upgradableAppTiers.Clear();
            }

            ResetAppServiceTable();
        }

        public async Task<ResultMultiple<AppServiceTier>> QueryAppUpgradeServiceTiersAsync(
            AppService service = null, Deployment deployment = null, bool doCache = true, bool throwIfError = true)
        {
            AppService theService = service == null ? AppService : service;

            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;
            theService.OwnedBy = theDeployment;

            AppServiceTier tierSeed = new AppServiceTier();
            tierSeed.OwnedBy = theService;

            ResultMultiple<AppServiceTier> result = await ResultMultipleUI<AppServiceTier>.WaitForObjectsAsync(
                true, tierSeed, new CachedHttpRequest<AppServiceTier, ResultMultiple<AppServiceTier>>(
                    Backend.QueryAsyncListAsync), true);

            if (result.Code >= 0)
            {
                _upgradableAppTiers = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultSingle<AppServiceTier>> UpdateAppUpgradeServiceTierAsync(
            AppService service = null, AppServiceTier teir = null, Deployment deployment = null,
            bool doCache = true, bool throwIfError = true)
        {
            AppService theService = service == null ? AppService : service;
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;
            AppServiceTier theTier = teir == null ? _upgradeAppServiceTier : teir;

            theService.OwnedBy = theDeployment;

            _upgradeAppServiceTier.OwnedBy = theService;

            return await ResultSingleUI<AppServiceTier>.WaitForObjectAsync(
                throwIfError, theTier, new CachedHttpRequest<AppServiceTier, ResultSingle<AppServiceTier>>(
                    Backend.UpdateAsync), doCache);
        }

        public static ServiceTableItem CreateServiceTableItem(AppServiceTier tier)
        {
            ServiceTableItem item = new ServiceTableItem();

            item.Tag = tier.Tag;
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

        public static ObservableCollection<ServiceTableItem> CreateServiceTable(
            ObservableCollection<AppServiceTier> tiers)
        {
            ObservableCollection<ServiceTableItem> table = 
                new ObservableCollection<ServiceTableItem>();

            foreach (var tier in tiers)
            {
                table.Add(CreateServiceTableItem(tier));
            }

            return table;
        }

        public static string[] TranslateFeaturesAll(AppService service)
        {
            List<string> values = JsonConvert.DeserializeObject<List<string>>(
                service.FeaturesAll);
                
            ResourceManager resmgr = (Application.Current as INesterClient)
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

        public AppService AppService
        {
            get
            {
                return Services.FirstOrDefault(
                    x => x.Tag == SelectedAppServiceTag);
            }
        }

        public ObservableCollection<ServicesViewModel<UserT>.ServiceTableItem> AppTierTable { get; }

        public ServiceTableItem SelectedAppServiceTableItem { get; private set; }

        public string[] SelectedAppServiceFeatures
        {
            get
            {
                return TranslateFeaturesAll(
                    (SelectedAppServiceTableItem.Tier.OwnedBy as AppService));
            }
        }

        public string[] SelectedAppServiceTierIncludes
        {
            get
            {
                return TranslateFeaturesIncluded(
                    SelectedAppServiceTableItem.Tier);
            }
        }

        public string SelectedAppServiceTag { get; private set; } = DefaultAppServiceTag;

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

            ResetAppServiceTable();
        }

        public void UpgradeAppServiceTier(AppServiceTier newTier)
        {
            _upgradeAppServiceTier = newTier;

            ResetAppServiceTable();
        }

        public void ResetAppServiceTable()
        {
            ObservableCollection<AppServiceTier> appTiers = null;
            AppServiceTier selectTier = null;

            if (_upgradableAppTiers.Any())
            {
                appTiers = _upgradableAppTiers;
                selectTier = _upgradeAppServiceTier;
            }
            else
            {
                if (_editApp.Subscriptions != null)
                {
                    var subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => (x.ServiceTier.OwnedBy as AppService).Type == "app");

                    selectTier = subscription.ServiceTier;
                    SelectedAppServiceTag = (selectTier.OwnedBy as AppService).Tag;
                }
                else
                {
                    SelectedAppServiceTag = DefaultAppServiceTag;
                }

                appTiers = AppService.Tiers;
            }

            AppTierTable.Clear();
            ServiceTableItem tableItem = null;

            foreach (var tier in appTiers)
            {
                tableItem = CreateServiceTableItem(tier);
                AppTierTable.Add(tableItem);

                if (selectTier != null &&
                    selectTier.Id == tableItem.Tier.Id)
                {
                    SelectedAppServiceTableItem = tableItem;
                }
            }

            OnPropertyChanged("AppTierTable");
        }

        public void BuildAppServiceTable(string appSerivceTag)
        {
            AppTierTable.Clear();
            ServiceTableItem tableItem = null;
            AppService service = Services.FirstOrDefault(
                x => x.Tag == appSerivceTag);

            AppServiceSubscription subscription = null;
            SelectedAppServiceTableItem = null;

            if (_editApp.Subscriptions != null)
            {
                subscription = _editApp.Subscriptions.FirstOrDefault(
                    x => (x.ServiceTier.OwnedBy as AppService).Type == "app");
            }

            foreach (var tier in service.Tiers)
            {
                tableItem = CreateServiceTableItem(tier);
                AppTierTable.Add(tableItem);

                if (subscription != null &&
                    subscription.ServiceTier.Id == tableItem.Tier.Id)
                {
                    SelectedAppServiceTableItem = tableItem;
                }
            }

            OnPropertyChanged("AppTierTable");
        }

        #endregion  

        #region Storage Service Tier

        public bool StorageServiceEnabled
        {
            get
            {
                return SelectedStorageServiceTableItem != null;
            }
        }

        public AppService StorageService
        {
            get
            {
                return Services.FirstOrDefault(
                    x => x.Type == "storage");
            }
        }

        public ServiceTableItem SelectedStorageServiceTableItem { get; private set; }

        public async Task CreateDefaultStorageServiceAsync()
        {
            // Only one tier available at present
            AppServiceTier defaultTier = StorageService.Tiers.First();
            await CreateSubscription(defaultTier);
            await QueryAppSubscriptions();

            CreateServicesTables();
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

            CreateServicesTables();
        }

        #endregion

        #region Domain Service Tier

        public ServiceTableItem SelectedDomainServiceTableItem { get; private set; }

        #endregion

        #region Monitor Service Tier

        public ServiceTableItem SelectedMonitorServiceTableItem { get; private set; }

        #endregion

        #region Batch Service Tier

        public ServiceTableItem SelectedBatchServiceTableItem { get; private set; }

        #endregion

        #region Track Service Tier

        public ServiceTableItem SelectedTrackServiceTableItem { get; private set; }

        #endregion

        #region Build Service Tier

        public ServiceTableItem SelectedBuildServiceTableItem { get; private set; }

        #endregion

        public decimal CalculateServiceCost()
        {
            decimal total = SelectedAppServiceTableItem.Cost +
                    SelectedTrackServiceTableItem.Cost +
                    SelectedDomainServiceTableItem.Cost +
                    SelectedMonitorServiceTableItem.Cost +
                    SelectedBatchServiceTableItem.Cost +
                    SelectedBuildServiceTableItem.Cost;

            if (StorageServiceEnabled)
            {
                total += SelectedAppServiceTableItem.Cost;
            }

            return total;
        }
    }
}
