/*
    Copyright (c) 2017 Inkton.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the Software
    is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission ServerStatusnotice shall be included in
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
using System.Text;
using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Storage;
using System.Reflection;

namespace Inkton.Nester.Cloud
{
    public struct BasicAuth
    {
        public BasicAuth(bool enabled = false,
            string username = null,
            string password = null)
        {
            Enabled = enabled;
            Username = username;
            Password = password;
        }

        public bool Enabled;
        public string Username;
        public string Password;
    }

    public delegate Task<ResultT> HttpRequest<PayloadT, ResultT>(
        PayloadT seed, IFlurlRequest flurlRequestl) where PayloadT : ICloudObject, new();
    public delegate Task<ResultT> CachedHttpRequest<PayloadT, ResultT>(PayloadT seed,
        IDictionary<string, string> data, string subPath = null, bool doCache = true) where PayloadT : ICloudObject, new();

    public interface INesterService
    {
        int Version { get; set; }
        string DeviceSignature { get; set; }
        string Endpoint { get; set; }
        BasicAuth BasicAuth { get; set; }
        Permit Permit { get; set; }

        Task<ResultSingle<Permit>> SignupAsync();
        Task<ResultSingle<Permit>> RecoverPasswordAsync();
        Task<ResultSingle<Permit>> QueryTokenAsync();

        Task<ResultSingle<T>> CreateAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new();
        Task<ResultSingle<T>> QueryAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new();
        Task<ResultMultiple<T>> QueryAsyncListAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new();
        Task<ResultSingle<T>> UpdateAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new();
        Task<ResultSingle<T>> RemoveAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = false) where T : Inkton.Nest.Cloud.ICloudObject, new();
    }

    public interface INesterServiceNotify
    {
        void BeginQuery();
        bool CanProgress(int attempt);
        void Waiting(int seconds);
        void EndQuery();
    }

    public class PropertyRenameAndIgnoreSerializerContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<Type, HashSet<string>> _ignores;
        private readonly Dictionary<Type, Dictionary<string, string>> _renames;

        public PropertyRenameAndIgnoreSerializerContractResolver()
        {
            _ignores = new Dictionary<Type, HashSet<string>>();
            _renames = new Dictionary<Type, Dictionary<string, string>>();
        }

        public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
        {
            if (!_ignores.ContainsKey(type))
                _ignores[type] = new HashSet<string>();

            foreach (var prop in jsonPropertyNames)
                _ignores[type].Add(prop);
        }

        public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
        {
            if (!_renames.ContainsKey(type))
                _renames[type] = new Dictionary<string, string>();

            _renames[type][propertyName] = newJsonPropertyName;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (IsIgnored(property.DeclaringType, property.PropertyName))
            {
                property.ShouldSerialize = i => false;
                property.Ignored = true;
            }

            if (IsRenamed(property.DeclaringType, property.PropertyName, out var newJsonPropertyName))
                property.PropertyName = newJsonPropertyName;

            return property;
        }

        private bool IsIgnored(Type type, string jsonPropertyName)
        {
            if (!_ignores.ContainsKey(type))
                return false;

            return _ignores[type].Contains(jsonPropertyName);
        }

        private bool IsRenamed(Type type, string jsonPropertyName, out string newJsonPropertyName)
        {
            Dictionary<string, string> renames;

            if (!_renames.TryGetValue(type, out renames) || !renames.TryGetValue(jsonPropertyName, out newJsonPropertyName))
            {
                newJsonPropertyName = null;
                return false;
            }

            return true;
        }
    }

    public class NesterService : INesterService
    {
        private int _version = 1;
        private string _deviceSignature;
        private string _endpoint;
        private BasicAuth _basicAuth;
        private Permit _permit = new Permit();
        private bool _autoTokenRenew = true;
        private int _retryCount = 3;
        private int _retryBaseIntervalInSecs = 2;
        private StorageService _cache;
        private INesterServiceNotify _notifier;
        //private JsonSerializerSettings _serializerSettings;

        public NesterService(
            int version, string deviceSignature, StorageService cache)
        {
            _version = version;
            _cache = cache;
            _deviceSignature = deviceSignature;
            _endpoint = "https://api.nest.yt/";

            //AddCustomRules();
        }

        public int Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public string DeviceSignature
        {
            get { return _deviceSignature; }
            set { _deviceSignature = value; }
        }

        public string Endpoint
        {
            get { return _endpoint; }
            set { _endpoint = value; }
        }

        public BasicAuth BasicAuth
        {
            get { return _basicAuth; }
            set { _basicAuth = value; }
        }   

        public Permit Permit
        {
            get { return _permit; }
            set { _permit = value; }
        }

        public bool AutoTokenRenew
        {
            get { return _autoTokenRenew; }
            set { _autoTokenRenew = value; }
        }

        public int RetryCount
        {
            get { return _retryCount; }
            set { _retryCount = value; }
        }

        public int RetryBaseIntervalInSecs
        {
            get { return _retryBaseIntervalInSecs; }
            set { _retryBaseIntervalInSecs = value; }
        }

        public INesterServiceNotify Notifier
        {
            get { return _notifier; }
            set { _notifier = value; }
        }

        //public JsonSerializerSettings SerializerSettings
        //{
        //    get { return _serializerSettings; }
        //    set { _serializerSettings = value; }
        //}

        public async Task<ResultSingle<Permit>> SignupAsync()
        {
            return await PostAsync(_permit, 
                CreateRequest(_permit, false));
        }

        public async Task<ResultSingle<Permit>> RecoverPasswordAsync()
        {
             ResultSingle<Permit> result = await PutAsync(_permit, 
                CreateRequest(_permit, true));

            return result;
        }

        public async Task<ResultSingle<Permit>> QueryTokenAsync()
        {
            var data = new Dictionary<string, string>();
            data["password"] = _permit.Password;

            ResultSingle<Permit> result = await GetAsync(
                _permit, CreateRequest(
                    _permit, true, data));

            return result;
        }

        public async Task<ResultSingle<Permit>> ResetTokenAsync()
        {
            return await TrySend<Permit, ResultSingle<Permit>, Permit>(
                new HttpRequest<Permit, ResultSingle<Permit>>(DeleteAsync),
                _permit, true, null, null, false);
        }

        #region Utility

/*
        private void AddCustomRules()
        {
            var jsonResolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "Id", "id");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "Email", "email");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "NormalizedEmail", "normalized_email");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "UserName", "username");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "NormalizedUserName", "normalized_username");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "EmailConfirmed", "email_confirmed");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "PasswordHash", "password_hash");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "SecurityStamp", "security_stamp");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "ConcurrencyStamp", "concurrency_stamp");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "PhoneNumber", "phonenumber");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "PhoneNumberConfirmed", "phonenumber_confirmed");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "TwoFactorEnabled", "two_factor_enabled");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "LockoutEnd", "lockout_end");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "LockoutEnabled", "lockout_enabled");
            jsonResolver.RenameProperty(typeof(Microsoft.AspNetCore.Identity.IdentityUser<int>), "AccessFailedCount", "access_failed_count");

            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = jsonResolver
            };
            FlurlHttp.Configure(settings => {
                settings.JsonSerializer = new NewtonsoftJsonSerializer(_serializerSettings);
            });
        }   
        */

        private void SetFailedResult<T>(
            Result<T> result, Exception ex)
        {
            if (ex is FlurlHttpException)
            {
                FlurlHttpException httpEx = ex as FlurlHttpException;
                string notes = "Failed to connect with " + _endpoint + "\n";
                result.Text = "NEST_RESULT_HTTP_ERROR";

                if (httpEx.Call.Response != null)
                {
                    result.HttpStatus = httpEx.Call.Response.StatusCode;

                    switch (result.HttpStatus)
                    {
                        case System.Net.HttpStatusCode.BadRequest:
                            result.Text = "NEST_RESULT_HTTP_400"; break;
                        case System.Net.HttpStatusCode.Unauthorized:
                            result.Text = "NEST_RESULT_HTTP_401"; break;
                        case System.Net.HttpStatusCode.Forbidden:
                            result.Text = "NEST_RESULT_HTTP_403"; break;
                        default:
                            result.Text = "NEST_RESULT_HTTP_ERROR";
                            result.Notes += "Http error " + result.HttpStatus.ToString() + "\n";
                            break;
                    }

                    var inner = httpEx.InnerException;
                    if (inner != null)
                    {
                        notes += $"Reason A : {inner.Message}";
                        if (inner.InnerException != null)
                        {
                            notes += $"Reason B : {inner.InnerException.Message}";
                        }
                    }
                }

                result.Notes = notes;
            }
            else
            {
                result.Text = "NEST_RESULT_ERROR";
                result.Notes = ex.Message;
            }
        }

        private IFlurlRequest CreateRequest<T>(T seed, bool keyRequest,
            IDictionary<string, string> data = null, string subPath = null) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            string fullUrl = Endpoint;

            if (keyRequest)
            {
                fullUrl += seed.CollectionKey;
            }
            else
            {
                fullUrl += seed.CollectionPath;
            }

            if (subPath != null)
            {
                fullUrl = fullUrl + subPath;
            }

            IFlurlRequest request = fullUrl.SetQueryParams(data)
                .WithHeader("x-device-signature", _deviceSignature)
                .WithHeader("x-api-version", string.Format("{0}.0", _version))
                .WithHeader("Accept", "application/json")
                .WithHeader("Accept", string.Format("application/vnd.nest.v{0}+json", _version))
                .WithOAuthBearerToken(_permit.Token);

            if (_basicAuth.Enabled)
            {
                request.WithBasicAuth(_basicAuth.Username, _basicAuth.Password);
            }

            return request;
        }   

        private async Task<ResultSingle<T>> PostAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            string json = await flurlRequest.SendJsonAsync(
                HttpMethod.Post, seed)
                .ReceiveString();

            return ResultSingle<T>.ConvertObject(json, seed);
        }

        private async Task<ResultSingle<T>> GetAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            string json = await flurlRequest.GetStringAsync();

            return ResultSingle<T>.ConvertObject(json, seed);
        }

        private async Task<ResultMultiple<T>> GetListAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            string json = await flurlRequest.GetStringAsync();

            return ResultMultiple<T>.ConvertObject(json, seed);
        }

        private async Task<ResultSingle<T>> PutAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            string json = await flurlRequest.SendJsonAsync(
                HttpMethod.Put, seed)
                .ReceiveString();

            return ResultSingle<T>.ConvertObject(json, seed);
        }
            
        private async Task<ResultSingle<T>> DeleteAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            string json = await flurlRequest.SendJsonAsync(
                HttpMethod.Delete, seed)
                .ReceiveString();

            return ResultSingle<T>.ConvertObject(json, seed);
        }

        private async Task<ResultT> TrySend<PayloadT, ResultT, ResultReturnT>(
            HttpRequest<PayloadT, ResultT> request,
            PayloadT seed, bool keyRequest, IDictionary<string, string> data,
            string subPath = null, bool doCache = true) 
                where PayloadT : Inkton.Nest.Cloud.ICloudObject, new()
                where ResultT : Result<ResultReturnT> , new()
        {
            // Try-send is used to send after a session has been established
            // it attaches the JWT token, attempts retry if failed and also 
            // handle cacheing

            ResultT result = new ResultT();

            if (data == null)
            {
                data = new Dictionary<string, string>();
            }

            if (_permit != null)
            {
                data["token"] = _permit.Token;
            }

            for (int attempt = 0; attempt < _retryCount; attempt++)
            {
                try
                {
                    result = await request(seed, CreateRequest(
                                seed, keyRequest, data, subPath));

                    UpdateCache<PayloadT, ResultT, ResultReturnT>(seed, keyRequest, doCache, result);

                    _notifier?.EndQuery();

                    return result;
                }
                catch (FlurlHttpException ex)
                {
                    SetFailedResult<ResultReturnT>(result, ex);

                    if (result.HttpStatus == System.Net.HttpStatusCode.Unauthorized)
                    {
                        if (_permit != null && _autoTokenRenew)
                        {
                            // Re-try with a fresh token
                            ResultSingle<Permit> permitResult =
                                await QueryTokenAsync();
                            _permit.Token = permitResult.Data.Payload.Token;
                            data["token"] = _permit.Token;
                        }
                        else
                        {
                            _notifier?.EndQuery();
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetFailedResult<ResultReturnT>(result, ex);
                }

                if (attempt < _retryCount)
                {
                    if (_notifier != null && !_notifier.CanProgress(attempt + 1))
                    {
                        // Ask the notifier whether its okay to proceed
                        _notifier?.EndQuery();
                        return result;
                    }

                    // Wait period grows after each re-try. if interval is 2 seconds then 
                    // the each try will be in 2, 4, 8 second intervals etc. 
                    // (Exponential back-off)

                    int waitIntervalSecs = (int)Math.Pow(
                        _retryBaseIntervalInSecs, attempt + 1);

                    System.Console.WriteLine(string.Format("Re-attempt {0}, waiting for - {1} seconds ...",
                        attempt + 1, waitIntervalSecs));

                    _notifier?.Waiting(waitIntervalSecs);

                    Task.Delay(waitIntervalSecs * 1000).Wait();
                }
            }

            return result;
        }

        private void UpdateCache<PayloadT, ResultT, ResultReturnT>(
            PayloadT seed, bool keyRequest, bool doCache, Result<ResultReturnT> result)
                where PayloadT : Inkton.Nest.Cloud.ICloudObject, new()
                where ResultT : Result<ResultReturnT>, new()
        {
            if (result.Code == 0)
            {
                if (doCache)
                {
                    if (keyRequest)
                    {
                        _cache.Save(seed);
                    }
                    else
                    {
                        ObservableCollection<PayloadT> list = result.Data.Payload
                            as ObservableCollection<PayloadT>;

                        if (list != null)
                        {
                            list.All(obj =>
                            {
                                _cache.Save(obj);
                                return true;
                            });
                        }
                    }
                }
                else
                {
                    if (keyRequest)
                    {
                        _cache.Remove(seed);
                    }
                    else
                    {
                        ObservableCollection<PayloadT> list = result.Data.Payload
                            as ObservableCollection<PayloadT>;

                        if (list != null)
                        {
                            list.All(obj =>
                            {
                                _cache.Remove(obj);
                                return true;
                            });
                        }
                    }
                }
            }
        }

        #endregion

        public async Task<ResultSingle<T>> CreateAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            _notifier?.BeginQuery();

            ResultSingle<T> result = await TrySend<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(PostAsync),
                seed, false, data, subPath, doCache);

            _notifier?.EndQuery();

            return result;
        }

        public async Task<ResultSingle<T>> QueryAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            ResultSingle<T> result;
            _notifier?.BeginQuery();

            if (doCache && _cache.Load<T>(seed))
            {
                result = new ResultSingle<T>(0);
                result.Data = new DataContainer<T>();
                result.Data.Payload = seed;
                _notifier?.EndQuery();
                return result;
            }

            result = await TrySend<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(GetAsync),
                seed, true, data, subPath, doCache);

            _notifier?.EndQuery();

            return result;
        }

        public async Task<ResultMultiple<T>> QueryAsyncListAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            _notifier?.BeginQuery();

            ResultMultiple<T> result = await TrySend<T, ResultMultiple<T>, ObservableCollection<T>>(
                new HttpRequest<T, ResultMultiple<T>>(GetListAsync),
                seed, false, data, subPath, doCache);

            _notifier?.EndQuery();

            return result;
        }

        public async Task<ResultSingle<T>> UpdateAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            _notifier?.BeginQuery();

            ResultSingle<T> result = await TrySend<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(PutAsync),
                seed, true, data, subPath, doCache);

            _notifier?.EndQuery();

            return result;
        }

        public async Task<ResultSingle<T>> RemoveAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = false) where T : Inkton.Nest.Cloud.ICloudObject, new()
        {
            _notifier?.BeginQuery();

            ResultSingle<T> result = await TrySend<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(DeleteAsync),
                seed, true, data, subPath, doCache);

            _notifier?.EndQuery();

            return result;
        }
    }
}
