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
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Resources;
using System.Reflection;
using System.Linq;
using Xamarin.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;

namespace Inkton.Nester.Cloud
{
    public struct ResultHandler<T>
    {
        private Result<T> _result;

        public ResultHandler(Result<T> result)
        {
            _result = result;
        }

        public string GetMessage()
        {
            string message;

            if (_result.Code >= 0)
            {
                 message = "Sucess!";
            }
            else
            {
                message = "Oops! somthing went wrong!";
            }

            string additionalInfo = "";

            if (_result.Text != null && _result.Text.Length > 0)
            {
                try
                {
                    ResourceManager resmgr = (Application.Current as INesterClient).GetResourceManager();
                    additionalInfo = resmgr.GetString(_result.Text,
                        System.Globalization.CultureInfo.CurrentUICulture);
                }
                catch (Exception) { }
            }
            if (_result.Notes.Length > 0)
            {
                if (additionalInfo.Length > 0)
                    additionalInfo += " - " + _result.Notes;
                else
                    additionalInfo = _result.Notes;
            }

            if (additionalInfo.Length > 0)
            {
                message += "\n" + additionalInfo;
            }

            return message;
        }

        public void Throw()
        {
            throw new Exception(GetMessage());
        }
    }

    public class ResultSingleUI<PayloadT> : ResultSingle<PayloadT> where PayloadT : Inkton.Nest.Cloud.ICloudObject, new()
    {
        public static ResultSingle<PayloadT> WaitForObject(bool throwIfError, PayloadT seed,
            CachedHttpRequest<PayloadT, ResultSingle<PayloadT>> request, bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            ResultSingle<PayloadT> result = ResultSingle<PayloadT>.WaitAsync(
                Task<ResultSingleUI<PayloadT>>.Run(async () => await request(seed, data, subPath, doCache))
                ).Result;

            if (result.Code < 0 && throwIfError)
            {
                new ResultHandler<PayloadT>(result).Throw();
            }

            return result;
        }

        public static async Task<ResultSingle<PayloadT>> WaitForObjectAsync(bool throwIfError, PayloadT seed,
            CachedHttpRequest<PayloadT, ResultSingle<PayloadT>> request, bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            ResultSingle<PayloadT> result = await
                request(seed, data, subPath, doCache);

            if (result.Code < 0 && throwIfError)
            {
                new ResultHandler<PayloadT>(result).Throw();
            }

            return result;
        }

    }

    public class ResultMultipleUI<PayloadT> : ResultMultiple<PayloadT> where PayloadT : Inkton.Nest.Cloud.ICloudObject, new()
    {
        public static ResultMultiple<PayloadT> WaitForObject(
            NesterService nesterService, bool throwIfError, PayloadT seed,
            bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            ResultMultiple<PayloadT> result = ResultMultiple<PayloadT>.WaitAsync(
                Task<ResultMultiple<PayloadT>>.Run(async () => await nesterService.QueryAsyncListAsync(seed, data, subPath, doCache))
                ).Result;

            if (result.Code < 0 && throwIfError)
            {
                new ResultHandler<ObservableCollection<PayloadT>>(result).Throw();
            }
            
            return result;
        }

        public static async Task<ResultMultiple<PayloadT>> WaitForObjectAsync(
            NesterService nesterService, bool throwIfError, PayloadT seed,
            bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            ResultMultiple<PayloadT> result = await
                nesterService.QueryAsyncListAsync(seed, data, subPath, doCache);

            if (result.Code < 0 && throwIfError)
            {
                new ResultHandler<ObservableCollection<PayloadT>>(result).Throw();
            }

            return result;
        }
    }
}
