﻿/*
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
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class ContactViewModel<UserT> : ViewModel<UserT>
        where UserT : User, new()
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

        public ContactViewModel(BackendService<UserT> backend, App app) : base(backend, app)
        {
            _contacts = new ObservableCollection<Contact>();
            _editContact = new Contact();
            _editContact.OwnedBy = app;

            _invitations = new ObservableCollection<Invitation>();
            _editInvitation = new Invitation();

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
                _editInvitation.OwnedBy = value.OwnedBy;

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

        public async Task<ResultMultiple<Invitation>> QueryInvitationsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            ResultMultiple<Invitation> result = await ResultMultipleUI<Invitation>.WaitForObjectsAsync(
                true, _editInvitation, new CachedHttpRequest<Invitation, ResultMultiple<Invitation>>(
                    Backend.QueryAsyncListAsync), true);

            if (result.Code >= 0)
            {
                _invitations = result.Data.Payload;

                if (_invitations.Any())
                {
                    _editInvitation = _invitations.FirstOrDefault();
                }
            }

            return result;
        }

        public async Task<ResultMultiple<Contact>> QueryContactsAsync(
            bool doCache = false, bool throwIfError = true)
        {
            ResultMultiple<Contact> result = await ResultMultipleUI<Contact>.WaitForObjectsAsync(
                true, _editContact, new CachedHttpRequest<Contact, ResultMultiple<Contact>>(
                    Backend.QueryAsyncListAsync), true);

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
                        contact.UserId == Backend.Permit.User.Id)
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

        public async Task<ResultSingle<Contact>> QueryContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            ResultSingle<Contact> result = await ResultSingleUI<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new CachedHttpRequest<Contact, ResultSingle<Contact>>(
                    Backend.QueryAsync), doCache, null, null);

            if (result.Code >= 0)
            {
                _editContact = result.Data.Payload;
                await QueryPermissionsAsync(_editContact, throwIfError);

                if (contact != null)
                    _editContact.CopyTo(contact);
            }

            return result;
        }

        public async Task<ResultSingle<Contact>> UpdateContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            bool leaving = theContact.UserId != null;

            ResultSingle<Contact> result = await ResultSingleUI<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new CachedHttpRequest<Contact, ResultSingle<Contact>>(
                    Backend.UpdateAsync), doCache);

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

        public async Task<ResultSingle<Contact>> CreateContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            ResultSingle<Contact> result = await Cloud.ResultSingleUI<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new CachedHttpRequest<Contact, ResultSingle<Contact>>(
                    Backend.CreateAsync), doCache);

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

        public async Task<ResultSingle<Contact>> RemoveContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            ResultSingle<Contact> result = await ResultSingleUI<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new CachedHttpRequest<Contact, ResultSingle<Contact>>(
                    Backend.RemoveAsync), doCache);

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

        public async Task<ResultSingle<Contact>> ReinviteContactAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;

            return await ResultSingleUI<Contact>.WaitForObjectAsync(
                throwIfError, theContact, new CachedHttpRequest<Contact, ResultSingle<Contact>>(
                    Backend.UpdateAsync), doCache);
        }

        public async Task<ResultMultiple<Permission>> QueryPermissionsAsync(Contact contact = null,
              bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            Permission seedPermission = new Permission();
            seedPermission.OwnedBy = theContact;
            ObservableCollection<Permission> permissions = new ObservableCollection<Permission>();

            ResultMultiple<Permission> result = await ResultMultipleUI<Permission>.WaitForObjectsAsync(
                true, seedPermission, new CachedHttpRequest<Permission, ResultMultiple<Permission>>(
                    Backend.QueryAsyncListAsync), true);

            if (result.Code >= 0)
            {
                permissions = result.Data.Payload;
                OwnerCapabilities caps = new OwnerCapabilities();
                caps.Reset();

                foreach (Permission permission in permissions)
                {
                    switch (permission.AppPermissionTag)
                    {
                        case "create-app": break;
                        case "view-app": caps.CanViewApp = true; break;
                        case "update-app": caps.CanUpdateApp = true; break;
                        case "delete-app": caps.CanDeleteApp = true; break;

                        case "create-nest": caps.CanCreateNest = true; break;
                        case "update-nest": caps.CanUpdateNest = true; break;
                        case "delete-nest": caps.CanDeleteNest = true; break;
                        case "view-nest": caps.CanViewNest = true; break;
                        default:
                            System.Diagnostics.Debugger.Break();
                            break;
                    }
                }

                theContact.OwnerCapabilities = caps;
            }

            return result;
        }

        public async Task<ResultSingle<Permission>> UpdatePermissionAsync(Contact contact = null,
            bool doCache = false, bool throwIfError = true)
        {
            Contact theContact = contact == null ? _editContact : contact;
            Permission seedPermission = new Permission();
            seedPermission.OwnedBy = theContact;

            PermissionSwitch[] switches = {
                new PermissionSwitch(caps => caps.CanViewApp, "view-app"),
                new PermissionSwitch(caps => caps.CanUpdateApp, "update-app"),
                new PermissionSwitch(caps => caps.CanDeleteApp, "delete-app"),
                new PermissionSwitch(caps => caps.CanCreateNest, "create-nest"),
                new PermissionSwitch(caps => caps.CanUpdateNest, "update-nest"),
                new PermissionSwitch(caps => caps.CanDeleteNest, "delete-nest"),
                new PermissionSwitch(caps => caps.CanViewNest, "view-nest")
            };

            ResultSingle<Permission> result = new ResultSingle<Permission>(0);
            Dictionary<string, object> permission = new Dictionary<string, object>();
             
            foreach (PermissionSwitch permSwitch in switches)
            {
                var capable = permSwitch.Getter(theContact.OwnerCapabilities);
                seedPermission.AppPermissionTag = permSwitch.PermissionTag;

                if (capable)
                {
                    permission["app_permission_tag"] = seedPermission.AppPermissionTag;

                    result = await ResultSingleUI<Permission>.WaitForObjectAsync(
                        false, seedPermission, new CachedHttpRequest<Permission, ResultSingle<Permission>>(
                            Backend.CreateAsync), doCache, permission);

                    if (result.Code != ServerStatus.NEST_RESULT_SUCCESS &&
                        result.Code != ServerStatus.NEST_RESULT_ERROR_PERM_FOUND)
                    {
                        break;
                    }

                    permission.Remove("app_permission_tag");
                }
                else
                {
                    result = await ResultSingleUI<Permission>.WaitForObjectAsync(
                        false, seedPermission, new CachedHttpRequest<Permission, ResultSingle<Permission>>(
                            Backend.RemoveAsync), doCache);

                    if (result.Code != ServerStatus.NEST_RESULT_SUCCESS &&
                        result.Code != ServerStatus.NEST_RESULT_ERROR_PERM_NFOUND)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public async Task<ResultSingle<Collaboration>> QueryContactCollaborateAccountAsync(Collaboration collaboration = null,
            bool doCache = false, bool throwIfError = true)
        {
            Collaboration theCollaboration = collaboration == null ? _collaboration : collaboration;
            theCollaboration.AccountId = "0";

            ResultSingle<Collaboration> result = await ResultSingleUI<Collaboration>.WaitForObjectAsync(
                throwIfError, theCollaboration, new CachedHttpRequest<Collaboration, ResultSingle<Collaboration>>(
                    Backend.QueryAsync), doCache, null, null);

            if (result.Code >= 0)
            {
                _collaboration = result.Data.Payload;
            }

            return result;
        }
    }
}
