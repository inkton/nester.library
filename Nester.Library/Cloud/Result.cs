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
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Inkton.Nester.Models;

namespace Inkton.Nester.Cloud
{
    [JsonObject]
    public class DataContainer<T>
    {
        public T Payload
        {
            get; set;
        }
    }

    class SingleDataContainerConverter<T> : JsonConverter where T : Cloud.ManagedEntity, new()
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

            if (jObject[target.Payload.Entity] != null)
            {
                target.Payload = jObject[target.Payload.Entity].ToObject<T>(serializer);
            }
                
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    class MultipleDataContainerConverter<T> : JsonConverter where T : Cloud.ManagedEntity, new()
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
            string key = type.Collection.TrimEnd('/');

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

    public class ResultBase<T>
    {
        public ResultBase()
        {
            ResultCode = -1;
            ResultText = "Uknown Error";
        }

        [JsonProperty("notes")]
        public string Notes { get; set; }
        [JsonProperty("result_code")]
        public int ResultCode { get; set; }
        [JsonProperty("result_text")]
        public string ResultText { get; set; }
        [JsonProperty("data")]
        public DataContainer<T> Data { get; set; }
    }

    public class ResultSingle<PayloadT> : ResultBase<PayloadT> where PayloadT : Cloud.ManagedEntity, new()
    {
        public static ServerStatus ConvertObject(string json, PayloadT seed)
        {
            ResultSingle<PayloadT> result = JsonConvert.DeserializeObject<ResultSingle<PayloadT>>(json, 
                new SingleDataContainerConverter<PayloadT>());

            if (result.ResultCode == 0 && result.Data != null)
            {
                Cloud.Object.FillBlanks(seed, result.Data.Payload);
            }

            return ServerStatus.FromServerResult<ResultSingle<PayloadT>, PayloadT>(result);
        }

        public static ServerStatus WaitForObject(bool throwIfError, PayloadT seed,
            CachedHttpRequest<PayloadT> request, bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            ServerStatus status = Cloud.Object.WaitAsync(
                Task<ServerStatus>.Run(async () => await request(seed, data, subPath, doCache))
                ).Result;

            if (status.Code < 0 && throwIfError)
            {
                status.Throw();
            }

            return status;
        }

        public static async Task<ServerStatus> WaitForObjectAsync(bool throwIfError, PayloadT seed,
            CachedHttpRequest<PayloadT> request, bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            Cloud.ServerStatus status = await
                request(seed, data, subPath, doCache);

            if (status.Code < 0 && throwIfError)
            {
                status.Throw();
            }

            return status;
        }

    }

    public class ResultMultiple<PayloadT> : ResultBase<ObservableCollection<PayloadT>> where PayloadT : Cloud.ManagedEntity, new()
    {
        public static ServerStatus ConvertObject(string json, PayloadT seed)
        {
            ResultMultiple<PayloadT> result =
                JsonConvert.DeserializeObject<ResultMultiple<PayloadT>>(json, 
                    new MultipleDataContainerConverter<PayloadT>());

            if (result.ResultCode == 0 && result.Data != null)
            {
                foreach (var item in result.Data.Payload)
                {
                    Cloud.Object.FillBlanks(seed, item);
                }
            }

            return ServerStatus.FromServerResult<ResultMultiple<PayloadT>, 
                ObservableCollection<PayloadT>>(result);
        }

        public static ServerStatus WaitForObject(
            NesterService nesterService, bool throwIfError, PayloadT seed,
            bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            Cloud.ServerStatus status = Cloud.Object.WaitAsync(
                Task<ServerStatus>.Run(async () => await nesterService.QueryAsyncListAsync(seed, data, subPath, doCache))
                ).Result;

            if (status.Code < 0 && throwIfError)
            {
                status.Throw();
            }

            return status;
        }

        public static async Task<ServerStatus> WaitForObjectAsync(
            NesterService nesterService, bool throwIfError, PayloadT seed, 
            bool doCache = true, IDictionary<string, string> data = null,
            string subPath = null)
        {
            Cloud.ServerStatus status = await
                nesterService.QueryAsyncListAsync(seed, data, subPath, doCache);

            if (status.Code < 0 && throwIfError)
            {
                status.Throw();
            }

            return status;
        }
    }
}
