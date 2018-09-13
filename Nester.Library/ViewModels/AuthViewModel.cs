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

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Inkton.Nest.Model;

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
                .EditApp.OwnedBy = newPermit.Owner;
            if (Keeper.BaseModels.PaymentViewModel != null)
                Keeper.BaseModels.PaymentViewModel
                .EditPaymentMethod.OwnedBy = newPermit.Owner;
            if (Keeper.BaseModels.AllApps != null)
                Keeper.BaseModels.AllApps
                .EditApp.OwnedBy = newPermit.Owner;
            if (Keeper.Service != null)
                Keeper.Service.Permit = newPermit;

            newPermit.CopyTo(_permit);
        }

        public Cloud.ResultSingle<Permit> Signup(
            bool throwIfError = true)
        {
            Cloud.ResultSingle<Permit> result =
                Keeper.Service.Signup(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    result.Throw();
            }
            else
            {
                _permit.Owner.CopyTo(
                    Keeper.User);
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Permit>> RecoverPasswordAsync(
            bool throwIfError = true)
        {
            Cloud.ResultSingle<Permit> result = await
                Keeper.Service.RecoverPasswordAsync(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    result.Throw();
            }

            return result;
        }

        public Cloud.ResultSingle<Permit> QueryToken(
            bool throwIfError = true)
        {
            Cloud.ResultSingle<Permit> result =
                Keeper.Service.QueryToken(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    result.Throw();
            }
            else
            {
                ChangePermit(result.Data.Payload);
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Permit>> QueryTokenAsync(
            bool throwIfError = true)
        {
            Cloud.ResultSingle<Permit> result =
                await Keeper.Service.QueryTokenAsync(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    result.Throw();
            }
            else
            {
                ChangePermit(result.Data.Payload);
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Permit>> ResetTokenAsync(
            bool throwIfError = true)
        {
            Cloud.ResultSingle<Permit> result = await
                Keeper.Service.ResetTokenAsync(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    result.Throw();
            }
            else
            {
                ChangePermit(_permit);
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<User>> UpdateUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? _permit.Owner : user;

            Cloud.ResultSingle<User> result = await Cloud.ResultSingle<User>.WaitForObjectAsync(
                throwIfError, user, new Cloud.CachedHttpRequest<User, Cloud.ResultSingle<User>>(
                    Keeper.Service.UpdateAsync), doCache);

            return result;
        }

        public async Task<Cloud.ResultSingle<User>> DeleteUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? _permit.Owner : user;

            Cloud.ResultSingle<User> result = await Cloud.ResultSingle<User>.WaitForObjectAsync(
                throwIfError, theUser, new Cloud.CachedHttpRequest<User, Cloud.ResultSingle<User>>(
                    Keeper.Service.RemoveAsync), doCache);

            return result;
        }

        public async Task< Cloud.ResultMultiple<UserEvent>> QueryUserEventsAsync(User user,
            bool doCache = false, bool throwIfError = true)
        {
            UserEvent userEventSeed = new UserEvent();
            userEventSeed.OwnedBy = Keeper.User;

             Cloud.ResultMultiple<UserEvent> result = await Cloud.ResultMultiple<UserEvent>.WaitForObjectAsync(
                Keeper.Service, throwIfError, userEventSeed, doCache);

            if (result.Code >= 0)
            {
                _userEvents = result.Data.Payload;
            }

            OnPropertyChanged("UserEvents");
            return result;
        }

        public ObservableCollection<UserEvent> UserEvents
        {
            get { return _userEvents; }
        }
    }
}
