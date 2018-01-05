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

using System.Net;
using System.Collections.ObjectModel;
using System.Resources;
using Xamarin.Forms;
using Inkton.Nester.ViewModels;
using System;

namespace Inkton.Nester.Cloud
{
    public struct ServerStatus
    {
        public const int NEST_RESULT_SUCCESS = 0;
        public const int NEST_RESULT_WARNING = 100;
        public const int NEST_RESULT_WARNING_UPDATING = 102;
        public const int NEST_RESULT_ERROR = -200;
        public const int NEST_RESULT_ERROR_FATAL = -202;
        public const int NEST_RESULT_ERROR_NAUTH = -204;
        public const int NEST_RESULT_ERROR_FIELDS = -206;
        public const int NEST_RESULT_ERROR_USER_NFOUND = -208;
        public const int NEST_RESULT_ERROR_APP_NFOUND = -210;
        public const int NEST_RESULT_ERROR_APP_TAG_EXIST = -212;
        public const int NEST_RESULT_ERROR_APP_EXISTS = -214;
        public const int NEST_RESULT_ERROR_USER_NAUTH = -216;
        public const int NEST_RESULT_ERROR_LOGIN_FAILED = -218;
        public const int NEST_RESULT_ERROR_PMETHOD_NFOUND = -220;
        public const int NEST_RESULT_ERROR_CONTACT_NFOUND = -222;
        public const int NEST_RESULT_ERROR_INVITATION_NFOUND = -224;
        public const int NEST_RESULT_ERROR_SERVICE_NFOUND = -226;
        public const int NEST_RESULT_ERROR_SERVICE_TYPE_EXISTS = -228;
        public const int NEST_RESULT_ERROR_APP_SERVICE_TIER_NFOUND = -230;
        public const int NEST_RESULT_ERROR_APP_SERVICE_TIER_EXIST = -232;
        public const int NEST_RESULT_ERROR_APP_SERVICE_FTIER_EXIST = -234;
        // Cannot cancel because serivice is essential                
        public const int NEST_RESULT_ERROR_APP_SERVICE_ESSENTIAL = -236;// Cann
        public const int NEST_RESULT_ERROR_APP_SERVICE_ALWAYS_ON = -238;
        public const int NEST_RESULT_ERROR_ADD_PAYMENT_METHOD = -240;
        public const int NEST_RESULT_ERROR_SUBSCRIPTION_NFOUND = -242;
        public const int NEST_RESULT_ERROR_SUBSCRIPTION_INVALID = -244;
        public const int NEST_RESULT_ERROR_FOREST_NOT_PLANTABLE = -246;
        public const int NEST_RESULT_ERROR_FOREST_NOT_FOUND = -248;
        // The project must have an app service before adding tree 
        public const int NEST_RESULT_ERROR_FOREST_APP_NFOUND = -250;
        // Must assign the project to tree before adding tree servi
        public const int NEST_RESULT_ERROR_APP_ASSIGN_TREE = -252;
        // Do not assign a tree now. Cannot assign a tree before su
        public const int NEST_RESULT_ERROR_APP_SERVICE_TIER_NEEDED = -254;
        // Project must be deployed to assign app services         
        public const int NEST_RESULT_ERROR_APP_NDEPLOYED = -256;
        // app cannot be deployed                                  
        public const int NEST_RESULT_ERROR_APP_DEPLOYED = -258;
        // The app cannot be changed after deployment              
        public const int NEST_RESULT_ERROR_APP_NUPDATES = -260;
        // Not a shared service tier                               
        public const int NEST_RESULT_ERROR_APP_SERVICE_TIER_NSHARED = -262;
        // Not a dedicated service tier                            
        public const int NEST_RESULT_ERROR_APP_SERVICE_TIER_NDEDICATED = -26;
        // A worker must specify the dependent handler.            
        public const int NEST_RESULT_ERROR_APP_HANDLER_NEEDED = -266;
        // Invalid service level.                                  
        public const int NEST_RESULT_ERROR_INVALID_SERVICE_LEVEL = -268;
        // Nest not found.                                         
        public const int NEST_RESULT_ERROR_NEST_NFOUND = -270;
        // Forest invalid for the  app tier                        
        public const int NEST_RESULT_ERROR_FOREST_INVALID = -272;
        // web handler nest already exist                          
        public const int NEST_RESULT_ERROR_NEST_HANDLER_EXIST = -274;
        // Domain not registered                                   
        public const int NEST_RESULT_ERROR_DOMAIN_UNREGISTERED = -276;
        // Domain not found                                        
        public const int NEST_RESULT_ERROR_DOMAIN_NFOUND = -278;
        // Domain ssl not found                                    
        public const int NEST_RESULT_ERROR_DOMAIN_CERT_NFOUND = -280;
        // Domain only one certificate can be assigned
        public const int NEST_RESULT_ERROR_DOMAIN_CERT_ASSIGNED = -282;
        // App not ready for the operation
        public const int NEST_RESULT_ERROR_APP_NREADY = -284;
        public const int NEST_RESULT_ERROR_SUBSCRIPTION_STATE = -286;
        public const int NEST_RESULT_ERROR_TREE_NFOUND = -288;
        public const int NEST_RESULT_NEST_HANDLER_NFOUND = -290;
        public const int NEST_RESULT_DOMAIN_DEFAULT = -292;
        public const int NEST_RESULT_DOMAIN_ALIAS_MALFORMED = -294;
        public const int NEST_RESULT_ERROR_DEPLOYMENT_NFOUND = -296;
        public const int NEST_RESULT_ERROR_DEPLOYMENT_EXIST = -298;
        public const int NEST_RESULT_ERROR_DEPLOYMENT_WRONGID = -300;
        // Errors produced locally whilst handing queries
        public const int NEST_RESULT_ERROR_LOCAL = -999;
        public const int NEST_RESULT_ERROR_PAYMENT_DECLINED = -302;
        public const int NEST_RESULT_ERROR_PAYMENT_BUSY = -304;
        public const int NEST_RESULT_ERROR_PERM_NFOUND = -306;
        public const int NEST_RESULT_ERROR_PERM_FOUND = -308;
        public const int NEST_RESULT_ERROR_NPLATFORM_NFOUND = -310;
        public const int NEST_RESULT_ERROR_AUTH_SECCODE = -312;
        public const int NEST_RESULT_ERROR_CONTACT_EXCEEDED = -320;

