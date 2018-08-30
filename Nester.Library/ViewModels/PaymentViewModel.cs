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
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class PaymentViewModel : ViewModel
    {
        private Credit _editCredit;
        private PaymentMethod _editPaymentMethod;
        private bool _displayPaymentMethodProof = false;
        private bool _displayPaymentMethodEntry = true;
        private string _paymentMethodProofDetail;
        private ObservableCollection<BillingCycle> _billingCycles;
        private ObservableCollection<UserBillingTask> _userBillingTasks;  

        public PaymentViewModel()
        {
            _editCredit = new Credit();

            _editPaymentMethod = new PaymentMethod();
            _editPaymentMethod.Owner = Keeper.User;
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

        override public async Task<Cloud.ServerStatus> InitAsync()
        {
            return await QueryPaymentMethodAsync(false, false);
        }

        public async Task<Cloud.ServerStatus> QueryCreditAsync(Credit credit = null,
            bool dCache = false, bool throwIfError = true)
        {
            Credit theCredit = credit == null ? _editCredit : credit;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Credit>.WaitForObjectAsync(
                throwIfError, theCredit, new Cloud.CachedHttpRequest<Credit>(
                    Keeper.Service.QueryAsync), dCache, null, null);

            if (status.Code >= 0)
            {
                _editCredit = status.PayloadToObject<Credit>();
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryPaymentMethodAsync(
            bool dCache = false, bool throwIfError = true)
        {
            Cloud.ServerStatus status = await Cloud.ResultSingle<PaymentMethod>.WaitForObjectAsync(
                throwIfError, _editPaymentMethod, new Cloud.CachedHttpRequest<PaymentMethod>(
                    Keeper.Service.QueryAsync), dCache, null, null);

            if (status.Code >= 0)
            {                
                _editPaymentMethod = status.PayloadToObject<PaymentMethod>();
                DisplayPaymentMethodProof = _editPaymentMethod.Proof != null;

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

            return status;
        }

        public async Task<Cloud.ServerStatus> CreatePaymentMethodAsync(
            string cardNumber, int expiryMonth, int expiryYear, string cvc, 
            bool doCache = false, bool throwIfError = true)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "cc");
            data.Add("number", cardNumber);
            data.Add("exp_month", expiryMonth.ToString());
            data.Add("exp_year", expiryYear.ToString());
            data.Add("cvc", cvc);

            Cloud.ServerStatus status = await Cloud.ResultSingle<PaymentMethod>.WaitForObjectAsync(
                throwIfError, _editPaymentMethod, new Cloud.CachedHttpRequest<PaymentMethod>(
                    Keeper.Service.CreateAsync), doCache, data);

            if (status.Code >= 0)
            {
                _editPaymentMethod = status.PayloadToObject<PaymentMethod>();
                DisplayPaymentMethodProof = _editPaymentMethod.Proof != null;

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

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryBillingCyclesAsync(
            bool doCache = true, bool throwIfError = true)
        {
            BillingCycle seed = new BillingCycle();

            Cloud.ServerStatus status = await Cloud.ResultMultiple<BillingCycle>.WaitForObjectAsync(
                Keeper.Service, throwIfError, seed, doCache);

            if (status.Code == 0)
            {
                _billingCycles = status.PayloadToList<BillingCycle>();
                OnPropertyChanged("BillingCycles");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryUserBillingTasksAsync(IDictionary<string, string> filter,
            bool doCache = true, bool throwIfError = true)
        {
            UserBillingTask seed = new UserBillingTask();
            seed.Owner = Keeper.User;
            
            Cloud.ServerStatus status = await Cloud.ResultMultiple<UserBillingTask>.WaitForObjectAsync(
                Keeper.Service, throwIfError, seed, doCache, filter);

            if (status.Code == 0)
            {
                _userBillingTasks = status.PayloadToList<UserBillingTask>();
                OnPropertyChanged("UserBillingTasks");
            }

            return status;
        }

        public string PaymentNotice
        {
            get
            {
                if (Keeper.User.TerritoryISOCode == "AU")
                {
                    return "The prices are in US Dollars and do not include GST.";
                }
                else
                {
                    return "The prices are in US Dollars. ";
                }
            }
        }
    }
}
