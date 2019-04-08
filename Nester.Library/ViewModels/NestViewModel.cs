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
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class NestViewModel : ViewModel
    {
        private Inkton.Nest.Model.Nest _editNest;

        private ObservableCollection<Inkton.Nest.Model.Nest> _nests = null;
        private ObservableCollection<NestPlatform> _platforms = null;

        public ICommand RemoveCommand { get; private set; }

        public NestViewModel(NesterService platform, App app) : base(platform, app)
        {
            _nests = new ObservableCollection<Inkton.Nest.Model.Nest>();
            _platforms = new ObservableCollection<NestPlatform>();

            _editNest = new Inkton.Nest.Model.Nest();
            _editNest.OwnedBy = app;

            RemoveCommand = new Command<Inkton.Nest.Model.Nest>(SendRemoveMessage);
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
                _editNest.OwnedBy = value;
            }
        }

        public void SendRemoveMessage(Inkton.Nest.Model.Nest nest)
        {
            ManagedObjectMessage<Inkton.Nest.Model.Nest> doThis =
                new ManagedObjectMessage<Inkton.Nest.Model.Nest>("remove", nest);
            MessagingCenter.Send(doThis, doThis.Type);
        }

        public Inkton.Nest.Model.Nest EditNest
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

        public ObservableCollection<Inkton.Nest.Model.Nest> Nests
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

        public async Task InitAsync()
        {
            await QueryNestPlatformsAsync();
            await QueryNestsAsync();
        }

        public async Task<ResultMultiple<NestPlatform>> QueryNestPlatformsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            NestPlatform platformSeed = new NestPlatform();
            platformSeed.OwnedBy = _editApp;

            ResultMultiple<NestPlatform> result = await ResultMultipleUI<NestPlatform>.WaitForObjectAsync(
                Platform, throwIfError, platformSeed, doCache);

            if (result.Code >= 0)
            {
                _platforms = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultMultiple<Inkton.Nest.Model.Nest>> QueryNestsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            _editNest.OwnedBy = _editApp;
            ResultMultiple<Inkton.Nest.Model.Nest> result = await ResultMultipleUI<Inkton.Nest.Model.Nest>.WaitForObjectAsync(
                Platform, throwIfError, _editNest, doCache);

            if (result.Code >= 0)
            {
                _nests = result.Data.Payload;
                OnPropertyChanged("Nests");
            }

            return result;
        }

        public async Task<ResultSingle<Inkton.Nest.Model.Nest>> QueryNestAsync(Inkton.Nest.Model.Nest nest = null,
             bool doCache = false, bool throwIfError = true)
        {
            Inkton.Nest.Model.Nest theNest = nest == null ? _editNest : nest;

            ResultSingle<Inkton.Nest.Model.Nest> result = await ResultSingleUI<Inkton.Nest.Model.Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Inkton.Nest.Model.Nest, ResultSingle<Inkton.Nest.Model.Nest>>(
                    Platform.QueryAsync), doCache, null, null);

            if (result.Code >= 0)
            {
                _editNest = result.Data.Payload;

                if (nest != null)
                {
                    _editNest.CopyTo(nest);
                }
            }

            return result;
        }

        public async Task<ResultSingle<Inkton.Nest.Model.Nest>> CreateNestAsync(Inkton.Nest.Model.Nest nest = null,
            bool doCache = false, bool throwIfError = true)
        {
            Inkton.Nest.Model.Nest theNest = nest == null ? _editNest : nest;

            ResultSingle<Inkton.Nest.Model.Nest> result = await ResultSingleUI<Inkton.Nest.Model.Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Inkton.Nest.Model.Nest, ResultSingle<Inkton.Nest.Model.Nest>>(
                    Platform.CreateAsync), doCache);

            if (result.Code >= 0)
            {
                _editNest = result.Data.Payload;

                if (nest != null)
                {
                    _editNest.CopyTo(nest);
                    _nests.Add(nest);
                }

                OnPropertyChanged("Nests");
            }

            return result;
        }

        public async Task<ResultSingle<Inkton.Nest.Model.Nest>> UpdateNestAsync(Inkton.Nest.Model.Nest nest = null,
            bool doCache = false, bool throwIfError = true)
        {
            Inkton.Nest.Model.Nest theNest = nest == null ? _editNest : nest;

            ResultSingle<Inkton.Nest.Model.Nest> result = await ResultSingleUI<Inkton.Nest.Model.Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Inkton.Nest.Model.Nest, ResultSingle<Inkton.Nest.Model.Nest>>(
                    Platform.UpdateAsync), doCache);

            if (result.Code >= 0)
            {
                _editNest = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultSingle<Inkton.Nest.Model.Nest>> RemoveNestAsync(Inkton.Nest.Model.Nest nest = null,
            bool doCache = false, bool throwIfError = true)
        {
            Inkton.Nest.Model.Nest theNest = nest == null ? _editNest : nest;

            ResultSingle<Inkton.Nest.Model.Nest> result = await ResultSingleUI<Inkton.Nest.Model.Nest>.WaitForObjectAsync(
                throwIfError, theNest, new Cloud.CachedHttpRequest<Inkton.Nest.Model.Nest, ResultSingle<Inkton.Nest.Model.Nest>>(
                    Platform.RemoveAsync), doCache);

            if (result.Code >= 0)
            {
                if (nest != null)
                {
                    _nests.Remove(nest);
                    OnPropertyChanged("Nests");
                }
            }

            return result;
        }
    }
}
