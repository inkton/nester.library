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
using Inkton.Nest.Model;

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
            _editDomain.OwnedBy = app;
        }

        override public App EditApp
        {
            get
            {
                return _editApp;
            }
            set
            {
                _editDomain.OwnedBy = value;
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

        public async Task InitAsync()
        {
            await QueryDomainsAsync();
        }

        public async Task<Cloud.ResultMultiple<AppDomain>> QueryDomainsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            Cloud.ResultMultiple<AppDomain> result = await Cloud.ResultMultiple<AppDomain>.WaitForObjectAsync(
                Keeper.Service, throwIfError, _editDomain, doCache);

            if (result.Code >= 0)
            {
                _domains = result.Data.Payload;

                foreach (AppDomain domain in _domains)
                {
                    domain.OwnedBy = _editApp;
                    domain.Primary = (_editApp.PrimaryDomainId == domain.Id);
                    domain.IPAddress = _editApp.IPAddress;
 
                    AppDomainCertificate seedCert = new AppDomainCertificate();
                    seedCert.OwnedBy = domain;

                    Cloud.ResultMultiple<AppDomainCertificate> certResult = 
                        await Cloud.ResultMultiple<AppDomainCertificate>.WaitForObjectAsync(
                            Keeper.Service, throwIfError, seedCert);

                    if (certResult.Code >= 0)
                    {
                        ObservableCollection<AppDomainCertificate> list = certResult.Data.Payload;

                        if (list.Any())
                        {
                            domain.Certificate = list.First();
                            domain.Certificate.OwnedBy = domain;
                        }
                    }
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppDomain>> QueryDomainAsync(AppDomain domain = null, 
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ResultSingle<AppDomain> result = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, Cloud.ResultSingle<AppDomain>>(
                    Keeper.Service.QueryAsync), doCache, null, null);

            if (result.Code >= 0)
            {
                _editDomain = result.Data.Payload;

                AppDomainCertificate seedCert = new AppDomainCertificate();
                seedCert.OwnedBy = _editDomain;

                Cloud.ResultMultiple<AppDomainCertificate> certResult = 
                        await Cloud.ResultMultiple<AppDomainCertificate>.WaitForObjectAsync(
                            Keeper.Service, throwIfError, seedCert, doCache);

                if (certResult.Code >= 0)
                {
                    ObservableCollection<AppDomainCertificate> list = certResult.Data.Payload;

                    if (list.Any())
                    {
                        theDomain.Certificate = list.First();
                        theDomain.Certificate.OwnedBy = theDomain;
                    }
                }

                if (domain != null)
                {
                    _editDomain.CopyTo(domain);
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppDomain>> CreateDomainAsync(AppDomain domain = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ResultSingle<AppDomain> result = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, Cloud.ResultSingle<AppDomain>>(
                    Keeper.Service.CreateAsync), doCache);

            if (result.Code >= 0)
            {
                _editDomain = result.Data.Payload;
                 
                // Add the default free certificate
                AppDomainCertificate cert = new AppDomainCertificate();
                cert.OwnedBy = _editDomain;
                cert.Tag = _editDomain.Tag;
                cert.Type = "free";

                await CreateDomainCertificateAsync(cert);

                if (domain != null)
                {
                    _editDomain.CopyTo(domain);
                    _domains.Add(domain);
                    OnPropertyChanged("Domains");
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppDomainCertificate>> CreateDomainCertificateAsync(AppDomainCertificate cert = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            Cloud.ResultSingle<AppDomainCertificate> result = await Cloud.ResultSingle<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate, Cloud.ResultSingle<AppDomainCertificate>>(
                    Keeper.Service.CreateAsync));

            if (result.Code >= 0)
            {
                _editDomain.Certificate = result.Data.Payload;

                if (cert != null)
                {
                    _editDomain.Certificate.CopyTo(cert);
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppDomain>> RemoveDomainAsync(AppDomain domain = null,
             bool doCache = false, bool throwIfError = true)
         {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ResultSingle<AppDomain> result = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, Cloud.ResultSingle<AppDomain>>(
                    Keeper.Service.RemoveAsync), doCache);

            if (result.Code >= 0)
            {
                if (domain != null)
                {
                    // any cert that belong to the domain
                    // are automatically removed in the server
                    _domains.Remove(domain);
                    OnPropertyChanged("Domains");
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppDomainCertificate>> RemoveDomainCertificateAsync(AppDomainCertificate cert = null,
             bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            Cloud.ResultSingle<AppDomainCertificate> result = await Cloud.ResultSingle<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate, Cloud.ResultSingle<AppDomainCertificate>>(
                    Keeper.Service.RemoveAsync), doCache);

            if (result.Code >= 0)
            {
                if (cert == null)
                {
                    _editDomain.Certificate = null;
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppDomain>> UpdateDomainAsync(AppDomain domain = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            Cloud.ResultSingle<AppDomain> result = await Cloud.ResultSingle<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, Cloud.ResultSingle<AppDomain>>(
                    Keeper.Service.UpdateAsync), doCache);

            if (result.Code >= 0)
            {
                _editDomain = result.Data.Payload;

                /* updates to the domain invalidates attached
                 * certificates.
                */
                _editDomain.Certificate = null;
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<AppDomainCertificate>> UpdateDomainCertificateAsync(AppDomainCertificate cert = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            Cloud.ResultSingle<AppDomainCertificate> result = await Cloud.ResultSingle<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate, Cloud.ResultSingle<AppDomainCertificate>>(
                    Keeper.Service.UpdateAsync), doCache);

            if (result.Code >= 0)
            {
                _editDomain.Certificate = result.Data.Payload;
            }

            return result;
        }
    }
}
