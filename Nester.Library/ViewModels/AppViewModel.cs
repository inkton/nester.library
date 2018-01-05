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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class AppViewModel : ViewModel
    {
        private bool _mariaDBEnabled = false;

        private ContactViewModel _contactViewModel;
        private NestViewModel _nestViewModel;
        private DomainViewModel _domainViewModel;
        private DeploymentViewModel _deploymentViewModel;
        private ServicesViewModel _servicesViewModel;
        private LogViewModel _logViewModel;

        private AppServiceTier _selectedAppServiceTier;
        private ObservableCollection<Notification> _notifications;

        public class AppType
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Image { get; set; }
            public string Tag { get; set; }
        }

        private AppType _editApplicationType;
        private ObservableCollection<AppType> _applicationTypes;

        public AppViewModel()
        {
            // when editing this will 
            // select uniflow default
            _editApp = new App();
            _editApp.Type = "uniflow";
            _editApp.Owner = NesterControl.User;

            _applicationTypes = new ObservableCollection<AppType> {
                new AppType {
                    Name ="Uniflow",
                    Description ="MVC Web server",
                    Image ="webnet32.png",
                    Tag = "uniflow"
                },
                new AppType {
                    Name ="Biflow",
                    Description ="API Web server",
                    Image ="websocketnet32.png",
                    Tag = "biflow"
                },
            };

            _contactViewModel = new ContactViewModel(_editApp);
            _nestViewModel = new NestViewModel(_editApp);
            _domainViewModel = new DomainViewModel(_editApp);
            _deploymentViewModel = new DeploymentViewModel(_editApp);
            _servicesViewModel = new ServicesViewModel(_editApp);
            _logViewModel = new LogViewModel(_editApp);
        }

        override public App EditApp
        {
            get
            {
                return _editApp;
            }
            set
            {
                SetProperty(ref _editApp, value);

                _contactViewModel.EditApp = value;
                _nestViewModel.EditApp = value;
                _domainViewModel.EditApp = value;
                _deploymentViewModel.EditApp = value;
                _servicesViewModel.EditApp = value;
                _logViewModel.EditApp = value;
            }
        }

        public bool IsAppOwner
        {
            get
            {
                bool isOwner = false;

                if (_editApp != null)
                {
                    isOwner = _editApp.UserId == NesterControl.User.Id;
                }

                return isOwner;
            }
        }

        public ContactViewModel ContactModel
        {
            get
            {
                return _contactViewModel;
            }
            set
            {
                SetProperty(ref _contactViewModel, value);
            }
        }

        public NestViewModel NestModel
        {
            get
            {
                return _nestViewModel;
            }
            set
            {
                SetProperty(ref _nestViewModel, value);
            }
        }

        public DomainViewModel DomainModel
        {
            get
            {
                return _domainViewModel;
            }
            set
            {
                SetProperty(ref _domainViewModel, value);
            }
        }

        public DeploymentViewModel DeploymentModel
        {
            get
            {
                return _deploymentViewModel;
            }
            set
            {
                SetProperty(ref _deploymentViewModel, value);
            }
        }

        public ServicesViewModel ServicesViewModel
        {
            get
            {
                return _servicesViewModel;
            }
            set
            {
                SetProperty(ref _servicesViewModel, value);
            }
        }

        public LogViewModel LogViewModel
        {
            get
            {
                return _logViewModel;
            }
            set
            {
                SetProperty(ref _logViewModel, value);
            }
        }

        public ObservableCollection<AppType> ApplicationTypes
        {
            get
            {
                return _applicationTypes;
            }
        }

        public AppType EditApplicationType
        {
            get
            {
                return _editApplicationType;
            }
            set
            {
                SetProperty(ref _editApplicationType, value);
            }
        }

        public ObservableCollection<Notification> Notifications
        {
            get
            {
                return _notifications;
            }
            set
            {
                SetProperty(ref _notifications, value);
            }
        }

        #region App Tier


        public ObservableCollection<AppServiceTier> AppServiceTiers
        {
            get
            {
                AppService service = _servicesViewModel.Services.FirstOrDefault(
                    x => x.Tag == "nest-oak");

                if (service != null)
                {
                    return service.Tiers;
                }

                return null;
            }
        }

        public AppServiceTier SelectedAppServiceTier
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "nest-oak");

                    if (subscription != null)
                    {
                        return subscription.ServiceTier;
                    }
                }
                else
                {
                    _selectedAppServiceTier = AppServiceTiers.First();
                }

                return _selectedAppServiceTier;
            }
            set
            {
                SetProperty(ref _selectedAppServiceTier, value);
            }
        }

        public string[] SelectedAppServiceFeatures
        {
            get
            {
                return _servicesViewModel.TranslateFeaturesAll(
                    SelectedAppServiceTier.Service);
            }
        }

        public string[] SelectedAppServiceTierIncludes
        {
            get
            {
                return _servicesViewModel.TranslateFeaturesIncluded(
                    SelectedAppServiceTier);
            }
        }

        public string PaymentNotice
        {
            get
            {
                if (NesterControl.User.TerritoryISOCode == "AU")
                {
                    return "The prices are in US Dollars and do not include GST.";
                }
                else
                {
                    return "The prices are in US Dollars. ";
                }
            }
        }

        #endregion  

        #region MariaDB Tier

        public bool MariaDBEnabled
        {
            get
            {
                return _mariaDBEnabled;
            }
            set
            {
                SetProperty(ref _mariaDBEnabled, value);
            }
        }

        public ObservableCollection<AppServiceTier> MariaDBTiers
        {
            get
            {
                AppService service = _servicesViewModel.Services.FirstOrDefault(
                    x => x.Tag == "mariadb");

                if (service != null)
                {
                    return service.Tiers;
                }

                return null;
            }
        }

        public AppServiceTier SelectedMariaDBTier
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "mariadb");

                    if (subscription != null)
                    {
                        return subscription.ServiceTier;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Letsencrypt Tier

        public AppServiceTier SelectedLetsencryptTier
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "letsencrypt");

                    if (subscription != null)
                    {
                        return subscription.ServiceTier;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Logging Tier

        public AppServiceTier SelectedLoggingTier
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "logging");

                    if (subscription != null)
                    {
                        return subscription.ServiceTier;
                    }
                }

                return null;
            }
        }

        #endregion

        #region RabbitMQ Tier

        public AppServiceTier SelectedRabbitMQTier
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "rabbitmq");

                    if (subscription != null)
                    {
                        return subscription.ServiceTier;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Git Tier

        public AppServiceTier SelectedGitServiceTier
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "git");

                    if (subscription != null)
                    {
                        return subscription.ServiceTier;
                    }
                }

                return null;
            }
        }

        #endregion

        override public async Task<Cloud.ServerStatus> InitAsync()
        {
            Cloud.ServerStatus status;

            status = await QueryAppAsync();
            if (status.Code < 0)
            {
                return status;
            }

            _mariaDBEnabled = SelectedMariaDBTier != null;

            await _deploymentViewModel.InitAsync();
            await _servicesViewModel.InitAsync();
            await _contactViewModel.InitAsync();
            await _nestViewModel.InitAsync();
            await _domainViewModel.InitAsync();

            OnPropertyChanged("EditApp");

            return status;
        }

        async public void NewAppAsync()
        {
            _editApp = new App();
            _editApp.Type = "uniflow";
            _editApp.Owner = NesterControl.User;

            _contactViewModel.EditApp = _editApp;
            _nestViewModel.EditApp = _editApp;
            _domainViewModel.EditApp = _editApp;
            _deploymentViewModel.EditApp = _editApp;
            _servicesViewModel.EditApp = _editApp;

            await ServicesViewModel.QueryServicesAsync();
        }

        async public void Reload()
        {
            await InitAsync();

            await ServicesViewModel.QueryServicesAsync();
        }

        public async Task<Cloud.ServerStatus> QueryAppNotificationsAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;
            Notification notificationSeed = new Notification();
            notificationSeed.App = theApp;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<Notification>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, notificationSeed, doCache);

            if (status.Code == 0)
            {
                _notifications = status.PayloadToList<Notification>();
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryAppAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            Cloud.ServerStatus status = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App>(
                    NesterControl.Service.QueryAsync), doCache);

            if (status.Code == 0)
            {
                EditApp = status.PayloadToObject<App>();

                if (_editApp.UserId == NesterControl.User.Id)
                {
                    _editApp.Owner = NesterControl.User;
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RemoveAppAsync(App app = null,
             bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            Cloud.ServerStatus status = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App>(
                    NesterControl.Service.RemoveAsync), doCache);

            if (status.Code == 0)
            {
                EditApp = theApp;
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateAppAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            Cloud.ServerStatus status = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App>(
                    NesterControl.Service.UpdateAsync), doCache);

            if (status.Code == 0)
            {
                EditApp = status.PayloadToObject<App>();
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> CreateAppAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;
            theApp.ServiceTierId = _selectedAppServiceTier.Id;

            Cloud.ServerStatus status = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App>(
                    NesterControl.Service.CreateAsync), doCache);

            if (status.Code == 0)
            {
                EditApp = status.PayloadToObject<App>();
                _editApp.Owner = NesterControl.User;

                if (throwIfError && _editApp.Status != "assigned")
                {
                    string message = "Failed to initialize the app. Please contact support.";
                    Helpers.ErrorHandler.Exception(message, string.Empty);
                    throw new Exception(message);
                }

                await InitAsync();
            }
            return status;
        }

        public async Task<Cloud.ServerStatus> QueryAppServiceTierLocationsAsync(AppServiceTier teir,
            bool doCache = false, bool throwIfError = true)
        {
            Forest forestSeeder = new Forest();
            forestSeeder.AppServiceTier = teir;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<Forest>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, forestSeeder, doCache);

            return status;
        }
    }
}
