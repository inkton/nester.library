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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Inkton.Nester.Models;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class ContactViewModel : ViewModel
    {
        private Contact _editContact;
        private Contact _ownerContact;
        private Collaboration _collaboration;

        private ObservableCollection<Contact> _contacts;

        private Invitation _editInvitation;

        private ObservableCollection<Invitation> _invitations;

        struct PermissionSwitch
        {
            public PermissionSwitch(Expression<Func<OwnerCapabilities, bool>> check, string permissionTag)
            {
                Check = check;
                PermissionTag = permissionTag;
            }

            public Func<OwnerCapabilities, bool> Getter
            {
                get
                {
                    return Check.Compile();
                }
            }

            public Expression<Func<OwnerCapabilities, bool>> Check;
            public string PermissionTag;
        }

        public ContactViewModel(App app) : base(app)
        {
            _contacts = new ObservableCollection<Contact>();
            _editContact = new Contact();
            _editContact.App = app;

            _invitations = new ObservableCollection<Invitation>();
            _editInvitation = new Invitation();
            _editInvitation.User = Keeper.User;

            _collaboration = new Collaboration();
            _collaboration.Contact = _editContact;
        }

        override public App EditApp
        {
            get
            {
                return _editApp;
            }
            set
            {
                _editContact.App = value; 
                _editInvitation.User = Keeper.User;

                SetProperty(ref _editApp, value);
            }
        }

        public Contact EditContact    
        {
            get
            {
                return _editContact;
            }
            set
            {
                SetProperty(ref _editContact, value);
                _collaboration.Contact = value;
                OnPropertyChanged("EditContact.OwnerCapabilities");
            }
        }

        public Invitation EditInvitation
        {
            get
            {
                return _editInvitation;
            }
            set
            {
                SetProperty(ref _editInvitation, value);
            }
        }

        public Contact OwnerContact
        {
            get
            {
                return _ownerContact;
            }
            set
            {
                SetProperty(ref _ownerContact, value);
            }
        }

        public Collaboration Collaboration
        {
            get
            {
                return _collaboration;
            }
            set
            {
                _collaboration.Contact = _editContact;
                SetProperty(ref _collaboration, value);
            }
        }

        public int ContactsCount
        {
            get
            {
                return _contacts.Count();
            }
        }

        public ObservableCollection<Contact> Contacts
        {
            get
            {
                return _contacts;
            }
            set
            {
                SetProperty(ref _contacts, value);
            }
        }

        public ObservableCollection<Invitation> Invitations
        {
            get
            {
                return _invitations;
            }
            set
            {
                SetProperty(ref _invitations, value);
            }
        }

        override public async Task<Cloud.ServerStatus> InitAsync()
        {
            Cloud.ServerStatus status;

            status = await QueryContactsAsync();
            if (status.Code < 0)
            {
                return status;
            }
            
            return status;
        }

        public async Task<Cloud.ServerStatus> QueryInvitationsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            Cloud.ServerStatus status = await Cloud.ResultMultiple<Invitation>.WaitForObjectAsync(
                Keeper.Service, throwIfError, _editInvitation, doCache);

            if (status.Code >= 0)
            {
                _invitations = status.PayloadToList<Invitation>();
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryContactsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            Cloud.ServerStatus status = await Cloud.ResultMultiple<Contact>.WaitForObjectAsync(
                Keeper.Service, throwIfError, _editContact, doCache);

            if (status.Code >= 0)
            {
                _contacts = status.PayloadToList<Contact>();
                _editApp.OwnerCapabilities = null;

                /* The owner is to be treated differently
                 * The owner contact does not need invitation
                 * for example.
                 */
                foreach (Contact contact in _contacts)
                {
                    contact.App = _editContact.App;
                    await QueryPermissionsAsync(contact, throwIfError);

                    if (contact.UserId != null &&
                        contact.UserId == Keeper.User.Id)
                    {
                        _ownerContact = contact;
                        _editContact = contact;
                        _collaboration.Contact = contact;
                        _editApp.OwnerCapabilities = contact.OwnerCapabilities;
                    }
                }

                OnPropertyChanged("Contacts");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact>(
                    Keeper.Service.QueryAsync), doCache, null, null);

            if (status.Code >= 0)
            {
                _editContact = status.PayloadToObject<Contact>();
                await QueryPermissionsAsync(_editContact, throwIfError);

                if (contact != null)
                    Cloud.Object.CopyPropertiesTo(_editContact, contact);
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdateContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            bool leaving = theContact.UserId != null;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact>(
                    Keeper.Service.UpdateAsync), doCache);

            if (status.Code >= 0)
            {
                _editContact = status.PayloadToObject<Contact>();

                if (!leaving)
                {
                    await QueryPermissionsAsync(_editContact, throwIfError);
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> CreateContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact>(
                    Keeper.Service.CreateAsync), doCache);

            if (status.Code >= 0)
            {
                EditContact = status.PayloadToObject<Contact>();
                await QueryPermissionsAsync(_editContact, throwIfError);

                if (contact != null)
                {
                    Cloud.Object.CopyPropertiesTo(_editContact, contact);
                    _contacts.Add(contact);
                    OnPropertyChanged("Contacts");
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> RemoveContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact>(
                    Keeper.Service.RemoveAsync), doCache);

            if (status.Code >= 0)
            {
                if (contact != null)
                {
                    _contacts.Remove(contact);
                    OnPropertyChanged("Contacts");
                }                
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> ReinviteContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            Cloud.ServerStatus status = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact>(
                    Keeper.Service.UpdateAsync), doCache);

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryPermissionsAsync(Contact contact = null,
              bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            Permission seedPermission = new Permission();
            seedPermission.Contact = theContact;
            ObservableCollection<Permission> permissions = new ObservableCollection<Permission>();

            Cloud.ServerStatus status = await Cloud.ResultMultiple<Permission>.WaitForObjectAsync(
                Keeper.Service, throwIfError, seedPermission, doCache);

            if (status.Code >= 0)
            {
                permissions = status.PayloadToList<Permission>();
                OwnerCapabilities caps = new OwnerCapabilities();
                caps.Reset();

                foreach (Permission permission in permissions)
                {
                    switch (permission.AppPermissionTag)
                    {
                        case "view-app": caps.CanViewApp = true; break;
                        case "update-app": caps.CanUpdateApp = true; break;
                        case "delete-app": caps.CanDeleteApp = true; break;

                        case "create-nest": caps.CanCreateNest = true; break;
                        case "update-nest": caps.CanUpdateNest = true; break;
                        case "delete-nest": caps.CanDeleteNest = true; break;
                        case "view-nest": caps.CanViewNest = true; break;
                    }
                }

                theContact.OwnerCapabilities = caps;
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> UpdatePermissionAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            Permission seedPermission = new Permission();
            seedPermission.Contact = theContact;

            PermissionSwitch[] switches = new PermissionSwitch[] {
                new PermissionSwitch(caps => caps.CanViewApp, "view-app"),
                new PermissionSwitch(caps => caps.CanUpdateApp, "update-app"),
                new PermissionSwitch(caps => caps.CanDeleteApp, "delete-app"),
                new PermissionSwitch(caps => caps.CanCreateNest, "create-nest"),
                new PermissionSwitch(caps => caps.CanUpdateNest, "update-nest"),
                new PermissionSwitch(caps => caps.CanDeleteNest, "delete-nest"),
                new PermissionSwitch(caps => caps.CanViewNest, "view-nest")
            };

            Cloud.ServerStatus status = new Cloud.ServerStatus(0);
            Dictionary<string, string> permission = new Dictionary<string, string>();
             
            foreach (PermissionSwitch permSwitch in switches)
            {
                var capable = permSwitch.Getter(theContact.OwnerCapabilities);
                seedPermission.AppPermissionTag = permSwitch.PermissionTag;

                if (capable)
                {
                    permission["app_permission_tag"] = seedPermission.AppPermissionTag;

                    status = await Cloud.ResultSingle<Permission>.WaitForObjectAsync(
                        false, seedPermission, new Cloud.CachedHttpRequest<Permission>(
                            Keeper.Service.CreateAsync), doCache, permission);

                    if (status.Code != Cloud.ServerStatus.NEST_RESULT_SUCCESS &&
                        status.Code != Cloud.ServerStatus.NEST_RESULT_ERROR_PERM_FOUND)
                    {
                        break;
                    }

                    permission.Remove("app_permission_tag");
                }
                else
                {
                    status = await Cloud.ResultSingle<Permission>.WaitForObjectAsync(
                        false, seedPermission, new Cloud.CachedHttpRequest<Permission>(
                            Keeper.Service.RemoveAsync), doCache);

                    if (status.Code != Cloud.ServerStatus.NEST_RESULT_SUCCESS &&
                        status.Code != Cloud.ServerStatus.NEST_RESULT_ERROR_PERM_NFOUND)
                    {
                        break;
                    }
                }
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryContactCollaborateAccountAsync(Collaboration collaboration = null,
            bool doCache = false, bool throwIfError = true)
        {
            Collaboration theCollaboration = collaboration == null ? _collaboration : collaboration;
            theCollaboration.AccountId = "0";

            Cloud.ServerStatus status = await Cloud.ResultSingle<Collaboration>.WaitForObjectAsync(
                false, theCollaboration, new Cloud.CachedHttpRequest<Collaboration>(
                    Keeper.Service.QueryAsync), doCache, null, null);

            if (status.Code >= 0)
            {
                _collaboration = status.PayloadToObject<Collaboration>();
            }

            return status;
        }
    }
}
