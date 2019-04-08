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
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class DomainViewModel : ViewModel
    {
        private AppDomain _editDomain;
        private bool _primary;

        private ObservableCollection<AppDomain> _domains;

        public ICommand EditCertCommand { get; private set; }

        public DomainViewModel(NesterService platform, App app) : base(platform, app)
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

        public async Task<ResultMultiple<AppDomain>> QueryDomainsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            ResultMultiple<AppDomain> result = await ResultMultipleUI<AppDomain>.WaitForObjectAsync(
                Platform, throwIfError, _editDomain, doCache);

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

                    ResultMultiple<AppDomainCertificate> certResult = 
                        await ResultMultipleUI<AppDomainCertificate>.WaitForObjectAsync(
                            Platform, throwIfError, seedCert);

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

        public async Task<ResultSingle<AppDomain>> QueryDomainAsync(AppDomain domain = null, 
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            ResultSingle<AppDomain> result = await ResultSingleUI<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, ResultSingle<AppDomain>>(
                    Platform.QueryAsync), doCache, null, null);

            if (result.Code >= 0)
            {
                _editDomain = result.Data.Payload;

                AppDomainCertificate seedCert = new AppDomainCertificate();
                seedCert.OwnedBy = _editDomain;

                ResultMultiple<AppDomainCertificate> certResult = 
                        await ResultMultipleUI<AppDomainCertificate>.WaitForObjectAsync(
                            Platform, throwIfError, seedCert, doCache);

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

        public async Task<ResultSingle<AppDomain>> CreateDomainAsync(AppDomain domain = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            ResultSingle<AppDomain> result = await ResultSingleUI<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, ResultSingle<AppDomain>>(
                    Platform.CreateAsync), doCache);

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

        public async Task<ResultSingle<AppDomainCertificate>> CreateDomainCertificateAsync(AppDomainCertificate cert = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            ResultSingle<AppDomainCertificate> result = await ResultSingleUI<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate, ResultSingle<AppDomainCertificate>>(
                    Platform.CreateAsync));

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

        public async Task<ResultSingle<AppDomain>> RemoveDomainAsync(AppDomain domain = null,
             bool doCache = false, bool throwIfError = true)
         {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            ResultSingle<AppDomain> result = await ResultSingleUI<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, ResultSingle<AppDomain>>(
                    Platform.RemoveAsync), doCache);

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

        public async Task<ResultSingle<AppDomainCertificate>> RemoveDomainCertificateAsync(AppDomainCertificate cert = null,
             bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            ResultSingle<AppDomainCertificate> result = await ResultSingleUI<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate, ResultSingle<AppDomainCertificate>>(
                    Platform.RemoveAsync), doCache);

            if (result.Code >= 0)
            {
                if (cert == null)
                {
                    _editDomain.Certificate = null;
                }
            }

            return result;
        }

        public async Task<ResultSingle<AppDomain>> UpdateDomainAsync(AppDomain domain = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomain theDomain = domain == null ? _editDomain : domain;

            ResultSingle<AppDomain> result = await ResultSingleUI<AppDomain>.WaitForObjectAsync(
                throwIfError, theDomain, new Cloud.CachedHttpRequest<AppDomain, ResultSingle<AppDomain>>(
                    Platform.UpdateAsync), doCache);

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

        public async Task<ResultSingle<AppDomainCertificate>> UpdateDomainCertificateAsync(AppDomainCertificate cert = null,
            bool doCache = false, bool throwIfError = true)
        {
            AppDomainCertificate theCert = cert == null ? _editDomain.Certificate : cert;

            ResultSingle<AppDomainCertificate> result = await ResultSingleUI<AppDomainCertificate>.WaitForObjectAsync(
                throwIfError, theCert, new Cloud.CachedHttpRequest<AppDomainCertificate, ResultSingle<AppDomainCertificate>>(
                    Platform.UpdateAsync), doCache);

            if (result.Code >= 0)
            {
                _editDomain.Certificate = result.Data.Payload;
            }

            return result;
        }
    }
}
