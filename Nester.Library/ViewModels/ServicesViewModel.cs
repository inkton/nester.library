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
using Newtonsoft.Json;
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class ServicesViewModel : ViewModel
    {
        private ObservableCollection<AppService> _appServices;

        public ServicesViewModel(App app) : base(app)
        {
            _appServices = new ObservableCollection<AppService>();
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

        override public async Task<Cloud.ServerStatus> InitAsync()
        {
            Cloud.ServerStatus status;

            status = await QueryAppSubscriptions();
            if (status.Code < 0)
            {
                return status;
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryServicesAsync(
            bool doCache = true, bool throwIfError = true)
        {
            AppService serviceSeed = new AppService();

            Cloud.ServerStatus status = await Cloud.ResultMultiple<AppService>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, serviceSeed, doCache);

            if (status.Code < 0)
            {
                return status;
            }

            _appServices = status.PayloadToList<AppService>();
            AppServiceTier tierSeed = new AppServiceTier();

            foreach (AppService service in _appServices)
            {
                tierSeed.Service = service;

                status = await Cloud.ResultMultiple<AppServiceTier>.WaitForObjectAsync(
                    NesterControl.Service, throwIfError, tierSeed, doCache);

                if (status.Code == 0)
                {
                    service.Tiers = status.PayloadToList<AppServiceTier>();
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryAppServiceTierLocationsAsync(AppServiceTier teir,
            bool doCache = true, bool throwIfError = true)
        {
            Forest forestSeeder = new Forest();
            forestSeeder.AppServiceTier = teir;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<Forest>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, forestSeeder, doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> CreateSubscription(AppServiceTier tier,
            bool doCache = true, bool throwIfError = true)
        {
            AppServiceSubscription subscription = new AppServiceSubscription();
            subscription.App = _editApp;
            subscription.ServiceTier = tier;
            subscription.AppServiceTierId = tier.Id;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppServiceSubscription>.WaitForObjectAsync(
                throwIfError, subscription, new Cloud.CachedHttpRequest<AppServiceSubscription>(
                    NesterControl.Service.CreateAsync), doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> RemoveSubscription(AppServiceSubscription subscription,
             bool doCache = false, bool throwIfError = true)
        {
            Cloud.ServerStatus status = await Cloud.ResultSingle<AppServiceSubscription>.WaitForObjectAsync(
                throwIfError, subscription, new Cloud.CachedHttpRequest<AppServiceSubscription>(
                    NesterControl.Service.RemoveAsync), doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryAppSubscriptions(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            if (!_appServices.Any())
            {
                await QueryServicesAsync();
            }

            AppServiceSubscription subSeeder = new AppServiceSubscription();
            subSeeder.App = (app == null ? _editApp : app);

            Cloud.ServerStatus status = await Cloud.ResultMultiple<AppServiceSubscription>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, subSeeder, doCache);

            if (status.Code >= 0)
            {
                ObservableCollection<AppServiceSubscription> serviceSubscriptions = 
                    status.PayloadToList<AppServiceSubscription>();

                foreach (AppServiceSubscription subscription in serviceSubscriptions)
                {
                    subscription.App = subSeeder.App;

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

                subSeeder.App.Subscriptions = serviceSubscriptions;
            }

            return status;
        }
        public string[] TranslateFeaturesAll(AppService service)
        {
            List<string> values = JsonConvert.DeserializeObject<List<string>>(
                service.FeaturesAll);
                
            ResourceManager resmgr = NesterControl.GetResourceManager();

            List<string> TranslatedValues = new List<string>();
            foreach (string value in values)
            {
                var translation = resmgr.GetString(value,
                    System.Globalization.CultureInfo.CurrentUICulture);
                TranslatedValues.Add(translation);
            }

            return TranslatedValues.ToArray<string>();
        }

        public string[] TranslateFeaturesIncluded(AppServiceTier tier)
        {
            List<string> values = JsonConvert.DeserializeObject<List<string>>(
                tier.FeaturesIncluded);
            return values.ToArray<string>();
        }
    }
}
