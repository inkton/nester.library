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
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class DeploymentViewModel : ViewModel
    {
        private Deployment _editDeployment;
        private AppBackup _editBackup;
        private AppAudit _editAudit;
        private Credit _applyCredit;

        private ObservableCollection<Deployment> _deployments;
        private ObservableCollection<Forest> _forests;
        private Dictionary<string, Forest> _forestByTag;
        private ObservableCollection<SoftwareFramework.Version> _dotnetVersions;
        private ObservableCollection<AppBackup> _backups;
        private ObservableCollection<AppAudit> _audits;

        public ICommand SelectForestCommand { get; private set; }

        public DeploymentViewModel(NesterService platform, App app) : base(platform, app)
        {
            _deployments = new ObservableCollection<Deployment>();
            _forests = new ObservableCollection<Forest>();
            _forestByTag = new Dictionary<string, Forest>();

            _editDeployment = new Deployment();
            _editDeployment.OwnedBy = app;
            app.Deployment = _editDeployment;

            _editBackup = new AppBackup();
            _editBackup.OwnedBy = _editDeployment;
            _backups = new ObservableCollection<AppBackup>();

            _editAudit = new AppAudit();
            _editAudit.OwnedBy = _editDeployment;
            _audits = new ObservableCollection<AppAudit>();
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
                _editDeployment.OwnedBy = value;
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

        public Credit ApplyCredit
        {
            get
            {
                return _applyCredit;
            }
            set
            {
                _applyCredit = value;
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
        
        public async Task InitAsync()
        {
            await QueryDeploymentsAsync();
        }

        public async Task CollectInfoAsync()
        {
            await QueryForestsAsync();
            await QuerySoftwareFrameworkVersionsAsync();
        }

        public async Task<ResultMultiple<Forest>> QueryForestsAsync(
            bool doCache = true, bool throwIfError = true)
        {
            Forest forestSeed = new Forest();

            ResultMultiple<Forest> result = await ResultMultipleUI<Forest>.WaitForObjectAsync(
                Platform, throwIfError, forestSeed, doCache);

            if (result.Code >= 0)
            {
                _forests = result.Data.Payload;
                _forestByTag.Clear();

                foreach (Forest forest in _forests)
                {
                    _forestByTag[forest.Tag] = forest;
                }
            }

            return result;
        }

        public async Task<ResultMultiple<SoftwareFramework.Version>> QuerySoftwareFrameworkVersionsAsync(
            bool doCache = true, bool throwIfError = true)
        {
            SoftwareFramework frameworkSeed = new SoftwareFramework();
            frameworkSeed.Tag = "aspdotnetcore";
            SoftwareFramework.Version versionSeed = new SoftwareFramework.Version();
            versionSeed.OwnedBy = frameworkSeed;

            ResultMultiple<SoftwareFramework.Version> result = await ResultMultipleUI<SoftwareFramework.Version>.WaitForObjectAsync(
                Platform, throwIfError, versionSeed, doCache);

            if (result.Code >= 0)
            {
                _dotnetVersions = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultMultiple<Deployment>> QueryDeploymentsAsync(
            Deployment deployment = null, bool doCache = false, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            ResultMultiple<Deployment> result = await ResultMultipleUI<Deployment>.WaitForObjectAsync(
                Platform, throwIfError, theDeployment, doCache);
            _editApp.Deployment = null;

            if (result.Code >= 0)
            {
                _deployments = result.Data.Payload;
                if (_deployments.Any())
                {
                    _editDeployment = _deployments.First();
                    _editApp.Deployment = _editDeployment;
                }

                OnPropertyChanged("Deployments");
            }

            return result;
        }

        public async Task<ResultSingle<Devkit>> QueryDevkitAsync(Devkit devkit,
            bool doCache = false, bool throwIfError = true)
        {
            ResultSingle<Devkit> result = await ResultSingleUI<Devkit>.WaitForObjectAsync(
                throwIfError, devkit, new Cloud.CachedHttpRequest<Devkit, ResultSingle<Devkit>>(
                    Platform.QueryAsync), doCache);

            if (result.Code >= 0)
            {
                result.Data.Payload.CopyTo(devkit);
            }

            return result;
        }

        public async Task<ResultSingle<Deployment>> CreateDeployment(Deployment deployment = null,
            bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            Dictionary<string, string> data = new Dictionary<string, string>();

            if (_applyCredit != null)
            {
                data.Add("credit_id", _applyCredit.Id.ToString());
            }

            ResultSingle<Deployment> result = await ResultSingleUI<Deployment>.WaitForObjectAsync(
                throwIfError, theDeployment, new Cloud.CachedHttpRequest<Deployment, ResultSingle<Deployment>>(
                    Platform.CreateAsync), doCache, data);

            if (result.Code >= 0)
            {
                _editDeployment = result.Data.Payload;
                _editApp.Deployment = _editDeployment;

                if (deployment != null)
                {
                    _editDeployment.CopyTo(deployment);
                    _deployments.Add(deployment);
                    OnPropertyChanged("Deployments");
                }
            }

            return result;
        }

        public async Task<ResultSingle<Deployment>> UpdateDeploymentAsync(string activity,
            Deployment deployment = null, bool doCache = true, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("activity", activity);

            ResultSingle<Deployment> result = await ResultSingleUI<Deployment>.WaitForObjectAsync(
                throwIfError, theDeployment, new Cloud.CachedHttpRequest<Deployment, ResultSingle<Deployment>>(
                    Platform.UpdateAsync), doCache, data);

            if (result.Code >= 0)
            {
                _editDeployment = result.Data.Payload;
                _editApp.Deployment = _editDeployment;
            }

            return result;
        }

        public async Task<ResultSingle<Deployment>> RemoveDeploymentAsync(Deployment deployment = null,
            bool doCache = false, bool throwIfError = true)
        {
            Deployment theDeployment = deployment == null ? _editApp.Deployment : deployment;

            ResultSingle<Deployment> result = await ResultSingleUI<Deployment>.WaitForObjectAsync(
                throwIfError, theDeployment, new Cloud.CachedHttpRequest<Deployment, ResultSingle<Deployment>>(
                    Platform.RemoveAsync), doCache);

            if (result.Code == 0)
            {
                if (deployment == null)
                {
                    _deployments.Remove(deployment);
                    OnPropertyChanged("Deployments");
                }
            }

            return result;
        }

        public async Task<ResultMultiple<AppBackup>> QueryAppBackupsAsync(AppBackup appBackup = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppBackup theBackup = appBackup == null ? _editBackup : appBackup;
            theBackup.OwnedBy = _editApp.Deployment;

            ResultMultiple<AppBackup> result = await ResultMultipleUI<AppBackup>.WaitForObjectAsync(
                Platform, throwIfError, theBackup, doCache);

            if (result.Code >= 0)
            {
                _backups = result.Data.Payload;
                OnPropertyChanged("AppBackups");
            }

            return result;
        }

        public async Task<ResultSingle<AppBackup>> RestoreAppAsync(AppBackup appBackup,
            bool doCache = false, bool throwIfError = true)
        {
            AppBackup theBackup = appBackup == null ? _editBackup : appBackup;
            theBackup.OwnedBy = _editApp.Deployment;

            return await ResultSingleUI<AppBackup>.WaitForObjectAsync(
                throwIfError, appBackup, new Cloud.CachedHttpRequest<AppBackup, ResultSingle<AppBackup>>(
                    Platform.UpdateAsync), doCache);
        }

        public async Task<ResultSingle<AppBackup>> BackupAppAsync(AppBackup appBackup = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppBackup theBackup = appBackup == null ? _editBackup : appBackup;
            theBackup.OwnedBy = _editApp.Deployment;

            ResultSingle<AppBackup> result = await ResultSingleUI<AppBackup>.WaitForObjectAsync(
                throwIfError, theBackup, new Cloud.CachedHttpRequest<AppBackup, ResultSingle<AppBackup>>(
                    Platform.CreateAsync), doCache);

            if (result.Code >= 0)
            {
                _editBackup = result.Data.Payload;

                if (appBackup != null)
                {
                    _editBackup.CopyTo(appBackup);
                }

                foreach (AppBackup backup in _backups)
                {
                    if (backup.Id == _editBackup.Id)
                    {
                        _editBackup.CopyTo(appBackup);
                        break;
                    }
                }
            }

            return result;
        }

        public async Task<ResultMultiple<AppAudit>> QueryAppAuditsAsync(IDictionary<string, string> filter,
            AppAudit appAudit = null, bool doCache = false, bool throwIfError = true)
        {
            AppAudit theAudit = appAudit == null ? _editAudit : appAudit;

            ResultMultiple<AppAudit> result = await ResultMultipleUI<AppAudit>.WaitForObjectAsync(
                Platform, throwIfError, theAudit, doCache, filter);

            if (result.Code >= 0)
            {
                _audits = result.Data.Payload;
                OnPropertyChanged("AppAudits");
            }

            return result;
        }
    }
}
