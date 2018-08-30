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
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class AppCollectionViewModel : ViewModel
    {
        private ObservableCollection<AppViewModel> _appModels;

        public AppCollectionViewModel()
        {
            _editApp = new App();
            _editApp.Owner = Keeper.User;
            _appModels = new ObservableCollection<AppViewModel>();
        }

        public ObservableCollection<AppViewModel> AppModels
        {
            get
            {
                return _appModels;
            }
            set
            {
                SetProperty(ref _appModels, value);
            }
        }

        public async Task<Cloud.ServerStatus> LoadApps(
            bool doCache = true, bool throwIfError = true)
        {
            Cloud.ServerStatus status = await Cloud.ResultMultiple<App>.WaitForObjectAsync(
                Keeper.Service, throwIfError, _editApp, doCache);

            if (status.Code == 0)
            {
                ObservableCollection<App> apps = status.PayloadToList<App>();

                if (apps.Any())
                {
                    foreach (App app in apps)
                    {
                        AddApp(app);
                    }
                }
            }

            return status;
        }

        public Cloud.ServerStatus AddApp(App app)
        {
            AppViewModel appModel = new AppViewModel();
            appModel.EditApp = app;
            AddModel(appModel);

            return Task<Cloud.ServerStatus>
                .Run(async () => await appModel.InitAsync()).Result;            
        }

        public void AddModel(AppViewModel appModel)
        {
            _appModels.Add(appModel);
            OnPropertyChanged("Apps");
        }

        public void RemoveApp(AppViewModel appModel)
        {
            _appModels.Remove(appModel);
            OnPropertyChanged("Apps");
        }
    }
}
