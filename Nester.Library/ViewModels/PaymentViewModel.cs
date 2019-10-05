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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class PaymentViewModel<UserT> : ViewModel<UserT>
        where UserT : User, new()
    {
        // The "edit" prefix is used as a convention to
        // indicate the object currently being edited.

        private PaymentMethod _editPaymentMethod;
        private Credit _editCredit;

        private ObservableCollection<BillingCycle> _billingCycles;
        private ObservableCollection<UserBillingTask> _userBillingTasks;

        public PaymentViewModel(BackendService<UserT> backend)
            : base(backend)
        {
            _editCredit = new Credit();

            _editPaymentMethod = new PaymentMethod();
        }

        public PaymentMethod EditPaymentMethod
        {
            get { return _editPaymentMethod; }
            private set { SetProperty(ref _editPaymentMethod, value); }
        }

        public Credit EditCredit
        {
            get { return _editCredit; }
            private set { SetProperty(ref _editCredit, value); }
        }

        public ObservableCollection<BillingCycle> BillingCycles
        {
            get { return _billingCycles; }
            private set { SetProperty(ref _billingCycles, value); }
        }

        public ObservableCollection<UserBillingTask> UserBillingTasks
        {
            get { return _userBillingTasks; }
            private set { SetProperty(ref _userBillingTasks, value); }
        }
      
        public async Task InitAsync()
        {
            SetDefaults();

            await QueryPaymentMethodAsync();
        }

        public void SetDefaults()
        {
            EditPaymentMethod.Id = 0;
            EditPaymentMethod.Tag = "stripe_cc";
            EditPaymentMethod.Type = "cc";
            EditPaymentMethod.ExpMonth = DateTime.Now.Month;
            EditPaymentMethod.ExpYear = DateTime.Now.Year;
            EditPaymentMethod.Token = string.Empty;
        }

        public async Task<ResultSingle<Credit>> QueryCreditAsync(Credit credit = null,
            bool dCache = false, bool throwIfError = true)
        {
            Credit theCredit = credit == null ? EditCredit : credit;

            ResultSingle<Credit> result = await ResultSingleUI<Credit>.WaitForObjectAsync(
                throwIfError, theCredit, new CachedHttpRequest<Credit, ResultSingle<Credit>>(
                    Backend.QueryAsync), dCache, null, null);

            if (result.Code >= 0)
            {
                EditCredit = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultSingle<PaymentMethod>> QueryPaymentMethodAsync(
            bool dCache = false, bool throwIfError = true)
        {
            EditPaymentMethod.OwnedBy = Backend.Permit.User;

            ResultSingle<PaymentMethod> result = await ResultSingleUI<PaymentMethod>.WaitForObjectAsync(
                throwIfError, EditPaymentMethod, new CachedHttpRequest<PaymentMethod, ResultSingle<PaymentMethod>>(
                    Backend.QueryAsync), dCache, null, null);

            if (result.Code >= 0)
            {                
                EditPaymentMethod = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultSingle<PaymentMethod>> CreatePaymentMethodAsync(
            bool doCache = false, bool throwIfError = true)
        {
            EditPaymentMethod.OwnedBy = Backend.Permit.User;

            ResultSingle<PaymentMethod> result = await ResultSingleUI<PaymentMethod>.WaitForObjectAsync(
                throwIfError, EditPaymentMethod, new CachedHttpRequest<PaymentMethod, ResultSingle<PaymentMethod>>(
                    Backend.CreateAsync), doCache);

            if (result.Code >= 0)
            {
                EditPaymentMethod = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultMultiple<BillingCycle>> QueryBillingCyclesAsync(
            bool doCache = true, bool throwIfError = true)
        {
            BillingCycle seed = new BillingCycle();

            ResultMultiple<BillingCycle> result = await ResultMultipleUI<BillingCycle>.WaitForObjectsAsync(
                true, seed, new CachedHttpRequest<BillingCycle, ResultMultiple<BillingCycle>>(
                    Backend.QueryAsyncListAsync), true);

            if (result.Code == 0)
            {
                BillingCycles = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultMultiple<UserBillingTask>> QueryUserBillingTasksAsync(IDictionary<string, object> filter,
            bool doCache = true, bool throwIfError = true)
        {
            UserBillingTask seed = new UserBillingTask();
            seed.OwnedBy = Backend.Permit.User;

            ResultMultiple<UserBillingTask> result = await ResultMultipleUI<UserBillingTask>.WaitForObjectsAsync(
                true, seed, new CachedHttpRequest<UserBillingTask, ResultMultiple<UserBillingTask>>(
                    Backend.QueryAsyncListAsync), true);

            if (result.Code == 0)
            {
                UserBillingTasks = result.Data.Payload;
            }

            return result;
        }
    }
}
