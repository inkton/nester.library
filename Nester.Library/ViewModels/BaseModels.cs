using Inkton.Nest.Model;

namespace Inkton.Nester.ViewModels
{
    public class BaseViewModels
    {
        private AuthViewModel _authViewModel = null;
        private PaymentViewModel _paymentViewModel = null;
        private AppViewModel _appViewModel = null;
        private AppCollectionViewModel _appCollectionViewModel = null;

        protected bool _wizardMode = false;

        public BaseViewModels(
            AuthViewModel authViewModel = null,
            PaymentViewModel paymentViewModel = null,
            AppViewModel appViewModel = null,
            AppCollectionViewModel appCollectionViewModel = null
            )
        {
            _authViewModel = authViewModel;
            _paymentViewModel = paymentViewModel;
            _appViewModel = appViewModel;
            _appCollectionViewModel = appCollectionViewModel;
        }

        public BaseViewModels(
            BaseViewModels other
            )
        {
            _authViewModel = other._authViewModel;
            _paymentViewModel = other._paymentViewModel;
            _appViewModel = other._appViewModel;
            _appCollectionViewModel = other._appCollectionViewModel;
        }

        public User Owner
        {
            set {
                if (_appViewModel != null && 
                    _appViewModel.EditApp != null)
                {
                    _appViewModel.EditApp.OwnedBy = value;
                }
            }
        }

        public AuthViewModel AuthViewModel
        {
            get { return _authViewModel; }
            set { _authViewModel = value; }
        }

        public PaymentViewModel PaymentViewModel
        {
            get { return _paymentViewModel; }
            set { _paymentViewModel = value; }
        }

        public AppViewModel AppViewModel
        {
            get { return _appViewModel; }
            set { _appViewModel = value; }
        }

        public AppCollectionViewModel AppCollectionViewModel
        {
            get { return _appCollectionViewModel; }
            set { _appCollectionViewModel = value; }
        }

        public bool WizardMode
        {
            get { return _wizardMode; }
            set { _wizardMode = value; }
        }
    }
}
