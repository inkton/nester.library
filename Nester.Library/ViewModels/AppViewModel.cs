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
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;
using Inkton.Nester.Storage;

namespace Inkton.Nester.ViewModels
{
    public class AppViewModel : ViewModel
    {
        private ContactViewModel _contactViewModel;
        private NestViewModel _nestViewModel;
        private DomainViewModel _domainViewModel;
        private DeploymentViewModel _deploymentViewModel;
        private ServicesViewModel _servicesViewModel;
        private LogViewModel _logViewModel;

        private ObservableCollection<Notification> _notifications;

        public class AppType
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Image { get; set; }
            public string Tag { get; set; }
        }

        private ObservableCollection<AppType> _applicationTypes;

        public AppViewModel(NesterService platform)
            :base(platform)
        {
            // when editing this will 
            // select uniflow default
            _editApp = new App();
            _editApp.Type = "uniflow";
            _editApp.OwnedBy = platform.Permit.Owner;

            _applicationTypes = new ObservableCollection<AppType> {
                new AppType {
                    Name ="Uniflow",
                    Description ="A Web Server",
                    Image ="webnet32.png",
                    Tag = "uniflow"
                },
                new AppType {
                    Name ="Biflow",
                    Description ="A Websocket Server",
                    Image ="websocketnet32.png",
                    Tag = "biflow"
                }
            };

            _contactViewModel = new ContactViewModel(platform, _editApp);
            _nestViewModel = new NestViewModel(platform, _editApp);
            _domainViewModel = new DomainViewModel(platform, _editApp);
            _deploymentViewModel = new DeploymentViewModel(platform, _editApp);
            _servicesViewModel = new ServicesViewModel(platform, _editApp);

            StorageService cache = new StorageService(Path.Combine(
                    Path.GetTempPath(), "NesterAppCache_" + _editApp.Tag));
            cache.Clear();

            NesterService backend = new NesterService(
                platform.Version, platform.DeviceSignature, cache);

            _logViewModel = new LogViewModel(backend, _editApp);
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

        public bool IsInteractive
        {
            get
            {
                // The app operations on the app are busy (IsBusy == true) or
                // The app itself is busy
                return !(IsBusy || EditApp.IsBusy);
            }
        }

        public bool IsDeployed
        {
            get
            {
                // The UI is functioning and
                // the app has been deployed
                return (IsInteractive && EditApp.IsDeployed);
            }
        }

        public bool IsActive
        {
            get
            {
                // The UI is functioning and
                // the app has been deployed and is active
                return (IsInteractive && EditApp.IsActive);
            }
        }

        public ContactViewModel ContactViewModel
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

        public NestViewModel NestViewModel
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

        public DomainViewModel DomainViewModel
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

        public DeploymentViewModel DeploymentViewModel
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

        public async Task InitAsync()
        {
            System.Diagnostics.Debug.WriteLine(
                string.Format("Begin - Init App - {0}", EditApp.Tag));

            await QueryAppAsync();

            await _deploymentViewModel.InitAsync();
            await _servicesViewModel.InitAsync();
            await _contactViewModel.InitAsync();
            await _nestViewModel.InitAsync();
            await _domainViewModel.InitAsync();

            OnPropertyChanged("EditApp");

            System.Diagnostics.Debug.WriteLine(
                string.Format("End - Init App - {0}", EditApp.Tag));

            MessagingCenter.Send(EditApp, "Updated");
        }

        async public Task<ResultSingle<App>> QueryStatusAsync()
        {
            ResultSingle<App> result = await QueryAppAsync();
            if (result.Code == 0)
            {
                await DeploymentViewModel.InitAsync();
            }

            OnPropertyChanged("EditApp");

            return result;
        }

        async public Task ReloadAsync()
        {
            await InitAsync();

            await ServicesViewModel.QueryServicesAsync();
        }

        public async Task<ResultMultiple<Notification>> QueryAppNotificationsAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;
            Notification notificationSeed = new Notification();
            notificationSeed.OwnedBy = theApp;

            ResultMultiple<Notification> result = await ResultMultipleUI<Notification>.WaitForObjectAsync(
                Platform, throwIfError, notificationSeed, doCache);

            if (result.Code == 0)
            {
                _notifications = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultSingle<App>> QueryAppAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            ResultSingle<App> result = await ResultSingleUI<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, ResultSingle<App>>(
                    Platform.QueryAsync), doCache);

            if (result.Code == 0)   
            {
                EditApp = result.Data.Payload;

                if (_editApp.UserId == Platform.Permit.Owner.Id)
                {
                    _editApp.OwnedBy = Platform.Permit.Owner;
                }

                if (app != null)
                    _editApp.CopyTo(app);
            }

            return result;
        }

        public async Task<ResultSingle<App>> RemoveAppAsync(App app = null,
             bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            ResultSingle<App> result = await ResultSingleUI<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, ResultSingle<App>>(
                    Platform.RemoveAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = theApp;
            }

            return result;
        }

        public async Task<ResultSingle<App>> UpdateAppAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            ResultSingle<App> result = await ResultSingleUI<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, ResultSingle<App>>(
                    Platform.UpdateAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultSingle<App>> CreateAppAsync(AppServiceTier tier,
            App app = null, bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;
            theApp.ServiceTierId = tier.Id;

            ResultSingle<App> result = await ResultSingleUI<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, ResultSingle<App>>(
                    Platform.CreateAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = result.Data.Payload;
                _editApp.OwnedBy = Platform.Permit.Owner;

                if (throwIfError && _editApp.Status != "assigned")
                {
                    string message = "Failed to initialize the app. Please contact support.";
                    //Helpers.ErrorHandler.Exception(message, string.Empty);
                    throw new Exception(message);
                }

                if (app != null)
                    _editApp.CopyTo(app);
            }
            return result;
        }

        public async Task<ResultMultiple<Forest>> QueryAppServiceTierLocationsAsync(AppServiceTier teir,
            bool doCache = false, bool throwIfError = true)
        {
            Forest forestSeeder = new Forest();
            forestSeeder.OwnedBy = teir;

            return await ResultMultipleUI<Forest>.WaitForObjectAsync(
                Platform, throwIfError, forestSeeder, doCache);
        }
    }
}
