
namespace Inkton.Nester.ViewModels
{
    public class BaseModels
    {
        private AuthViewModel _authViewModel = null;
        private PaymentViewModel _paymentViewModel = null;
        private AppViewModel _targetViewModel = null;
        private AppCollectionViewModel _allApps = null;

        protected bool _wizardMode = false;

        public BaseModels(
            AuthViewModel authViewModel = null,
            PaymentViewModel paymentViewModel = null,
            AppViewModel targetViewModel = null,
            AppCollectionViewModel allApps = null
            )
        {
            _authViewModel = authViewModel;
            _paymentViewModel = paymentViewModel;
            _targetViewModel = targetViewModel;
            _allApps = allApps;
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

        public AppViewModel TargetViewModel
        {
            get { return _targetViewModel; }
            set { _targetViewModel = value; }
        }

        public AppCollectionViewModel AllApps
        {
            get { return _allApps; }
            set { _allApps = value; }
        }

        public bool WizardMode
        {
            get { return _wizardMode; }
            set { _wizardMode = value; }
        }
    }
}
