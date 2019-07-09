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
using Inkton.Nest.Model;
using Inkton.Nest.Cloud;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class AuthViewModel : ViewModel
    {
        private ObservableCollection<Role> _roles;
        private ObservableCollection<UserEvent> _userEvents;
        private bool _canRecoverPassword;

        public AuthViewModel(BackendService backend)
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
            get { return Backend.Permit.Token.Length > 0; }
        }

        public ObservableCollection<Role> Roles
        {
            get { return _roles; }
            set { SetProperty(ref _roles, value); }
        }

        public ObservableCollection<UserEvent> UserEvents
        {
            get { return _userEvents; }
            set { SetProperty(ref _userEvents, value); }
        }

        public void UpdatePermit(ResultSingle<Permit> result, 
            bool throwIfError)
        {
            if (result.Code == 0)
            {
                result.Data.Payload.Owner.CopyTo(Backend.Permit.Owner);
                Backend.Permit.Token = result.Data.Payload.Token;
            }
            else if (throwIfError)
            {
                new ResultHandler<Permit>(result).Throw();
            }
        }

        public async Task<ResultSingle<Permit>> SignupAsync(
            bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Backend.Permit.Owner.Email));

            ResultSingle<Permit> result =
                await Backend.SignupAsync();

            UpdatePermit(result, throwIfError);

            return result;
        }

        public async Task<ResultSingle<Permit>> RecoverPasswordAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result = await
                Backend.RecoverPasswordAsync();

            if (result.Code < 0)
            {
                if (throwIfError)
                    new ResultHandler<Permit>(result).Throw();
            }

            return result;
        }

        public async Task<ResultSingle<Permit>> QueryTokenAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result =
                await Backend.QueryTokenAsync();

             UpdatePermit(result, throwIfError);

            return result;
        }

        public async Task<ResultSingle<Permit>> ResetTokenAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result = await
                Backend.ResetTokenAsync();

            UpdatePermit(result, throwIfError);

            return result;
        }

        public async Task<ResultSingle<User>> UpdateUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? Backend.Permit.Owner : user;

            ResultSingle<User> result = await ResultSingleUI<User>.WaitForObjectAsync(
                throwIfError, user, new Cloud.CachedHttpRequest<User, ResultSingle<User>>(
                    Backend.UpdateAsync), doCache);

            return result;
        }

        public async Task<ResultSingle<User>> DeleteUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? Backend.Permit.Owner : user;

            ResultSingle<User> result = await ResultSingleUI<User>.WaitForObjectAsync(
                throwIfError, theUser, new Cloud.CachedHttpRequest<User, ResultSingle<User>>(
                    Backend.RemoveAsync), doCache);

            return result;
        }

        public async Task<ResultMultiple<UserEvent>> QueryUserEventsAsync(User user,
            bool doCache = false, bool throwIfError = true)
        {
            UserEvent userEventSeed = new UserEvent();
            userEventSeed.OwnedBy = user == null ? Backend.Permit.Owner : user;

            ResultMultiple<UserEvent> result = await ResultMultipleUI<UserEvent>.WaitForObjectAsync(
                Backend, throwIfError, userEventSeed, doCache);

            if (result.Code >= 0)
            {
                UserEvents = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultMultiple<Role>> QueryRolesAsync(
            bool doCache = false, bool throwIfError = true)
        {
            Role roleSeed = new Role();

            ResultMultiple<Role> result = await ResultMultipleUI<Role>.WaitForObjectAsync(
                Backend, throwIfError, roleSeed, doCache);

            if (result.Code >= 0)
            {
                Roles = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultMultiple<Role>> QueryUserRolesAsync(User user,
            bool doCache = false, bool throwIfError = true)
        {
            Role userRoleSeed = new Role();
            userRoleSeed.OwnedBy = user == null ? Backend.Permit.Owner : user;

            ResultMultiple<Role> result = await ResultMultipleUI<Role>.WaitForObjectAsync(
                Backend, throwIfError, userRoleSeed, doCache);

            return result;
        }

        public async Task<ResultSingle<Role>> AddUserRoleAsync(User user,
            Role role, bool doCache = false, bool throwIfError = true)
        {
            role.OwnedBy = user;

            ResultSingle<Role> result = await ResultSingleUI<Role>.WaitForObjectAsync(
                throwIfError, role, new Cloud.CachedHttpRequest<Role, ResultSingle<Role>>(
                    Backend.CreateAsync), doCache);

            return result;
        }

        public async Task<ResultSingle<Role>> RemoveUserRoleAsync(User user,
            Role role, bool doCache = false, bool throwIfError = true)
        {
            role.OwnedBy = user;

            ResultSingle<Role> result = await ResultSingleUI<Role>.WaitForObjectAsync(
                throwIfError, role, new Cloud.CachedHttpRequest<Role, ResultSingle<Role>>(
                    Backend.RemoveAsync), doCache);

            return result;
        }
    }
}
