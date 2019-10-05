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

using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Inkton.Nest.Model;
using Inkton.Nest.Cloud;
using Inkton.Nester.Cloud;
using Inkton.Nester.Helpers;

namespace Inkton.Nester.ViewModels
{
    public class AuthViewModel<UserT> : ViewModel<UserT>
        where UserT : User, new()
    {
        private ObservableCollection<UserEvent> _userEvents;
        private bool _canRecoverPassword;

        public enum PermitAction
        {
            RequestEmailConfirmation,
            ConfirmEmail,
            Login,
            ChangePassword
        }

        public AuthViewModel(BackendService<UserT> backend)
            :base(backend)
        {
            _userEvents = new ObservableCollection<UserEvent>();
            _canRecoverPassword = false;
        }

        public bool CanRecoverPassword
        {
            get { return _canRecoverPassword; }
            set { SetProperty(ref _canRecoverPassword, value); }
        }

        public bool IsAuthenticated
        {
            get { return Backend.Permit.AccessToken.Length > 0; }
        }

        public ObservableCollection<UserEvent> UserEvents
        {
            get { return _userEvents; }
            set { SetProperty(ref _userEvents, value); }
        }

        public async Task<ResultSingle<Permit<UserT>>> SignupAsync(
            string password, bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Backend.Permit.User.Email));

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["password"] = password;

            ResultSingle<Permit<UserT>> result =
                await Backend.SignupAsync(data);

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public async Task<ResultSingle<Permit<UserT>>> RequestEmailConfirmationAsync(
            bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Backend.Permit.User.Email));

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["action"] = PermitAction.RequestEmailConfirmation.ToString();

            ResultSingle<Permit<UserT>> result =
                await Backend.SetupPermitAsync(data);

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public async Task<ResultSingle<Permit<UserT>>> ConfirmEmailAsync(
            string securityCode, string password, bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Backend.Permit.User.Email));

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["action"] = PermitAction.ConfirmEmail.ToString();
            data["securityCode"] = securityCode;
            data["password"] = password;

            ResultSingle<Permit<UserT>> result =
                await Backend.SetupPermitAsync(data);

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public async Task<ResultSingle<Permit<UserT>>> LoginAsync(
            string password, bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Backend.Permit.User.Email));

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["action"] = PermitAction.Login.ToString();
            data["password"] = password;

            ResultSingle<Permit<UserT>> result =
                await Backend.SetupPermitAsync(data);

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public async Task<ResultSingle<Permit<UserT>>> ChangePasswordAsync(
            string securityCode, string password, bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Backend.Permit.User.Email));

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["action"] = PermitAction.ChangePassword.ToString();
            data["securityCode"] = securityCode;
            data["password"] = password;

            ResultSingle<Permit<UserT>> result =
                await Backend.SetupPermitAsync(data);

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public async Task<ResultSingle<Permit<UserT>>> RenewTokenAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit<UserT>> result =
                await Backend.RenewAccessAsync();

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public async Task<ResultSingle<Permit<UserT>>> RevokeTokenAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit<UserT>> result = await
                Backend.RevokeAccessAsync();

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }       

        public async Task<ResultSingle<UserT>> UpdateUserAsync(UserT user = null,
            bool doCache = false, bool throwIfError = true, Dictionary<string, object> data = null)
        {
            UserT theUser = user == null ? Backend.Permit.User : user;

            ResultSingle<UserT> result = await ResultSingleUI<UserT>.WaitForObjectAsync(
                throwIfError, user, new CachedHttpRequest<UserT, ResultSingle<UserT>>(
                    Backend.UpdateAsync), doCache, data);

            return result;
        }

        public async Task<ResultSingle<UserT>> DeleteUserAsync(UserT user = null,
            bool doCache = false, bool throwIfError = true, Dictionary<string, object> data = null)
        {
            UserT theUser = user == null ? Backend.Permit.User : user;

            ResultSingle<UserT> result = await ResultSingleUI<UserT>.WaitForObjectAsync(
                throwIfError, theUser, new CachedHttpRequest<UserT, ResultSingle<UserT>>(
                    Backend.RemoveAsync), doCache);

            return result;
        }

        public async Task<ResultMultiple<UserEvent>> QueryUserEventsAsync(UserT user,
            bool doCache = false, bool throwIfError = true, Dictionary<string, object> data = null)
        {
            UserEvent userEventSeed = new UserEvent();
            userEventSeed.OwnedBy = user == null ? Backend.Permit.User : user;

            ResultMultiple<UserEvent> result = await ResultMultipleUI<UserEvent>.WaitForObjectsAsync(
                true, userEventSeed, new CachedHttpRequest<UserEvent, ResultMultiple<UserEvent>>(
                    Backend.QueryAsyncListAsync), true);

            if (result.Code >= 0)
            {
                UserEvents = result.Data.Payload;
            }

            return result;
        } 
    }
}
