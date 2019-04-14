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
        private ObservableCollection<UserEvent> _userEvents;
        private bool _canRecoverPassword = false;

        public AuthViewModel(NesterService platform)
            :base(platform)
        {
            _userEvents = new ObservableCollection<UserEvent>();
        }

        public bool CanRecoverPassword
        {
            get { return _canRecoverPassword; }
            set { SetProperty(ref _canRecoverPassword, value); }
        }

        public void Reset()
        {
            Permit permit = Platform.Permit;
            permit.Invalid();
            Platform.Permit = permit;
        }

        public void ChangePermit(Permit newPermit)
        {
            Platform.Permit = newPermit;
            Client.User = newPermit.Owner;
        }

        public ResultSingle<Permit> Signup(
            bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Platform.Permit.Owner.Email));

            if (string.IsNullOrWhiteSpace(Platform.Permit.Owner.Nickname)) {
                Platform.Permit.Owner.Nickname = Platform.Permit.Owner.Email;
            }

            ResultSingle<Permit> result =
                Platform.Signup();

            if (result.Code < 0)
            {
                if (throwIfError)
                    new ResultHandler<Permit>(result).Throw();
            }
           
            return result;
        }

        public async Task<ResultSingle<Permit>> SignupAsync(
            bool throwIfError = true)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Platform.Permit.Owner.Email));

            if (string.IsNullOrWhiteSpace(Platform.Permit.Owner.Nickname))
            {
                Platform.Permit.Owner.Nickname = Platform.Permit.Owner.Email;
            }

            ResultSingle<Permit> result =
                await Platform.SignupAsync();

            if (result.Code < 0)
            {
                if (throwIfError)
                {
                    List<AuthError> reasons =
                        JsonConvert.DeserializeObject<List<AuthError>>((result.Notes);
                    allErrors.ForEach
                    

                    //string json1 = @"{'Name':'James'}";
                    //var customer1 = JsonConvert.DeserializeAnonymousType(json1, definition);

                    new ResultHandler<Permit>(result).Throw();
                }
            }

            return result;
        }

        public async Task<ResultSingle<Permit>> RecoverPasswordAsync(
            bool throwIfError = true)
        {
            ResultSingle<Permit> result = await
                Platform.RecoverPasswordAsync();

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
                Platform.QueryToken();

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
                await Platform.QueryTokenAsync();

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
                Platform.ResetTokenAsync();

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

        public async Task<ResultSingle<User>> UpdateUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? Client.User : user;

            ResultSingle<User> result = await ResultSingleUI<User>.WaitForObjectAsync(
                throwIfError, user, new Cloud.CachedHttpRequest<User, ResultSingle<User>>(
                    Platform.UpdateAsync), doCache);

            return result;
        }

        public async Task<ResultSingle<User>> DeleteUserAsync(User user = null,
            bool doCache = false, bool throwIfError = true)
        {
            User theUser = user == null ? Client.User : user;

            ResultSingle<User> result = await ResultSingleUI<User>.WaitForObjectAsync(
                throwIfError, theUser, new Cloud.CachedHttpRequest<User, ResultSingle<User>>(
                    Platform.RemoveAsync), doCache);

            return result;
        }

        public async Task<ResultMultiple<UserEvent>> QueryUserEventsAsync(User user,
            bool doCache = false, bool throwIfError = true)
        {
            UserEvent userEventSeed = new UserEvent();
            userEventSeed.OwnedBy = user == null ? Client.User : user;

            ResultMultiple<UserEvent> result = await ResultMultipleUI<UserEvent>.WaitForObjectAsync(
                Platform, throwIfError, userEventSeed, doCache);

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
