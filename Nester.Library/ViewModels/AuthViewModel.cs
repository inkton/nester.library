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
using Inkton.Nest.Model;
using Inkton.Nest.Cloud;
using Inkton.Nester.Cloud;

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

            if (Keeper.ViewModels.AppViewModel != null)
                Keeper.ViewModels.AppViewModel
                .EditApp.OwnedBy = newPermit.Owner;
            if (Keeper.ViewModels.PaymentViewModel != null)
                Keeper.ViewModels.PaymentViewModel
                .EditPaymentMethod.OwnedBy = newPermit.Owner;
            if (Keeper.ViewModels.AppCollectionViewModel != null)
                Keeper.ViewModels.AppCollectionViewModel
                .EditApp.OwnedBy = newPermit.Owner;
            if (Keeper.Service != null)
                Keeper.Service.Permit = newPermit;

            newPermit.CopyTo(_permit);
        }

        public ResultSingle<Permit> Signup(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result =
                Keeper.Service.Signup(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    new ResultHandler<Permit>(result).Throw();
            }
            else
            {
                _permit.Owner.CopyTo(
                    Keeper.User);
            }

            return result;
        }

        public async Task<ResultSingle<Permit>> RecoverPasswordAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result = await
                Keeper.Service.RecoverPasswordAsync(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    new ResultHandler<Permit>(result).Throw();
            }

            return result;
        }

        public ResultSingle<Permit> QueryToken(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result =
                Keeper.Service.QueryToken(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    new ResultHandler<Permit>(result).Throw();
            }
            else
            {
                ChangePermit(result.Data.Payload);
            }

            return result;
        }

        public async Task<ResultSingle<Permit>> QueryTokenAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result =
                await Keeper.Service.QueryTokenAsync(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    new ResultHandler<Permit>(result).Throw();
            }
            else
            {
                ChangePermit(result.Data.Payload);
            }

            return result;
        }

        public async Task<ResultSingle<Permit>> ResetTokenAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result = await
                Keeper.Service.ResetTokenAsync(_permit);

            if (result.Code < 0)
            {
                if (throwIfError)
                    new ResultHandler<Permit>(result).Throw();
            }
            else
            {
                ChangePermit(_permit);
            }

            return result;
        }

        public async Task<ResultSingle<User>> UpdateUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? _permit.Owner : user;

            ResultSingle<User> result = await ResultSingleUI<User>.WaitForObjectAsync(
                throwIfError, user, new Cloud.CachedHttpRequest<User, ResultSingle<User>>(
                    Keeper.Service.UpdateAsync), doCache);

            return result;
        }

        public async Task<ResultSingle<User>> DeleteUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? _permit.Owner : user;

            ResultSingle<User> result = await ResultSingleUI<User>.WaitForObjectAsync(
                throwIfError, theUser, new Cloud.CachedHttpRequest<User, ResultSingle<User>>(
                    Keeper.Service.RemoveAsync), doCache);

            return result;
        }

        public async Task<ResultMultiple<UserEvent>> QueryUserEventsAsync(User user,
            bool doCache = false, bool throwIfError = true)
        {
            UserEvent userEventSeed = new UserEvent();
            userEventSeed.OwnedBy = Keeper.User;

            ResultMultiple<UserEvent> result = await ResultMultipleUI<UserEvent>.WaitForObjectAsync(
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
