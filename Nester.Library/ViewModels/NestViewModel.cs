﻿/*
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
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class NestViewModel : ViewModel
    {
        private Nest _editNest;

        private ObservableCollection<Nest> _nests = null;        
        private ObservableCollection<NestPlatform> _platforms = null;

        public ICommand RemoveCommand { get; private set; }

        public NestViewModel(App app) : base(app)
        {
            _nests = new ObservableCollection<Nest>();
            _platforms = new ObservableCollection<NestPlatform>();

            _editNest = new Nest();
            _editNest.App = app;

            RemoveCommand = new Command<Nest>(SendRemoveMessage);
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
                _editNest.App = value;
            }
        }

        public void SendRemoveMessage(Nest nest)
        {
            ManagedObjectMessage<Nest> doThis =
                new ManagedObjectMessage<Nest>("remove", nest);
            MessagingCenter.Send(doThis, doThis.Type);
        }

        public Nest EditNest
        {
            get
            {
                return _editNest;
            }
            set
            {
                SetProperty(ref _editNest, value);
            }
        }

        public ObservableCollection<Nest> Nests
        {
            get
            {
                return _nests;
            }
            set
            {
                SetProperty(ref _nests, value);
            }
        }

        public ObservableCollection<NestPlatform> Platforms
        {
            get
            {
                return _platforms;
            }
            set
            {
                SetProperty(ref _platforms, value);
            }
        }

        override public async Task<Cloud.ServerStatus> InitAsync()
        {
            Cloud.ServerStatus status;

            status = await QueryNestPlatformsAsync();
            if (status.Code < 0)
            {
                return status;
            }

            status = await QueryNestsAsync();
            return status;
        }

        public async Task<Cloud.ServerStatus> QueryNestPlatformsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            NestPlatform platformSeed = new NestPlatform();
            platformSeed.App = _editApp;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<NestPlatform>.WaitForObjectAsync(
                Keeper.Service, throwIfError, platformSeed, doCache);

            if (status.Code >= 0)
            {
                _platforms = status.PayloadToList<NestPlatform>();
            }

            return status;
        }

        private void SetNestHosts(Nest nest)
        {
            foreach (NestPlatform platform in _platforms)
            {
                if (nest.PlatformId == platform.Id)
                {
                    nest.Platform = platform;
                    break;
                }
            }
        }

        private void SetNestsHosts()
        {
            foreach (Nest nest in _nests)
            {
                SetNestHosts(nest);
            }
        }

        public async Task<Cloud.ServerStatus> QueryNestsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            _editNest.App = _editApp;
            Cloud.ServerStatus status = await Cloud.ResultMultiple<Nest>.WaitForObjectAsync(
                Keeper.Service, throwIfError, _editNest, doCache);

            if (status.Code >= 0)
            {
                _nests = status.PayloadToList<Nest>();
                SetNestsHosts();
                OnPropertyChanged("Nests");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryNestAsync(Nest nest = null,
             bool doCache = false, bool throwIfError = true)
        {
            Nest theNest = nest == null ? _editNest : nest;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Nest>(
                    Keeper.Service.QueryAsync), doCache, null, null);

            if (status.Code >= 0)
            {
                _editNest = status.PayloadToObject<Nest>();
                SetNestHosts(_editNest);

                if (nest != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editNest, nest);
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> CreateNestAsync(Nest nest = null,
            bool doCache = false, bool throwIfError = true)
        {
            Nest theNest = nest == null ? _editNest : nest;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Nest>(
                    Keeper.Service.CreateAsync), doCache);

            if (status.Code >= 0)
            {
                _editNest = status.PayloadToObject<Nest>();
                SetNestHosts(_editNest);

                if (nest != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editNest, nest);
                    _nests.Add(nest);               
                }

                OnPropertyChanged("Nests");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateNestAsync(Nest nest = null,
            bool doCache = false, bool throwIfError = true)
        {
            Nest theNest = nest == null ? _editNest : nest;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Nest>(
                    Keeper.Service.UpdateAsync), doCache);

            if (status.Code >= 0)
            {
                _editNest = status.PayloadToObject<Nest>();
                SetNestHosts(_editNest);
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RemoveNestAsync(Nest nest = null,
            bool doCache = false, bool throwIfError = true)
        {
            Nest theNest = nest == null ? _editNest : nest;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Nest>(
                    Keeper.Service.RemoveAsync), doCache);

            if (status.Code >= 0)
            {
                if (nest != null)
                {
                    _nests.Remove(nest);
                    OnPropertyChanged("Nests");
                }
            }

            return status;
        }
    }
}
