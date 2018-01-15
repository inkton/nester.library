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
using Inkton.Nester.Models;

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

    public delegate Task<Cloud.ServerStatus> HttpRequest<T>(T seed,
        IDictionary<string, string> data, string subPath = null) where T : Cloud.ManagedEntity, new();
    public delegate Task<Cloud.ServerStatus> CachedHttpRequest<T>(T seed,
        IDictionary<string, string> data, string subPath = null, bool doCache = true);

    public class NesterService
    {
        private string _endpoint;
        private BasicAuth _basicAuth;

        private Permit _permit;
        private Cache.StorageService _storage;
        private int _version = 1;

        public NesterService()
        {
            _endpoint = "https://api.nest.yt/";

            _storage = new Cache.StorageService();
        }

        public int Version
        {
            get { return _version; }
            set { _version = value; }
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

        public ServerStatus Signup(Permit permit)
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

            ServerStatus status = Object.WaitAsync(
                Task<ServerStatus>.Run(async () => await PostAsync(permit, data))
                ).Result;

            return status;
        }

        public async Task<ServerStatus> SignupAsync(Permit permit)
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

            return await PostAsync(permit, data);
        }

        public async Task<ServerStatus> RecoverPasswordAsync(Permit permit)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("email", permit.Owner.Email);

            ServerStatus status = await PutAsync(permit, data);

            return status;
        }

        #region Utility

        private string GetVersionHeader()
        {
            return string.Format("application/vnd.nest.v{0}+json", _version);
        }

        private void LogConnectFailure(
            ref Cloud.ServerStatus result, Exception ex)
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

        public ServerStatus QueryToken(Permit permit = null)
        {
            if (permit != null)
            {
                _permit = permit;
            }

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("password", _permit.Password);

            ServerStatus status = Object.WaitAsync(
                Task<ServerStatus>.Run(async () => await GetAsync(_permit, data))
                ).Result;

            return status;
        }

        public async Task<ServerStatus> QueryTokenAsync(Permit permit = null)
        {
            if (permit != null)
            {
                _permit = permit;
            }

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("password", _permit.Password);

            ServerStatus status = await GetAsync(_permit, data);

            return status;
        }

        public async Task<Cloud.ServerStatus> ResetTokenAsync(Permit newPermit)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("token", _permit.Token);
            data.Add("password", newPermit.Password);

            Cloud.ServerStatus status = await DeleteAsync(_permit, data);

            return status;
        }

        private async Task<Cloud.ServerStatus> PostAsync<T>(T seed,
            IDictionary<string, string> data, string subPath = null) where T : Cloud.ManagedEntity, new()
        {
            Cloud.ServerStatus result = new Cloud.ServerStatus(
                ServerStatus.NEST_RESULT_ERROR_LOCAL);

            try
            {
                string fullUrl = Endpoint + seed.Collection;
                if (subPath != null)
                {
                    fullUrl = fullUrl + subPath;
                }

                string json = string.Empty;

                if (_basicAuth.Enabled)
                {
                    json = await fullUrl.SetQueryParams(data)
                        .WithBasicAuth(_basicAuth.Username, _basicAuth.Password)
                        .WithHeader("Accept", GetVersionHeader())
                        .PostJsonAsync(seed)
                        .ReceiveString();
                }
                else
                {
                    json = await fullUrl.SetQueryParams(data)
                        .WithHeader("Accept", GetVersionHeader())
                        .PostJsonAsync(seed)
                        .ReceiveString();
                }

                result = Cloud.ResultSingle<T>.ConvertObject(json, seed);
            }
            catch (Exception ex)
            {
                LogConnectFailure(ref result, ex);
            }

            return result;
        }

        private async Task<Cloud.ServerStatus> GetAsync<T>(T seed,
            IDictionary<string, string> data, string subPath = null) where T : Cloud.ManagedEntity, new()
        {
            Cloud.ServerStatus result = new Cloud.ServerStatus(
                ServerStatus.NEST_RESULT_ERROR_LOCAL);

            try
            {
                string fullUrl = Endpoint + seed.CollectionKey;
                if (subPath != null)
                {
                    fullUrl = fullUrl + subPath;
                }

                string json = string.Empty;

                if (_basicAuth.Enabled)
                {
                    json = await fullUrl.SetQueryParams(data)
                            .WithHeader("Accept", GetVersionHeader())
                            .WithBasicAuth(_basicAuth.Username, _basicAuth.Password)
                            .GetAsync().ReceiveString();
                }
                else
                {
                    json = await fullUrl.SetQueryParams(data)
                            .WithHeader("Accept", GetVersionHeader())
                            .GetAsync()
                            .ReceiveString();
                }

                result = Cloud.ResultSingle<T>.ConvertObject(json, seed);
            }
            catch (Exception ex)
            {
                LogConnectFailure(ref result, ex);
            }

            return result;
        }

        private async Task<Cloud.ServerStatus> PutAsync<T>(T seed,
            IDictionary<string, string> data, string subPath = null) where T : Cloud.ManagedEntity, new()
        {
            Cloud.ServerStatus result = new Cloud.ServerStatus(
                ServerStatus.NEST_RESULT_ERROR_LOCAL);

            try
            {
                string fullUrl = Endpoint + seed.CollectionKey;
                if (subPath != null)
                {
                    fullUrl = fullUrl + subPath;
                }

                string objJson = JsonConvert.SerializeObject(seed);
                var httpContent = new StringContent(objJson, Encoding.UTF8, "application/json");

                string json = string.Empty;

                if (_basicAuth.Enabled)
                {
                    json = await fullUrl.SetQueryParams(data)
                        .WithHeader("Accept", GetVersionHeader())
                        .WithBasicAuth(_basicAuth.Username, _basicAuth.Password)
                        .PutAsync(httpContent)
                        .ReceiveString();
                }
                else
                {
                    json = await fullUrl.SetQueryParams(data)
                        .WithHeader("Accept", GetVersionHeader())
                        .PutAsync(httpContent)
                        .ReceiveString();
                }

                result = Cloud.ResultSingle<T>.ConvertObject(json, seed);
            }
            catch (Exception ex)
            {
                LogConnectFailure(ref result, ex);
            }

            return result;
        }

        private async Task<Cloud.ServerStatus> DeleteAsync<T>(T seed,
            IDictionary<string, string> data, string subPath = null) where T : Cloud.ManagedEntity, new()
        {
            Cloud.ServerStatus result = new Cloud.ServerStatus(
                ServerStatus.NEST_RESULT_ERROR_LOCAL);

            try
            {
                string fullUrl = Endpoint + seed.CollectionKey;
                if (subPath != null)
                {
                    fullUrl = fullUrl + subPath;
                }

                string objJson = JsonConvert.SerializeObject(seed);
                var httpContent = new StringContent(objJson, Encoding.UTF8, "application/json");
                string json = string.Empty;

                if (_basicAuth.Enabled)
                {
                    json = await fullUrl.SetQueryParams(data)
                        .WithHeader("Accept", GetVersionHeader())
                        .WithBasicAuth(_basicAuth.Username, _basicAuth.Password)
                        .DeleteAsync()
                        .ReceiveString();
                }
                else
                {
                    json = await fullUrl.SetQueryParams(data)
                        .WithHeader("Accept", GetVersionHeader())
                        .DeleteAsync()
                        .ReceiveString();
                }

                result = Cloud.ResultSingle<T>.ConvertObject(json, seed);
            }
            catch (Exception ex)
            {
                LogConnectFailure(ref result, ex);
            }

            return result;
        }

        private async Task<Cloud.ServerStatus> RetryWithFreshToken<T>(HttpRequest<T> request,
            T seed, IDictionary<string, string> data,
            string subPath = null, bool doCache = true) where T : Cloud.ManagedEntity, new()
        {
            int retryCount = 3;
            Cloud.ServerStatus result = new Cloud.ServerStatus(
                ServerStatus.NEST_RESULT_ERROR_LOCAL);

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

                result = await request(seed, data, subPath);

                if (result.HttpStatus != System.Net.HttpStatusCode.Unauthorized)
                {
                    if (result.Code == 0)
                    {
                        if (doCache)
                        {
                            _storage.Save(seed);
                        }
                        else
                        {
                            _storage.Remove(seed);
                        }
                    }
                    break;
                }

                // Token expired, get another
                _permit = QueryToken().PayloadToObject<Permit>();
            }

            return result;
        }

        #endregion

        public async Task<Cloud.ServerStatus> CreateAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Cloud.ManagedEntity, new()
        {
            return await RetryWithFreshToken(new HttpRequest<T>(PostAsync),
                seed, data, subPath, doCache);
        }

        public async Task<Cloud.ServerStatus> QueryAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Cloud.ManagedEntity, new()
        {
            if (doCache && _storage.Load<T>(seed))
            {
                Cloud.ServerStatus status = new Cloud.ServerStatus(0);
                status.Payload = seed;
                return status;
            }

            return await RetryWithFreshToken(new HttpRequest<T>(GetAsync),
                seed, data, subPath, doCache);
        }

        public async Task<Cloud.ServerStatus> QueryAsyncListAsync<T>(T seed,
            IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Cloud.ManagedEntity, new()
        {
            int retryCount = 3;
            Cloud.ServerStatus result = new Cloud.ServerStatus(
                   ServerStatus.NEST_RESULT_ERROR_LOCAL);

            if (data == null)
            {
                data = new Dictionary<string, string>();
            }

            string fullUrl = Endpoint + seed.Collection;
            if (subPath != null)
            {
                fullUrl = fullUrl + subPath;
            }

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    if (_permit != null)
                    {
                        if (data.Keys.Contains("token"))
                        {
                            data.Remove("token");
                        }

                        data.Add("token", _permit.Token);
                    }

                    string json = string.Empty;

                    if (_basicAuth.Enabled)
                    {
                        json = await fullUrl
                                .WithHeader("Accept", GetVersionHeader())
                                .SetQueryParams(data)
                                .WithBasicAuth(_basicAuth.Username, _basicAuth.Password)
                                .GetAsync().ReceiveString();
                    }
                    else
                    {
                        json = await fullUrl
                                .WithHeader("Accept", GetVersionHeader())
                                .SetQueryParams(data)
                                .GetAsync().ReceiveString();
                    }

                    result = ResultMultiple<T>.ConvertObject(json, seed);
                }
                catch (Exception ex)
                {
                    LogConnectFailure(ref result, ex);
                }

                if (result.HttpStatus != System.Net.HttpStatusCode.Unauthorized)
                {
                    if (result.Code == 0)
                    {
                        ObservableCollection<T> list = result.Payload as ObservableCollection<T>;

                        if (doCache)
                        {                            
                            list.All(obj =>
                            {
                                _storage.Save(obj);
                                return true;
                            });
                        }
                        else
                        {
                            list.All(obj =>
                            {
                                _storage.Remove(obj);
                                return true;
                            });
                        }
                    }
                    break;
                }

                // Token expired, get another
                _permit = QueryToken().PayloadToObject<Permit>();
            }

            return result;
        }

        public async Task<Cloud.ServerStatus> UpdateAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = true) where T : Cloud.ManagedEntity, new()
        {
            return await RetryWithFreshToken(new HttpRequest<T>(PutAsync),
                seed, data, subPath, doCache);
        }

        public async Task<Cloud.ServerStatus> RemoveAsync<T>(T seed,
                IDictionary<string, string> data = null, string subPath = null, bool doCache = false) where T : Cloud.ManagedEntity, new()
        {
            return await RetryWithFreshToken(new HttpRequest<T>(DeleteAsync),
                seed, data, subPath, doCache);
        }
    }
}
