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
using Xamarin.Forms;
using Inkton.Nest.Model;

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

        public AppViewModel()
        {
            // when editing this will 
            // select uniflow default
            _editApp = new App();
            _editApp.Type = "uniflow";
            _editApp.OwnedBy = Keeper.User;

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

            MessagingCenter.Send(this, "Updated", EditApp);
        }

        async public void NewAppAsync()
        {
            _editApp = new App();
            _editApp.Type = "uniflow";
            _editApp.OwnedBy = Keeper.User;

            _contactViewModel.EditApp = _editApp;
            _nestViewModel.EditApp = _editApp;
            _domainViewModel.EditApp = _editApp;
            _deploymentViewModel.EditApp = _editApp;
            _servicesViewModel.EditApp = _editApp;

            await ServicesViewModel.QueryServicesAsync();
        }

        async public Task<Cloud.ResultSingle<App>> QueryStatusAsync()
        {
            Cloud.ResultSingle<App> result = await QueryAppAsync();
            if (result.Code == 0)
            {
                await DeploymentViewModel.InitAsync();
            }

            OnPropertyChanged("EditApp");

            return result;
        }

        async public void Reload()
        {
            await InitAsync();

            await ServicesViewModel.QueryServicesAsync();
        }

        public async Task<Cloud.ResultMultiple<Notification>> QueryAppNotificationsAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;
            Notification notificationSeed = new Notification();
            notificationSeed.OwnedBy = theApp;

            Cloud.ResultMultiple<Notification> result = await Cloud.ResultMultiple<Notification>.WaitForObjectAsync(
                Keeper.Service, throwIfError, notificationSeed, doCache);

            if (result.Code == 0)
            {
                _notifications = result.Data.Payload;
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<App>> QueryAppAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            Cloud.ResultSingle<App> result = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, Cloud.ResultSingle<App>>(
                    Keeper.Service.QueryAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = result.Data.Payload;

                if (_editApp.UserId == Keeper.User.Id)
                {
                    _editApp.OwnedBy = Keeper.User;
                }

                if (app != null)
                    _editApp.CopyTo(app);
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<App>> RemoveAppAsync(App app = null,
             bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            Cloud.ResultSingle<App> result = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, Cloud.ResultSingle<App>>(
                    Keeper.Service.RemoveAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = theApp;
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<App>> UpdateAppAsync(App app = null,
            bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;

            Cloud.ResultSingle<App> result = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, Cloud.ResultSingle<App>>(
                    Keeper.Service.UpdateAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = result.Data.Payload;
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<App>> CreateAppAsync(AppServiceTier tier,
            App app = null, bool doCache = false, bool throwIfError = true)
        {
            App theApp = app == null ? _editApp : app;
            theApp.ServiceTierId = tier.Id;

            Cloud.ResultSingle<App> result = await Cloud.ResultSingle<App>.WaitForObjectAsync(
                throwIfError, theApp, new Cloud.CachedHttpRequest<App, Cloud.ResultSingle<App>>(
                    Keeper.Service.CreateAsync), doCache);

            if (result.Code == 0)
            {
                EditApp = result.Data.Payload;
                _editApp.OwnedBy = Keeper.User;

                if (throwIfError && _editApp.Status != "assigned")
                {
                    string message = "Failed to initialize the app. Please contact support.";
                    Helpers.ErrorHandler.Exception(message, string.Empty);
                    throw new Exception(message);
                }

                await InitAsync();

                if (app != null)
                    _editApp.CopyTo(app);
            }
            return result;
        }

        public async Task<Cloud.ResultMultiple<Forest>> QueryAppServiceTierLocationsAsync(AppServiceTier teir,
            bool doCache = false, bool throwIfError = true)
        {
            Forest forestSeeder = new Forest();
            forestSeeder.OwnedBy = teir;

            return await Cloud.ResultMultiple<Forest>.WaitForObjectAsync(
                Keeper.Service, throwIfError, forestSeeder, doCache);
        }
    }
}