        private int _code;
        private string _description;
        private string _notes;
        private HttpStatusCode _httpStatus;
        private object _payload;

        public ServerStatus(int code = -999)
        {
            _code = code;
            _description = "unknown";
            _notes = "none";
            _httpStatus = HttpStatusCode.NotFound;
            _payload = null;
        }

        public void Throw()
        {
            string message = "Failed to connect to Nest server - Please check Internet connectiviy";

            if (Code != NEST_RESULT_ERROR_LOCAL)
            {
                if (Description != null && Description.Length > 0)
                {
                    message = LocalDescription;
                }
                if (Notes != null && Notes.Length > 0)
                {
                    message += " - " + Notes;
                }
            }

            throw new Exception(message);
        }

        public static ServerStatus FromServerResult<ResultBase, T>(ResultBase<T> result)
        {
            ServerStatus status = new ServerStatus();
            status.Code = result.ResultCode;
            status.Description = result.ResultText;
            status.Notes = result.Notes;

            if (result.Data != null)
            {
                status.Payload = result.Data.Payload;
            }

            return status;
        }

        public T PayloadToObject<T>() where T : ManagedEntity
        {
            return _payload as T;
        }

        public ObservableCollection<T> PayloadToList<T>() where T : ManagedEntity
        {
            return _payload as ObservableCollection<T>;
        }

        public int Code
        {
            get { return _code; }
            set { _code = value; }
        }

        public HttpStatusCode HttpStatus
        {
            get { return _httpStatus; }
            set { _httpStatus = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string LocalDescription
        {
            get
            {
                ResourceManager resmgr = (Application.Current as INesterControl).GetResourceManager();
                return resmgr.GetString(_description,
                    System.Globalization.CultureInfo.CurrentUICulture);
            }
        }

        public string Notes
        {
            get { return _notes; }
            set { _notes = value; }
        }

        public object Payload
        {
            get { return _payload; }
            set { _payload = value; }
        }
    }
}
