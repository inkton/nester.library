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
using System.Windows.Input;
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class DomainViewModel : ViewModel
    {
        private AppDomain _editDomain;
        private bool _primary;

        private ObservableCollection<AppDomain> _domains;

        public ICommand EditCertCommand { get; private set; }

        public DomainViewModel(App app) : base(app)
        {
            _domains = new ObservableCollection<AppDomain>();
            _primary = false;

            _editDomain = new AppDomain();
            _editDomain.App = app;
        }

        override public App EditApp
        {
            get
            {
                return _editApp;
            }
            set
            {
                _editDomain.App = value;
                SetProperty(ref _editApp, value);
            }
        }

        public bool Primary
        {
            get { return _primary; }
            set
            {
                SetProperty(ref _primary, value);
            }
        }

        public AppDomain EditDomain
        {
            get
            {
                return _editDomain;
            }
            set
            {
                SetProperty(ref _editDomain, value);
            }
        }

        public ObservableCollection<AppDomain> Domains
        {
            get
            {
                return _domains;
            }
            set
            {
                SetProperty(ref _domains, value);
            }
        }

        public AppDomain DefaultDomain
        {
            get
            {
                var existDomains = from domain in _domains
                                   where domain.Default == true
                                   select domain;
                AppDomain defaultDomain = existDomains.FirstOrDefault();
                return defaultDomain;
            }
        }

        override public async Task<Cloud.ServerStatus> InitAsync()
        {
            return await QueryDomainsAsync();
        }

        public async Task<Cloud.ServerStatus> QueryDomainsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            Cloud.ServerStatus status = await Cloud.ResultMultiple<AppDomain>.WaitForObjectAsync(
                NesterControl.Service, throwIfError, _editDomain, doCache);

            if (status.Code >= 0)
            {
                _domains = status.PayloadToList<AppDomain>();

                foreach (AppDomain domain in _domains)
                {
                    domain.App = _editApp;
                    domain.Primary = (_editApp.PrimaryDomainId == domain.Id);
                    domain.IPAddress = _editApp.IPAddress;
 
                    AppDomainCertificate seedCert = new AppDomainCertificate();
                    seedCert.AppDomain = domain;

                    status = await Cloud.ResultMultiple<AppDomainCertificate>.WaitForObjectAsync(
                        NesterControl.Service, throwIfError, seedCert);

                    if (status.Code >= 0)
                    {
                        ObservableCollection<AppDomainCertificate> list = 
                            status.PayloadToList<AppDomainCertificate>();

                        if (list.Any())
                        {
                            domain.Certificate = list.First();
                            domain.Certificate.AppDomain = domain;
                        }
                    }
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryDomainAsync(AppDomain domain = null, 
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain>(
                    NesterControl.Service.QueryAsync), doCache, null, null);

            if (status.Code >= 0)
            {
                _editDomain = status.PayloadToObject<AppDomain>();

                AppDomainCertificate seedCert = new AppDomainCertificate();
                seedCert.AppDomain = _editDomain;

                status = await Cloud.ResultMultiple<AppDomainCertificate>.WaitForObjectAsync(
                    NesterControl.Service, throwIfError, seedCert, doCache);

                if (status.Code >= 0)
                {
                    ObservableCollection<AppDomainCertificate> list =
                        status.PayloadToList<AppDomainCertificate>();

                    if (list.Any())
                    {
                        theDomain.Certificate = list.First();
                        theDomain.Certificate.AppDomain = theDomain;
                    }
                }

                if (domain != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editDomain, domain);
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> CreateDomainAsync(AppDomain domain = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain>(
                    NesterControl.Service.CreateAsync), doCache);

            if (status.Code >= 0)
            {
                _editDomain = status.PayloadToObject<AppDomain>();
                 
                // Add the default free certificate
                AppDomainCertificate cert = new AppDomainCertificate();
                cert.AppDomain = _editDomain;
                cert.Tag = _editDomain.Tag;
                cert.Type = "free";

                await CreateDomainCertificateAsync(cert);

                if (domain != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editDomain, domain);
                    _domains.Add(domain);
                    OnPropertyChanged("Domains");
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> CreateDomainCertificateAsync(AppDomainCertificate cert = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate>(
                    NesterControl.Service.CreateAsync));

            if (status.Code >= 0)
            {
                _editDomain.Certificate = status.PayloadToObject<AppDomainCertificate>();

                if (cert != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editDomain.Certificate, cert);
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RemoveDomainAsync(AppDomain domain = null,
             bool doCache = false, bool throwIfError = true)
         {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain>(
                    NesterControl.Service.RemoveAsync), doCache);

            if (status.Code >= 0)
            {
                if (domain != null)
                {
                    // any cert that belong to the domain
                    // are automatically removed in the server
                    _domains.Remove(domain);
                    OnPropertyChanged("Domains");
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RemoveDomainCertificateAsync(AppDomainCertificate cert = null,
             bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate>(
                    NesterControl.Service.RemoveAsync), doCache);

            if (status.Code >= 0)
            {
                if (cert == null)
                {
                    _editDomain.Certificate = null;
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateDomainAsync(AppDomain domain = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain>(
                    NesterControl.Service.UpdateAsync), doCache);

            if (status.Code >= 0)
            {
                _editDomain = status.PayloadToObject<AppDomain>();

                /* updates to the domain invalidates attached
                 * certificates.
                */
                _editDomain.Certificate = null;
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateDomainCertificateAsync(AppDomainCertificate cert = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            Cloud.ServerStatus status = await Cloud.ResultSingle<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate>(
                    NesterControl.Service.UpdateAsync), doCache);

            if (status.Code >= 0)
            {
                _editDomain.Certificate = status.PayloadToObject<AppDomainCertificate>();
            }

            return status;
        }
    }
}
