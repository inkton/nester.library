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
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class AuthViewModel : ViewModel
    {
        private Permit _permit;
        private ObservableCollection<UserEvent> _userEvents;
        private bool _canRecoverPassword = false;

        public AuthViewModel()
        {
            _permit = new Permit();
            _permit.Owner = Keeper.User;

            _userEvents = new ObservableCollection<UserEvent>();
        }

        public Permit Permit
        {
            get { return _permit; }
            set { SetProperty(ref _permit, value); }
        }

        public bool CanRecoverPassword
        {
            get { return _canRecoverPassword; }
            set { SetProperty(ref _canRecoverPassword, value); }
        }

        public void Reset()
        {
            Keeper.Service.Permit = null;
            _permit.SecurityCode = null;
            _permit.Token = null;
        }

        public void ChangePermit(Permit newPermit)
        {
            Keeper.User = newPermit.Owner;

            if (Keeper.BaseModels.TargetViewModel != null)
                Keeper.BaseModels.TargetViewModel
                .EditApp.Owner = newPermit.Owner;
            if (Keeper.BaseModels.PaymentViewModel != null)
                Keeper.BaseModels.PaymentViewModel
                .EditPaymentMethod.Owner = newPermit.Owner;
            if (Keeper.BaseModels.AllApps != null)
                Keeper.BaseModels.AllApps
                .EditApp.Owner = newPermit.Owner;
            if (Keeper.Service != null)
                Keeper.Service.Permit = newPermit;

            Cloud.Object.CopyPropertiesTo(newPermit, _permit);
        }

        public Cloud.ServerStatus Signup(
            bool throwIfError = true)
        {
            Cloud.ServerStatus status =
                Keeper.Service.Signup(_permit);

            if (status.Code < 0)
            {
                if (throwIfError)
                    status.Throw();
            }
            else
            {
                Cloud.Object.CopyPropertiesTo(
                    _permit.Owner, Keeper.User);
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RecoverPasswordAsync(
            bool throwIfError = true)
        {
            Cloud.ServerStatus status = await
                Keeper.Service.RecoverPasswordAsync(_permit);

            if (status.Code < 0)
            {
                if (throwIfError)
                    status.Throw();
            }

            return status;
        }

        public Cloud.ServerStatus QueryToken(
            bool throwIfError = true)
        {
            Cloud.ServerStatus status =
                Keeper.Service.QueryToken(_permit);

            if (status.Code < 0)
            {
                if (throwIfError)
                    status.Throw();
            }
            else
            {
                ChangePermit(status.PayloadToObject<Permit>());
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryTokenAsync(
            bool throwIfError = true)
        {
            Cloud.ServerStatus status =
                await Keeper.Service.QueryTokenAsync(_permit);

            if (status.Code < 0)
            {
                if (throwIfError)
                    status.Throw();
            }
            else
            {
                ChangePermit(status.PayloadToObject<Permit>());
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> ResetTokenAsync(
            bool throwIfError = true)
        {
            Cloud.ServerStatus status = await
                Keeper.Service.ResetTokenAsync(_permit);

            if (status.Code < 0)
            {
                if (throwIfError)
                    status.Throw();
            }
            else
            {
                ChangePermit(_permit);
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? _permit.Owner : user;

            Cloud.ServerStatus status = await Cloud.ResultSingle<User>.WaitForObjectAsync(
                throwIfError, user, new Cloud.CachedHttpRequest<User>(
                    Keeper.Service.UpdateAsync), doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> DeleteUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? _permit.Owner : user;

            Cloud.ServerStatus status = await Cloud.ResultSingle<User>.WaitForObjectAsync(
                throwIfError, theUser, new Cloud.CachedHttpRequest<User>(
                    Keeper.Service.RemoveAsync), doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryUserEventsAsync(User user,
            bool doCache = false, bool throwIfError = true)
        {
            UserEvent userEventSeed = new UserEvent();
            userEventSeed.Owner = Keeper.User;

            Cloud.ServerStatus status = await Cloud.ResultMultiple<UserEvent>.WaitForObjectAsync(
                Keeper.Service, throwIfError, userEventSeed, doCache);

            if (status.Code >= 0)
            {
                _userEvents = status.PayloadToList<UserEvent>();
            }

            OnPropertyChanged("UserEvents");
            return status;
        }

        public ObservableCollection<UserEvent> UserEvents
        {
            get { return _userEvents; }
        }
    }
}
