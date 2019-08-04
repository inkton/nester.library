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
using System.Threading.Tasks;
using Xamarin.Forms;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class AppViewModel<UserT> : ViewModel<UserT>
        where UserT : User, new()
    {
        private ContactViewModel<UserT> _contactViewModel;
        private NestViewModel<UserT> _nestViewModel;
        private DomainViewModel<UserT> _domainViewModel;
        private DeploymentViewModel<UserT> _deploymentViewModel;
        private ServicesViewModel<UserT> _servicesViewModel;
        private LogViewModel<UserT> _logViewModel;

        private ObservableCollection<Notification> _notifications;

        public class AppType
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Image { get; set; }
            public string Tag { get; set; }
        }

        public AppViewModel(BackendService<UserT> backend)
            :base(backend)
        {
            // when editing this will 
            // select uniflow default
            _editApp = new App();
            _editApp.Type = "uniflow";
            _editApp.OwnedBy = _backend.Permit.User;

            ApplicationTypes = new ObservableCollection<AppType> {
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

            _contactViewModel = new ContactViewModel<UserT>(_backend, _editApp);
            _nestViewModel = new NestViewModel<UserT>(_backend, _editApp);
            _domainViewModel = new DomainViewModel<UserT>(_backend, _editApp);
            _deploymentViewModel = new DeploymentViewModel<UserT>(_backend, _editApp);
            _servicesViewModel = new ServicesViewModel<UserT>(_backend, _editApp);

            BackendService<UserT> appBackend = new BackendService<UserT>();
            appBackend.Version = backend.Version;
            appBackend.DeviceSignature = backend.DeviceSignature;

            _logViewModel = new LogViewModel<UserT>(appBackend, _editApp);
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

        public ContactViewModel<UserT> ContactViewModel
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

        public NestViewModel<UserT> NestViewModel
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

        public DomainViewModel<UserT> DomainViewModel
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

        public DeploymentViewModel<UserT> DeploymentViewModel
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

        public ServicesViewModel<UserT> ServicesViewModel
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

        public LogViewModel<UserT> LogViewModel
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

        public ObservableCollection<AppType> ApplicationTypes { get; }

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
        }

        public async Task<ResultMultiple<Notification>> QueryAppNotificationsAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;
            Notification notificationSeed = new Notification();
            notificationSeed.OwnedBy = theApp;

            ResultMultiple<Notification> result = await ResultMultipleUI<Notification>.WaitForObjectsAsync(
                true, notificationSeed, new CachedHttpRequest<Notification, ResultMultiple<Notification>>(
                    Backend.QueryAsyncListAsync), true);

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
                throwIfError, theApp, new CachedHttpRequest<App, ResultSingle<App>>(
                    Backend.QueryAsync), doCache);

            if (result.Code == 0)   
            {
                EditApp = result.Data.Payload;

                if (_editApp.UserId == Backend.Permit.User.Id)
                {
                    _editApp.OwnedBy = Backend.Permit.User;
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
                throwIfError, theApp, new CachedHttpRequest<App, ResultSingle<App>>(
                    Backend.RemoveAsync), doCache);

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
                throwIfError, theApp, new CachedHttpRequest<App, ResultSingle<App>>(
                    Backend.UpdateAsync), doCache);

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
                throwIfError, theApp, new CachedHttpRequest<App, ResultSingle<App>>(
                    Backend.CreateAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = result.Data.Payload;
                _editApp.OwnedBy = Backend.Permit.User;

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

            return await ResultMultipleUI<Forest>.WaitForObjectsAsync(
                throwIfError, forestSeeder, new CachedHttpRequest<Forest, ResultMultiple<Forest>>(
                    Backend.QueryAsyncListAsync), doCache);
        }
    }
}
