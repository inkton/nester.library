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

namespace Inkton.Nester.Cloud
{
    public class Result<T> : ResultBase<T>
    {
        public HttpStatusCode HttpStatus;

        public Result(int code = -999)
        {
            Code = code;
            Text = "unknown";
            Notes = "none";
            HttpStatus = HttpStatusCode.NotFound;
        }

        public void Throw()
        {
            string message = "Failed to connect to Nest server - Please check Internet connectiviy";

            if (Text != null && Text.Length > 0)
            {
                message = GetLocalDescription();
            }
            if (Notes != null && Notes.Length > 0)
            {
                message += " - " + Notes;
            }

            throw new Exception(message);
        }

        public string GetLocalDescription()
        {
            ResourceManager resmgr = (Application.Current as INesterControl).GetResourceManager();
            return resmgr.GetString(Text,
                System.Globalization.CultureInfo.CurrentUICulture);
        }

        public static Task<ResultT> WaitAsync<ResultT>(Task<ResultT> task)
        {
            // Ensure that awaits were called with .ConfigureAwait(false)

            var wait = new ManualResetEventSlim(false);

            var continuation = task.ContinueWith(_ =>
            {
                wait.Set();
                return _.Result;
            });

            wait.Wait();

            return continuation;
        }

        protected static void FillNullsFrom(CloudObject thisObject, CloudObject otherObject)
        {
            /* Fill otherObject null properties with 
             * this object object properties.
             */

            var sourceProps = otherObject.GetType().GetRuntimeProperties()
                             .Where(x => x.CanWrite).ToList();
            var destProps = otherObject.GetType().GetRuntimeProperties()
                   .Where(x => x.CanWrite).ToList();

            foreach (var sourceProp in sourceProps)
            {
                var value = sourceProp.GetValue(thisObject, null);
                var p = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);

                if (p != null)
                {
                    var valueDst = p.GetValue(otherObject, null);

                    if (valueDst == null)
                    {
                        p.SetValue(otherObject, value, null);
                    }
                }
            }
        }
    }

    class SingleDataContainerConverter<T> : JsonConverter where T : Inkton.Nest.Cloud.CloudObject, new()
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(DataContainer<T>).IsAssignableFrom(objectType);
        }

        protected DataContainer<T> Create(Type objectType, JObject jObject)
        {
            if (objectType.Name.StartsWith("DataContainer"))
            {
                return new DataContainer<T>();
            }

            throw new Exception(String.Format("The given vehicle type {0} is not supported!", objectType.Name));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            DataContainer<T> target = Create(objectType, jObject);
            target.Payload = new T();

            if (jObject[target.Payload.GetObjectName()] != null)
            {
                target.Payload = jObject[target.Payload.GetObjectName()].ToObject<T>(serializer);
            }
                
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    class MultipleDataContainerConverter<T> : JsonConverter where T : Inkton.Nest.Cloud.CloudObject, new()
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(DataContainer<ObservableCollection<T>>).IsAssignableFrom(objectType);
        }

        protected DataContainer<ObservableCollection<T>> Create(Type objectType, JObject jObject)
        {
            if (objectType.Name.StartsWith("DataContainer"))
            {
                return new DataContainer<ObservableCollection<T>>();
            }

            throw new Exception(String.Format("The given vehicle type {0} is not supported!", objectType.Name));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            DataContainer<ObservableCollection<T>> target = Create(objectType, jObject);
            T type = new T();
            string key = type.GetCollectionName();

            if (jObject[key] != null)
            {
                try
                {
                    target.Payload = jObject[key].ToObject<ObservableCollection<T>>();
                }
                catch (Exception)
                {
                    // empty object list throws. 
                    target.Payload = new ObservableCollection<T>();
                }
            }

            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class ResultSingle<PayloadT> : Result<PayloadT> where PayloadT : Inkton.Nest.Cloud.CloudObject, new()
    {
        public ResultSingle() { }

        public ResultSingle(int code) : base(code) { }

        public static ResultSingle<PayloadT> ConvertObject(string json, PayloadT seed)
        {
            ResultSingle<PayloadT> result = JsonConvert.DeserializeObject<ResultSingle<PayloadT>>(json, 
                new SingleDataContainerConverter<PayloadT>());

            if (result.Code == 0 && result.Data != null)
            {
                /* make a complete object by setting null
                 * values in the received object by the seed
                 * object
                 */
                FillNullsFrom(seed, result.Data.Payload);
            }

            return result;
        }

        public static ResultSingle<PayloadT> WaitForObject(bool throwIfError, PayloadT seed,
            CachedHttpRequest<PayloadT, ResultSingle<PayloadT>> request, bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            ResultSingle<PayloadT> result = ResultSingle<PayloadT>.WaitAsync(
                Task<ResultSingle<PayloadT>>.Run(async () => await request(seed, data, subPath, doCache))
                ).Result;

            if (result.Code < 0 && throwIfError)
            {
                result.Throw();
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
                result.Throw();
            }

            return result;
        }

    }

    public class ResultMultiple<PayloadT> : Result<ObservableCollection<PayloadT>> where PayloadT : Inkton.Nest.Cloud.CloudObject, new()
    {
        public ResultMultiple() { }

        public ResultMultiple(int code) : base(code) { }

        public static ResultMultiple<PayloadT> ConvertObject(string json, PayloadT seed)
        {
            ResultMultiple<PayloadT> result =
                JsonConvert.DeserializeObject<ResultMultiple<PayloadT>>(json, 
                    new MultipleDataContainerConverter<PayloadT>());

            if (result.Code == 0 && result.Data != null)
            {
                foreach (var item in result.Data.Payload)
                {
                    /* make a complete object by setting null
                     * values in the received object by the seed
                     * object
                     */
                    FillNullsFrom(seed, item);
                }
            }

            return result;
        }

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
                result.Throw();
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
                result.Throw();
            }

            return result;
        }
    }
}
