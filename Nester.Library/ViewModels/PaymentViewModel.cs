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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class PaymentViewModel : ViewModel
    {
        private Credit _editCredit;
        private PaymentMethod _editPaymentMethod;
        private bool _displayPaymentMethodProof;
        private bool _displayPaymentMethodEntry;
        private string _paymentMethodProofDetail;
        private ObservableCollection<BillingCycle> _billingCycles;
        private ObservableCollection<UserBillingTask> _userBillingTasks;  

        public PaymentViewModel(NesterService platform)
            : base(platform)
        {
            _editCredit = new Credit();
            _displayPaymentMethodEntry = false;
            _displayPaymentMethodEntry = false;
            _editPaymentMethod = new PaymentMethod();
            //_editPaymentMethod.OwnedBy = Client.User;
        }

        public Credit EditCredit
        {
            get
            {
                return _editCredit;
            }
        }

        public PaymentMethod EditPaymentMethod
        {
            get
            {
                return _editPaymentMethod;
            }
        }

        public ObservableCollection<BillingCycle> BillingCycles
        {
            get
            {
                return _billingCycles;
            }
        }

        public ObservableCollection<UserBillingTask> UserBillingTasks
        {
            get
            {
                return _userBillingTasks;
            }
        }

        public bool DisplayPaymentMethodProof
        {
            get { return _displayPaymentMethodProof; }
            set { SetProperty(ref _displayPaymentMethodProof, value); }
        }

        public bool DisplayPaymentMethodEntry
        {
            get { return _displayPaymentMethodEntry; }
            set { SetProperty(ref _displayPaymentMethodEntry, value); }
        }

        public string PaymentMethodProofDetail
        {
            get { return _paymentMethodProofDetail; }
            set { SetProperty(ref _paymentMethodProofDetail, value); }
        }

        public async Task InitAsync()
        {
            await QueryPaymentMethodAsync(false, false);
        }

        public async Task<ResultSingle<Credit>> QueryCreditAsync(Credit credit = null,
            bool dCache = false, bool throwIfError = true)
        {
            Credit theCredit = credit == null ? _editCredit : credit;

            ResultSingle<Credit> result = await ResultSingleUI<Credit>.WaitForObjectAsync(
                throwIfError, theCredit, new Cloud.CachedHttpRequest<Credit, ResultSingle<Credit>>(
                    Platform.QueryAsync), dCache, null, null);

            if (result.Code >= 0)
            {
                _editCredit = result.Data.Payload;
            }

            return result;
        }

        public async Task<ResultSingle<PaymentMethod>> QueryPaymentMethodAsync(
            bool dCache = false, bool throwIfError = true)
        {
            ResultSingle<PaymentMethod> result = await ResultSingleUI<PaymentMethod>.WaitForObjectAsync(
                throwIfError, _editPaymentMethod, new Cloud.CachedHttpRequest<PaymentMethod, ResultSingle<PaymentMethod>>(
                    Platform.QueryAsync), dCache, null, null);

            if (result.Code >= 0)
            {                
                _editPaymentMethod = result.Data.Payload;
                DisplayPaymentMethodProof = _editPaymentMethod.IsActive;

                if (DisplayPaymentMethodProof)
                {
                    DisplayPaymentMethodEntry = false;

                    PaymentMethodProofDetail = string.Format("{0}\nLast 4 Digits {1}\nExpiry {2}/{3}",
                                    _editPaymentMethod.Proof.Brand,
                                    _editPaymentMethod.Proof.Last4,
                                    _editPaymentMethod.Proof.ExpMonth, _editPaymentMethod.Proof.ExpYear);
                }
                else
                {
                    DisplayPaymentMethodEntry = true;

                    PaymentMethodProofDetail = "";
                }
            }

            return result;
        }

        public async Task<ResultSingle<PaymentMethod>> CreatePaymentMethodAsync(
            string cardNumber, int expiryMonth, int expiryYear, string cvc, 
            bool doCache = false, bool throwIfError = true)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "cc");
            data.Add("number", cardNumber);
            data.Add("exp_month", expiryMonth.ToString());
            data.Add("exp_year", expiryYear.ToString());
            data.Add("cvc", cvc);

            ResultSingle<PaymentMethod> result = await ResultSingleUI<PaymentMethod>.WaitForObjectAsync(
                throwIfError, _editPaymentMethod, new Cloud.CachedHttpRequest<PaymentMethod, ResultSingle<PaymentMethod>>(
                    Platform.CreateAsync), doCache, data);

            if (result.Code >= 0)
            {
                _editPaymentMethod = result.Data.Payload;
                DisplayPaymentMethodProof = _editPaymentMethod.IsActive;

                if (DisplayPaymentMethodProof)
                {
                    DisplayPaymentMethodEntry = false;

                    PaymentMethodProofDetail = string.Format("{0}\nLast 4 Digits {1}\nExpiry {2}/{3}",
                                    _editPaymentMethod.Proof.Brand,
                                    _editPaymentMethod.Proof.Last4,
                                    _editPaymentMethod.Proof.ExpMonth, _editPaymentMethod.Proof.ExpYear);
                }
                else
                {
                    DisplayPaymentMethodEntry = true;

                    PaymentMethodProofDetail = "";
                }
            }

            return result;
        }

        public async Task<ResultMultiple<BillingCycle>> QueryBillingCyclesAsync(
            bool doCache = true, bool throwIfError = true)
        {
            BillingCycle seed = new BillingCycle();

            ResultMultiple<BillingCycle> result = await ResultMultipleUI<BillingCycle>.WaitForObjectAsync(
                Platform, throwIfError, seed, doCache);

            if (result.Code == 0)
            {
                _billingCycles = result.Data.Payload;
                OnPropertyChanged("BillingCycles");
            }

            return result;
        }

        public async Task<ResultMultiple<UserBillingTask>> QueryUserBillingTasksAsync(IDictionary<string, string> filter,
            bool doCache = true, bool throwIfError = true)
        {
            UserBillingTask seed = new UserBillingTask();
            seed.OwnedBy = Client.User;

            ResultMultiple<UserBillingTask> result = await ResultMultipleUI<UserBillingTask>.WaitForObjectAsync(
                Platform, throwIfError, seed, doCache, filter);

            if (result.Code == 0)
            {
                _userBillingTasks = result.Data.Payload;
                OnPropertyChanged("UserBillingTasks");
            }

            return result;
        }
    }
}
