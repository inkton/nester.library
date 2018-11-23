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
using Flurl;
using Flurl.Http;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Storage;

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
        PayloadT seed, IFlurlRequest flurlRequestl) where PayloadT : CloudObject, new();
    public delegate Task<ResultT> CachedHttpRequest<PayloadT, ResultT>(PayloadT seed,
        IDictionary<string, string> data, string subPath = null, bool doCache = true) where PayloadT : CloudObject, new();

    public class NesterService
    {
        private string _endpoint;
        private BasicAuth _basicAuth;

        private Permit _permit;
        private int _version = 1;
        private StorageService _cache;
        private string _deviceSignature;

        public NesterService(
            int version, string deviceSignature, StorageService cache)
        {
            _version = version;
            _cache = cache;
            _deviceSignature = deviceSignature;
            _endpoint = "https://api.nest.yt/";
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

        public async Task<string> GetIPAsync(string host)
        {
            string ip = null;

            try
            {
                IPAddress[] ipAddress = await Dns.GetHostAddressesAsync(host);
                ip = ipAddress[0].MapToIPv4().ToString();
                return ip;
            }
            catch (Exception) { }

            return ip;
        }

        public ResultSingle<Permit> Signup(Permit permit)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("password", permit.Password);
            data.Add("email", permit.Owner.Email);

            if (permit.SecurityCode != null)
            {
                data.Add("security_code", permit.SecurityCode);
                data.Add("nickname", permit.Owner.Nickname);
                data.Add("first_name", permit.Owner.FirstName);
                data.Add("surname", permit.Owner.LastName);
                data.Add("territory_iso_code", permit.Owner.TerritoryISOCode);
            }

            ResultSingle<Permit> result = ResultSingle<Permit>.WaitAsync(
                Task<ResultSingle<Permit>>.Run(async () => await PostAsync(
                    permit, CreateRequest(permit, false, data)))
                ).Result;

            return result;
        }

        public async Task<ResultSingle<Permit>> SignupAsync(Permit permit)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("password", permit.Password);
            data.Add("email", permit.Owner.Email);

            if (permit.SecurityCode != null)
            {
                data.Add("security_code", permit.SecurityCode);
                data.Add("nickname", permit.Owner.Nickname);
                data.Add("first_name", permit.Owner.FirstName);
                data.Add("surname", permit.Owner.LastName);
                data.Add("territory_iso_code", permit.Owner.TerritoryISOCode);
            }

            return await PostAsync(permit, 
                CreateRequest(permit, false, data));
        }

        public async Task<ResultSingle<Permit>> RecoverPasswordAsync(Permit permit)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("email", permit.Owner.Email);

            ResultSingle<Permit> result = await PutAsync(permit, 
                CreateRequest(permit, true, data));

            return result;
        }

        #region Utility

        private IFlurlRequest CreateRequest<T>(T seed, bool keyRequest,
            IDictionary<string, string> data = null, string subPath = null) where T : Inkton.Nest.Cloud.CloudObject, new()
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
                .WithHeader("Accept", string.Format("application/vnd.nest.v{0}+json", _version));

            if (_basicAuth.Enabled)
            {
                request.WithBasicAuth(_basicAuth.Username, _basicAuth.Password);
            }

            return request;
        }   

        private void LogConnectFailure<T>(
            Result<T> result, Exception ex)
        {       
            if (ex is FlurlHttpException)
            {
                FlurlHttpException httpEx = ex as FlurlHttpException;
                if (httpEx.Call.Response != null)
                {
                    result.HttpStatus = httpEx.Call.Response.StatusCode;
                }
            }

            Helpers.ErrorHandler.Exception(
                ex.Message, ex.StackTrace);
            result.Notes = "Failed to connect to remote server";
        }

        public ResultSingle<Permit> QueryToken(Permit permit = null)
        {
            if (permit != null)
            {
                _permit = permit;
            }

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("password", _permit.Password);

            ResultSingle<Permit> result = ResultSingle<Permit>.WaitAsync(
                Task<ResultSingle<Permit>>.Run(async () => await GetAsync(
                    _permit, CreateRequest(
                        _permit, true, data)))
                ).Result;

            return result;
        }

        public async Task<ResultSingle<Permit>> QueryTokenAsync(Permit permit = null)
        {
            if (permit != null)
            {
                _permit = permit;
            }

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("password", _permit.Password);

            ResultSingle<Permit> result = await GetAsync(
                _permit, CreateRequest(
                    _permit, true, data));

            return result;
        }

        public async Task<ResultSingle<Permit>> ResetTokenAsync(Permit newPermit)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("token", _permit.Token);
            data.Add("password", newPermit.Password);

            ResultSingle<Permit> result = await DeleteAsync(
                _permit, CreateRequest(
                    _permit, true, data));

            return result;
        }

        private async Task<ResultSingle<T>> PostAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            string json = await flurlRequest.PostJsonAsync(seed)
                    .ReceiveString();

            return ResultSingle<T>.ConvertObject(json, seed);
        }

        private async Task<ResultSingle<T>> GetAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            string json = await flurlRequest.GetAsync()
                    .ReceiveString();

            return ResultSingle<T>.ConvertObject(json, seed);
        }

        private async Task<ResultMultiple<T>> GetListAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            string json = await flurlRequest.GetAsync()
                    .ReceiveString();

            return ResultMultiple<T>.ConvertObject(json, seed);
        }

        private async Task<ResultSingle<T>> PutAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            string objJson = JsonConvert.SerializeObject(seed);
            var httpContent = new StringContent(objJson, Encoding.UTF8, "application/json");

            string json = await flurlRequest
                        .PutAsync(httpContent)
                        .ReceiveString();

            return ResultSingle<T>.ConvertObject(json, seed);
        }
            
        private async Task<ResultSingle<T>> DeleteAsync<T>(
            T seed, IFlurlRequest flurlRequest) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            string objJson = JsonConvert.SerializeObject(seed);
            var httpContent = new StringContent(objJson, Encoding.UTF8, "application/json");

            string json = await flurlRequest.DeleteAsync()
                    .ReceiveString();

            return ResultSingle<T>.ConvertObject(json, seed);
        }

        private async Task<ResultT> RetryWithFreshToken<PayloadT, ResultT, ResultReturnT>(
            HttpRequest<PayloadT, ResultT> request,
            PayloadT seed, bool keyRequest, IDictionary<string, string> data,
            string subPath = null, bool doCache = true) 
                where PayloadT : Inkton.Nest.Cloud.CloudObject, new()
                where ResultT : Result<ResultReturnT> , new()
        {
            int retryCount = 3;
            ResultT result = new ResultT();

            if (data == null)
            {
                data = new Dictionary<string, string>();
            }

            for (int i = 0; i < retryCount; i++)
            {
                // the service allows some non-secure calls
                // such as browse all apps. these do not
                // require a permit.           
                if (_permit != null)
                {
                    if (data.Keys.Contains("token"))
                    {
                        data.Remove("token");
                    }

                    data.Add("token", _permit.Token);
                }

                try
                {
                    result = await request(seed, CreateRequest(
                                seed, keyRequest, data, subPath));

                    if (result.HttpStatus != System.Net.HttpStatusCode.Unauthorized)
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

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    LogConnectFailure<ResultReturnT>(result, ex);
                }

                if (_permit != null)
                {
                    // Re-try with a fresh token
                    _permit = QueryToken().Data.Payload;
                }
            }

            return result;
        }

        #endregion

        public async Task<ResultSingle<T>> CreateAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            return await RetryWithFreshToken<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(PostAsync),
                seed, false, data, subPath, doCache);
        }

        public async Task<ResultSingle<T>> QueryAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            if (doCache && _cache.Load<T>(seed))
            {
                ResultSingle<T> result = new ResultSingle<T>(0);
                result.Data.Payload = seed;
                return result;
            }

            return await RetryWithFreshToken<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(GetAsync),
                seed, true, data, subPath, doCache);
        }

        public async Task<ResultMultiple<T>> QueryAsyncListAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            /*
             * todo: Load list from cache
             *
            if (doCache && _storage.Load<T>(seed))
            {
                Cloud.ServerStatus result = new ServerStatus(0);
                result.Payload = seed;
                return result;
            }
            */

            return await RetryWithFreshToken<T, ResultMultiple<T>, ObservableCollection<T>>(
                new HttpRequest<T, ResultMultiple<T>>(GetListAsync),
                seed, false, data, subPath, doCache);
        }

        public async Task<ResultSingle<T>> UpdateAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            return await RetryWithFreshToken<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(PutAsync),
                seed, true, data, subPath, doCache);
        }

        public async Task<ResultSingle<T>> RemoveAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = false) where T : Inkton.Nest.Cloud.CloudObject, new()
        {
            return await RetryWithFreshToken<T, ResultSingle<T>, T>(
                new HttpRequest<T, ResultSingle<T>>(DeleteAsync),
                seed, true, data, subPath, doCache);
        }
    }
}
