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
using Inkton.Nest.Model;
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
            _editContact.OwnedBy = app;

            _invitations = new ObservableCollection<Invitation>();
            _editInvitation = new Invitation();
            _editInvitation.OwnedBy = Keeper.User;

            _collaboration = new Collaboration();
            _collaboration.OwnedBy = _editContact;
        }

        override public App EditApp
        {
            get
            {
                return _editApp;
            }
            set
            {
                _editContact.OwnedBy = value; 
                _editInvitation.OwnedBy = Keeper.User;

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
                _collaboration.OwnedBy = value;
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
                _collaboration.OwnedBy = _editContact;
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

        public async Task InitAsync()
        {
            await QueryContactsAsync();
        }

        public async Task<Cloud.ResultMultiple<Invitation>> QueryInvitationsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            Cloud.ResultMultiple<Invitation> result = await Cloud.ResultMultiple<Invitation>.WaitForObjectAsync(
                Keeper.Service, throwIfError, _editInvitation, doCache);

            if (result.Code >= 0)
            {
                _invitations = result.Data.Payload;
            }

            return result;
        }

        public async Task<Cloud.ResultMultiple<Contact>> QueryContactsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            Cloud.ResultMultiple<Contact> result = await Cloud.ResultMultiple<Contact>.WaitForObjectAsync(
                Keeper.Service, throwIfError, _editContact, doCache);

            if (result.Code >= 0)
            {
                _contacts = result.Data.Payload;
                _editApp.OwnerCapabilities = null;

                /* The owner is to be treated differently
                 * The owner contact does not need invitation
                 * for example.
                 */
                foreach (Contact contact in _contacts)
                {
                    contact.OwnedBy = _editContact.OwnedBy;
                    await QueryPermissionsAsync(contact, throwIfError);

                    if (contact.UserId != null &&
                        contact.UserId == Keeper.User.Id)
                    {
                        _ownerContact = contact;
                        _editContact = contact;
                        _collaboration.OwnedBy = contact;
                        _editApp.OwnerCapabilities = contact.OwnerCapabilities;
                    }
                }

                OnPropertyChanged("Contacts");
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Contact>> QueryContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            Cloud.ResultSingle<Contact> result = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact, Cloud.ResultSingle<Contact>>(
                    Keeper.Service.QueryAsync), doCache, null, null);

            if (result.Code >= 0)
            {
                _editContact = result.Data.Payload;
                await QueryPermissionsAsync(_editContact, throwIfError);

                if (contact != null)
                    _editContact.CopyTo(contact);
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Contact>> UpdateContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            bool leaving = theContact.UserId != null;

            Cloud.ResultSingle<Contact> result = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact, Cloud.ResultSingle<Contact>>(
                    Keeper.Service.UpdateAsync), doCache);

            if (result.Code >= 0)
            {
                _editContact = result.Data.Payload;

                if (!leaving)
                {
                    await QueryPermissionsAsync(_editContact, throwIfError);
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Contact>> CreateContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            Cloud.ResultSingle<Contact> result = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact, Cloud.ResultSingle<Contact>>(
                    Keeper.Service.CreateAsync), doCache);

            if (result.Code >= 0)
            {
                EditContact = result.Data.Payload;
                await QueryPermissionsAsync(_editContact, throwIfError);

                if (contact != null)
                {
                    _editContact.CopyTo(contact);
                    _contacts.Add(contact);
                    OnPropertyChanged("Contacts");
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Contact>> RemoveContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            Cloud.ResultSingle<Contact> result = await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact, Cloud.ResultSingle<Contact>>(
                    Keeper.Service.RemoveAsync), doCache);

            if (result.Code >= 0)
            {
                if (contact != null)
                {
                    _contacts.Remove(contact);
                    OnPropertyChanged("Contacts");
                }                
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Contact>> ReinviteContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            return await Cloud.ResultSingle<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new Cloud.CachedHttpRequest<Contact, Cloud.ResultSingle<Contact>>(
                    Keeper.Service.UpdateAsync), doCache);
        }

        public async Task<Cloud.ResultMultiple<Permission>> QueryPermissionsAsync(Contact contact = null,
              bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            Permission seedPermission = new Permission();
            seedPermission.OwnedBy = theContact;
            ObservableCollection<Permission> permissions = new ObservableCollection<Permission>();

            Cloud.ResultMultiple<Permission> result = await Cloud.ResultMultiple<Permission>.WaitForObjectAsync(
                Keeper.Service, throwIfError, seedPermission, doCache);

            if (result.Code >= 0)
            {
                permissions = result.Data.Payload;
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

            return result;
        }

        public async Task<Cloud.ResultSingle<Permission>> UpdatePermissionAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            Permission seedPermission = new Permission();
            seedPermission.OwnedBy = theContact;

            PermissionSwitch[] switches = new PermissionSwitch[] {
                new PermissionSwitch(caps => caps.CanViewApp, "view-app"),
                new PermissionSwitch(caps => caps.CanUpdateApp, "update-app"),
                new PermissionSwitch(caps => caps.CanDeleteApp, "delete-app"),
                new PermissionSwitch(caps => caps.CanCreateNest, "create-nest"),
                new PermissionSwitch(caps => caps.CanUpdateNest, "update-nest"),
                new PermissionSwitch(caps => caps.CanDeleteNest, "delete-nest"),
                new PermissionSwitch(caps => caps.CanViewNest, "view-nest")
            };

            Cloud.ResultSingle<Permission> result = new Cloud.ResultSingle<Permission>(0);
            Dictionary<string, string> permission = new Dictionary<string, string>();
             
            foreach (PermissionSwitch permSwitch in switches)
            {
                var capable = permSwitch.Getter(theContact.OwnerCapabilities);
                seedPermission.AppPermissionTag = permSwitch.PermissionTag;

                if (capable)
                {
                    permission["app_permission_tag"] = seedPermission.AppPermissionTag;

                    result = await Cloud.ResultSingle<Permission>.WaitForObjectAsync(
                        false, seedPermission, new Cloud.CachedHttpRequest<Permission, Cloud.ResultSingle<Permission>>(
                            Keeper.Service.CreateAsync), doCache, permission);

                    if (result.Code != Cloud.ServerStatus.NEST_RESULT_SUCCESS &&
                        result.Code != Cloud.ServerStatus.NEST_RESULT_ERROR_PERM_FOUND)
                    {
                        break;
                    }

                    permission.Remove("app_permission_tag");
                }
                else
                {
                    result = await Cloud.ResultSingle<Permission>.WaitForObjectAsync(
                        false, seedPermission, new Cloud.CachedHttpRequest<Permission, Cloud.ResultSingle<Permission>>(
                            Keeper.Service.RemoveAsync), doCache);

                    if (result.Code != Cloud.ServerStatus.NEST_RESULT_SUCCESS &&
                        result.Code != Cloud.ServerStatus.NEST_RESULT_ERROR_PERM_NFOUND)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public async Task<Cloud.ResultSingle<Collaboration>> QueryContactCollaborateAccountAsync(Collaboration collaboration = null,
            bool doCache = false, bool throwIfError = true)
        {
            Collaboration theCollaboration = collaboration == null ? _collaboration : collaboration;
            theCollaboration.AccountId = "0";

            Cloud.ResultSingle<Collaboration> result = await Cloud.ResultSingle<Collaboration>.WaitForObjectAsync(
                false, theCollaboration, new Cloud.CachedHttpRequest<Collaboration, Cloud.ResultSingle<Collaboration>>(
                    Keeper.Service.QueryAsync), doCache, null, null);

            if (result.Code >= 0)
            {
                _collaboration = result.Data.Payload;
            }

            return result;
        }
    }
}
