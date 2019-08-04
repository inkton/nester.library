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

using System.Collections.Generic;
using System.Threading.Tasks;
using Inkton.Nest.Cloud;
using Inkton.Nester.Helpers;

namespace Inkton.Nester.Cloud
{
    public class ResultSingleUI<PayloadT> : ResultSingle<PayloadT>
        where PayloadT : ICloudObject, new()
    {
        public static ResultSingle<PayloadT> WaitForObject(bool throwIfError,
            PayloadT seed, CachedHttpRequest<PayloadT, ResultSingle<PayloadT>> request,
            bool doCache = true, IDictionary<string, string> data = null, string subPath = null)
        {
            ResultSingle<PayloadT> result = WaitAsync(
                Task<ResultSingleUI<PayloadT>>.Run(async () => await request(seed, data, subPath, doCache))
                ).Result;

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public static async Task<ResultSingle<PayloadT>> WaitForObjectAsync(bool throwIfError,
            PayloadT seed, CachedHttpRequest<PayloadT, ResultSingle<PayloadT>> request,
            bool doCache = true, IDictionary<string, string> data = null, string subPath = null)
        {
            ResultSingle<PayloadT> result = await
                request(seed, data, subPath, doCache);

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

    }

    public class ResultMultipleUI<PayloadT> : ResultMultiple<PayloadT>
        where PayloadT : ICloudObject, new()
    {
        public static ResultMultiple<PayloadT> WaitForObjects(bool throwIfError,
            PayloadT seed, CachedHttpRequest<PayloadT, ResultMultiple<PayloadT>> request,
            bool doCache = true, IDictionary<string, string> data = null, string subPath = null)
        {
            ResultMultiple<PayloadT> result = WaitAsync(
                Task<ResultMultiple<PayloadT>>.Run(async () => await request(seed, data, subPath, doCache))
                ).Result;

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }

        public static async Task<ResultMultiple<PayloadT>> WaitForObjectsAsync(bool throwIfError,
            PayloadT seed, CachedHttpRequest<PayloadT, ResultMultiple<PayloadT>> request,
            bool doCache = true, IDictionary<string, string> data = null, string subPath = null)
        {
            ResultMultiple<PayloadT> result = await
                request(seed, data, subPath, doCache);

            if (result.Code < 0 && throwIfError)
            {
                MessageHandler.ThrowMessage(result);
            }

            return result;
        }
    }
}
