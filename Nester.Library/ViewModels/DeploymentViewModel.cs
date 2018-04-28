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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class DeploymentViewModel : ViewModel
    {
        private Deployment _editDeployment;
        private AppBackup _editBackup;
        private AppAudit _editAudit;

        private ObservableCollection<Deployment> _deployments;
        private ObservableCollection<AppServiceTier> _upgradableAppTiers;
        private ObservableCollection<Forest> _forests;
        private Dictionary<string, Forest> _forestByTag;
        private ObservableCollection<SoftwareFramework.Version> _dotnetVersions;
        private ObservableCollection<AppBackup> _backups;
        private ObservableCollection<AppAudit> _audits;
        private AppServiceTier _upgradeAppServiceTier;

        public bool _mariaDBEnabled = false;

        public ICommand SelectForestCommand { get; private set; }

        public DeploymentViewModel(App app) : base(app)
        {
            _deployments = new ObservableCollection<Deployment>();
            _upgradableAppTiers = new ObservableCollection<AppServiceTier>();
            _forests = new ObservableCollection<Forest>();
            _forestByTag = new Dictionary<string, Forest>();

            _editDeployment = new Deployment();
            _editDeployment.App = app;
            app.Deployment = _editDeployment;

            _editBackup = new AppBackup();
            _editBackup.Deployment = _editDeployment;
            _backups = new ObservableCollection<AppBackup>();

            _editAudit = new AppAudit();
            _editAudit.Deployment = _editDeployment;
            _audits = new ObservableCollection<AppAudit>();

            SelectForestCommand = new Command<Forest>(
                (forest) => HandleCommand<Forest>(forest, "select"));
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
                _editApp.Deployment = _editDeployment;
                _editDeployment.App = value;
            }
        }

        public Deployment EditDeployment
        {
            get
            {
                return _editDeployment;
            }
            set
            {
                SetProperty(ref _editDeployment, value);
                _editApp.Deployment = value;
            }
        }

        public Dictionary<string, Forest> ForestsByTag
        {
            get
            {
                return _forestByTag;
            }
            set
            {
                SetProperty(ref _forestByTag, value);
            }
        }

        public ObservableCollection<SoftwareFramework.Version> DotnetVersions
        {
            get
            {
                return _dotnetVersions;
            }
            set
            {
                SetProperty(ref _dotnetVersions, value);
            }
        }

        public ObservableCollection<Deployment> Deployments
        {
            get
            {
                return _deployments;
            }
            set
            {
                SetProperty(ref _deployments, value);
            }
        }

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

        public ServicesViewModel.ServiceTableItem SelectedAppService
        {
            get
            {
                if (_upgradeAppServiceTier != null)
                {
                    return ServicesViewModel.CreateServiceItem(
                        _upgradeAppServiceTier);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                _upgradeAppServiceTier = value.Tier;
            }
        }

        public ObservableCollection<AppBackup> AppBackups
        {
            get
            {
                return _backups;
            }
            set
            {
                SetProperty(ref _backups, value);
            }
        }

        public ObservableCollection<AppAudit> AppAudits
        {
            get
            {
                return _audits;
            }
            set
            {
                SetProperty(ref _audits, value);
            }
        }

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

        public ServicesViewModel.ServiceTableItem SelectedMariaDBService
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "mariadb");

                    if (subscription != null)
                    {
                        return ServicesViewModel.CreateServiceItem(
                            subscription.ServiceTier);
                    }
                }

                return null;
            }
        }

        public ServicesViewModel.ServiceTableItem SelectedGitService
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "git");

                    if (subscription != null)
                    {
                        return ServicesViewModel.CreateServiceItem(
                            subscription.ServiceTier);
                    }
                }

                return null;
            }
        }

        public ServicesViewModel.ServiceTableItem SelectedLetsencryptService
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "letsencrypt");

                    if (subscription != null)
                    {
                        return ServicesViewModel.CreateServiceItem(
                            subscription.ServiceTier);
                    }
                }

                return null;
            }
        }

        public ServicesViewModel.ServiceTableItem SelectedLoggingService
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "logging");

                    if (subscription != null)
                    {
                        return ServicesViewModel.CreateServiceItem(
                            subscription.ServiceTier);
                    }
                }

                return null;
            }
        }

        public ServicesViewModel.ServiceTableItem SelectedRabbitMQService
        {
            get
            {
                if (_editApp.Subscriptions != null)
                {
                    AppServiceSubscription subscription = _editApp.Subscriptions.FirstOrDefault(
                        x => x.ServiceTier.Service.Tag == "rabbitmq");

                    if (subscription != null)
                    {
                        return ServicesViewModel.CreateServiceItem(
                            subscription.ServiceTier);
                    }
                }

                return null;
            }
        }

        override public async Task<Cloud.ServerStatus> InitAsync()
        {
            Cloud.ServerStatus status;

            status = await QueryDeploymentsAsync();
            if (status.Code < 0)
            {
                return status;
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> CollectInfoAsync()
        {
            Cloud.ServerStatus status;

            status = await QueryForestsAsync();
            if (status.Code < 0)
            {
                return status;
            }

            status = await QuerySoftwareFrameworkVersionsAsync();
            if (status.Code < 0)
            {
                return status;
            }

            return status;
        }

        public ObservableCollection<ServicesViewModel.ServiceTableItem> AppFeaturesTable
        {
            get
            {
                return ServicesViewModel.CreateServicesTable(
                    _upgradableAppTiers);
            }
        }

        public async Task<Cloud.ServerStatus> QueryForestsAsync(
            bool doCache = true, bool throwIfError = true)
        {
            Forest forestSeed = new Forest();

            Cloud.ServerStatus status = await Cloud.ResultMultiple<Forest>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, forestSeed, doCache);

            if (status.Code >= 0)
            {
                _forests = status.PayloadToList<Forest>();
                _forestByTag.Clear();

                foreach (Forest forest in _forests)
                {
                    _forestByTag[forest.Tag] = forest;
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QuerySoftwareFrameworkVersionsAsync(
            bool doCache = true, bool throwIfError = true)
        {
            SoftwareFramework frameworkSeed = new SoftwareFramework();
            frameworkSeed.Tag = "aspdotnetcore";
            SoftwareFramework.Version versionSeed = new SoftwareFramework.Version();
            versionSeed.Framework = frameworkSeed;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<SoftwareFramework.Version>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, versionSeed, doCache);

            if (status.Code >= 0)
            {
                _dotnetVersions = status.PayloadToList<SoftwareFramework.Version>();
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryDeploymentsAsync(
            Deployment deployment = null, bool doCache = false, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<Deployment>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, theDeployment, doCache);
            _editApp.Deployment = null;

            if (status.Code >= 0)
            {
                _deployments = status.PayloadToList<Deployment>();
                if (_deployments.Any())
                {
                    _editDeployment = _deployments.First();
                    _editApp.Deployment = _editDeployment;
                }

                OnPropertyChanged("Deployments");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryDevkitAsync(Devkit devkit,
            bool doCache = false, bool throwIfError = true)
        {
            Cloud.ServerStatus status = await Cloud.ResultSingle<Devkit>.WaitForObjectAsync(
                throwIfError, devkit, new Cloud.CachedHttpRequest<Devkit>(
                    NesterControl.Service.QueryAsync), doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> CreateDeployment(Deployment deployment = null,
            bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Deployment>.WaitForObjectAsync(
                throwIfError, theDeployment, new Cloud.CachedHttpRequest<Deployment>(
                    NesterControl.Service.CreateAsync), doCache);

            if (status.Code >= 0)
            {
                _editDeployment = status.PayloadToObject<Deployment>();
                _editApp.Deployment = _editDeployment;

                if (deployment != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editDeployment, deployment);
                    _deployments.Add(deployment);
                    OnPropertyChanged("Deployments");
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateDeploymentAsync(Deployment deployment = null,
             bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Deployment>.WaitForObjectAsync(
                throwIfError, theDeployment, new Cloud.CachedHttpRequest<Deployment>(
                    NesterControl.Service.UpdateAsync), doCache);

            if (status.Code >= 0)
            {
                _editDeployment = status.PayloadToObject<Deployment>();
                _editApp.Deployment = _editDeployment;
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RemoveDeploymentAsync(Deployment deployment = null,
            bool doCache = false, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Deployment>.WaitForObjectAsync(
                throwIfError, theDeployment, new Cloud.CachedHttpRequest<Deployment>(
                    NesterControl.Service.RemoveAsync), doCache);

            if (status.Code == 0)
            {
                if (deployment == null)
                {
                    _deployments.Remove(deployment);
                    OnPropertyChanged("Deployments");
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryAppUpgradeServiceTiersAsync(
            AppService service, Deployment deployment = null, bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;
            service.Deployment = theDeployment;

            AppServiceTier tierSeed = new AppServiceTier();
            tierSeed.Service = service;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<AppServiceTier>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, tierSeed, doCache);

            if (status.Code >= 0)
            {
                _upgradableAppTiers = status.PayloadToList<AppServiceTier>();
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateAppUpgradeServiceTiersAsync(
            AppService service = null, AppServiceTier tierSeed = null, Deployment deployment = null, 
            bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;
            service.Deployment = theDeployment;
            _upgradeAppServiceTier.Service = service;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppServiceTier>.WaitForObjectAsync(
                throwIfError, _upgradeAppServiceTier, new Cloud.CachedHttpRequest<AppServiceTier>(
                    NesterControl.Service.UpdateAsync), doCache);

            if (status.Code >= 0)
            {
                _upgradableAppTiers = status.PayloadToList<AppServiceTier>();
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryAppBackupsAsync(AppBackup appBackup = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppBackup theBackup = appBackup == null ? _editBackup : appBackup;
            theBackup.Deployment = _editApp.Deployment;
            
            Cloud.ServerStatus status = await Cloud.ResultMultiple<AppBackup>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, theBackup, doCache);

            if (status.Code >= 0)
            {
                _backups = status.PayloadToList<AppBackup>();
                OnPropertyChanged("AppBackups");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RestoreAppAsync(AppBackup appBackup,
            bool doCache = false, bool throwIfError = true)
        {
            AppBackup theBackup = appBackup == null ? _editBackup : appBackup;
            theBackup.Deployment = _editApp.Deployment;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppBackup>.WaitForObjectAsync(
                throwIfError, appBackup, new Cloud.CachedHttpRequest<AppBackup>(
                    NesterControl.Service.UpdateAsync), doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> BackupAppAsync(AppBackup appBackup = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppBackup theBackup = appBackup == null ? _editBackup : appBackup;
            theBackup.Deployment = _editApp.Deployment;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppBackup>.WaitForObjectAsync(
                throwIfError, theBackup, new Cloud.CachedHttpRequest<AppBackup>(
                    NesterControl.Service.CreateAsync), doCache);

            if (status.Code >= 0)
            {
                _editBackup = status.PayloadToObject<AppBackup>();

                if (appBackup != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editBackup, appBackup);
                }

                foreach (AppBackup backup in _backups)
                {
                    if (backup.Id == _editBackup.Id)
                    {
                        Cloud.Object.CopyPropertiesTo(_editBackup, backup);
                        break;
                    }
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryAppAuditsAsync(IDictionary<string, string> filter,
            AppAudit appAudit = null, bool doCache = false, bool throwIfError = true)
        {
            AppAudit theAudit = appAudit == null ? _editAudit : appAudit;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<AppAudit>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, theAudit, doCache, filter);

            if (status.Code >= 0)
            {
                _audits = status.PayloadToList<AppAudit>();
                OnPropertyChanged("AppAudits");
            }

            return status;
        }
    }
}
