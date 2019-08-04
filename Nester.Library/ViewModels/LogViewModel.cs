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
using System.Threading.Tasks;
using Inkton.Nest.Cloud;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;

namespace Inkton.Nester.ViewModels
{
    public class LogViewModel<UserT> : ViewModel<UserT>
        where UserT : User, new()
    {
        [Flags]
        public enum QueryIndex : byte
        {
            QueryIndexNestLog = 0x01,
            QueryIndexIO = 0x02,
            QueryIndexRam = 0x04,
            QueryIndexCpu = 0x08,
            QueryIndexIP = 0x10,
            QueryIndexAll = QueryIndexNestLog | QueryIndexIO |
                            QueryIndexRam | QueryIndexCpu | QueryIndexIP
        }

        private QueryIndex _queryIndexs = QueryIndex.QueryIndexAll;
        private NestLog _editNestLog;

        #region Data Classes

        public class DataSeries { }

        public class MultiCategoryData : DataSeries
        {
            protected Log _dataLog;

            public Log DataLog
            {
                get
                {
                    return _dataLog;
                }
                set
                {
                    _dataLog = value;
                }
            }
        }

        public class MultiSeriesData
        {
            public class DataSeriesPoints : DataSeries
            {
                public class Point
                {
                    public DateTime Time { get; set; }
                    public double Value { get; set; }
                }

                public ObservableCollection<Point> Values;

                public DataSeriesPoints()
                {
                    Values = new ObservableCollection<Point>();
                }

                public void Clear()
                {
                    Values.Clear();
                }
            }

            public MultiSeriesData()
            {
                Series = new Dictionary<string, DataSeriesPoints>();
            }

            public Dictionary<string, DataSeriesPoints> Series { get; }

            public void Init(string name)
            {
                Series[name] = new MultiSeriesData.DataSeriesPoints();
            }

            public void AddLog(Log log)
            {
                foreach (string key in log.Fields.Keys)
                {
                    if (Series.Keys.Contains(key))
                    {
                        if (Series[key].Values == null)
                        {
                            Series[key].Values = new ObservableCollection<DataSeriesPoints.Point>();
                        }

                        DataSeriesPoints.Point point = new DataSeriesPoints.Point();
                        point.Time = log.Time;
                        point.Value = Convert.ToDouble(log.Fields[key]);
                        Series[key].Values.Add(point);
                    }
                }
            }

            public void Clear()
            {
                foreach (DataSeriesPoints points in Series.Values)
                {
                    points.Clear();
                }
            }
        }

        #endregion

        public LogViewModel(BackendService<UserT> backend, App app) : base(backend, app)
        {
            CpuSeries = new MultiSeriesData();
            CpuSeries.Init("User");
            CpuSeries.Init("System");
            CpuSeries.Init("IRQ");
            CpuSeries.Init("Nice");
            CpuSeries.Init("IOWait");

            ioSeries = new MultiSeriesData();
            ioSeries.Init("In");
            ioSeries.Init("Out");

            IpSeries = new MultiSeriesData();
            IpSeries.Init("Sent");
            IpSeries.Init("Received");

            RamSeries = new MultiSeriesData();
            RamSeries.Init("Free");
            RamSeries.Init("Used");
            RamSeries.Init("Cached");
            RamSeries.Init("Buffers");
        }

        override public App EditApp
        {
            get
            {
                return _editApp;
            }
            set
            {
                SetProperty(ref _editApp, value);
            }
        }

        public QueryIndex QueryIndexs
        {
            get
            {
                return _queryIndexs;
            }
            set
            {
                SetProperty(ref _queryIndexs, value);
            }
        }

        public NestLog EditNestLog
        {
            get
            {
                return _editNestLog;
            }
            set
            {
                SetProperty(ref _editNestLog, value);
            }
        }

        public ObservableCollection<NestLog> NestLogs { get; private set; }

        public MultiSeriesData CpuSeries { get; }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesUser
        {
            get
            {
                if (CpuSeries == null)
                    return null;
                return CpuSeries.Series["User"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesSystem
        {
            get
            {
                if (CpuSeries == null)
                    return null;
                return CpuSeries.Series["System"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesIRQ
        {
            get
            {
                if (CpuSeries == null)
                    return null;
                return CpuSeries.Series["IRQ"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesNice
        {
            get
            {
                if (CpuSeries == null)
                    return null;
                return CpuSeries.Series["Nice"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesIOWait
        {
            get
            {
                if (CpuSeries == null)
                    return null;
                return CpuSeries.Series["IOWait"].Values;
            }
        }

        public ObservableCollection<SystemCPULog> SystemCPULogs { get; private set; }

        public MultiSeriesData ioSeries { get; }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IoSeriesIn
        {
            get
            {
                if (IpSeries == null)
                    return null;
                return ioSeries.Series["In"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IoSeriesOut
        {
            get
            {
                if (ioSeries == null)
                    return null;
                return ioSeries.Series["Out"].Values;
            }
        }

        public ObservableCollection<SystemIOLog> SystemIOLogs { get; private set; }

        public MultiSeriesData IpSeries { get; }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IpSeriesSent
        {
            get
            {
                if (IpSeries == null)
                    return null;
                return IpSeries.Series["Sent"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IpSeriesReceived
        {
            get
            {
                if (IpSeries == null)
                    return null;
                return IpSeries.Series["Received"].Values;
            }
        }

        public ObservableCollection<SystemIPLog> SystemIPLogs { get; private set; }

        public MultiSeriesData RamSeries { get; }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesFree
        {
            get
            {
                if (RamSeries == null)
                    return null;
                return RamSeries.Series["Free"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesUsed
        {
            get
            {
                if (RamSeries == null)
                    return null;
                return RamSeries.Series["Used"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesCached
        {
            get
            {
                if (RamSeries == null)
                    return null;
                return RamSeries.Series["Cached"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesBuffers
        {
            get
            {
                if (RamSeries == null)
                    return null;
                return RamSeries.Series["Buffers"].Values;
            }
        }

        public ObservableCollection<SystemRAMLog> SystemRAMLogs { get; private set; }

        public void ResetBackend()
        {
            // Set the backend address for querying logs and metrics
            Backend.Endpoint = string.Format(
                    "https://{0}/", EditApp.Hostname);
            Backend.BasicAuth = new Inkton.Nest.Cloud.BasicAuth(true,
                    EditApp.Tag, EditApp.NetworkPassword);
        }

        public async Task QueryMetricsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            if ((_queryIndexs & QueryIndex.QueryIndexCpu) == QueryIndex.QueryIndexCpu)
            {
                await QuerySystemCPULogsAsync(
                    filter, orderBy, limit, doCache, throwIfError);
            }

            if ((_queryIndexs & QueryIndex.QueryIndexIO) == QueryIndex.QueryIndexIO)
            {
                await QuerySystemIOLogsAsync(
                    filter, orderBy, limit, doCache, throwIfError);
            }

            if ((_queryIndexs & QueryIndex.QueryIndexIP) == QueryIndex.QueryIndexIP)
            {
                await QuerSystemIPLogsAsync(
                        filter, orderBy, limit, doCache, throwIfError);
            }

            if ((_queryIndexs & QueryIndex.QueryIndexRam) == QueryIndex.QueryIndexRam)
            {
                await QuerSystemRAMLogsAsync(
                        filter, orderBy, limit, doCache, throwIfError);
             }
        }

        public async Task QueryAsync(long UnixEpochSecsSince, 
            bool doCache = false, bool throwIfError = true)
        {
            await QueryNestLogsAsync(string.Format("id > {0}",
                        UnixEpochSecsSince * 1000000 // to microseconds
                    ), "id asc", 200, doCache, throwIfError);

            await QueryMetricsAsync(string.Format("id > {0}",
                        UnixEpochSecsSince
                    ), "id asc", 200, doCache, throwIfError);
        }

        public async Task QueryAsync(
            long beginUnixEpochSecs, long endUnixEpochSecs, int rows = -1,
            bool doCache = false, bool throwIfError = true)
        {
            await QueryNestLogsAsync(string.Format("id >= {0} and id < {1}",
                        beginUnixEpochSecs * 1000000, endUnixEpochSecs * 1000000  // to microseconds
                    ), null, rows, doCache, throwIfError);

            await QueryMetricsAsync(string.Format("id >= {0} and id < {1}",
                        beginUnixEpochSecs, endUnixEpochSecs
                    ), null, rows, doCache, throwIfError);
        }

        public async Task QueryAsync(bool last, int rows,
            bool doCache = false, bool throwIfError = true)
        {
            // fetch rows
            string orderBy = string.Format("id {0}",
                        last ? "desc" : "asc"
                    );

            await QueryNestLogsAsync(null, orderBy, rows, doCache, throwIfError);
            
            await QueryMetricsAsync(null, orderBy, rows, doCache, throwIfError);
        }

        private string FormSql(string table, string fields = "*",
            string filter = null, string orderBy = null, int limit = -1)
        {
            string sql = string.Format("select {0} from {1}", fields, table);

            if (filter != null)
            {
                sql += " where " + filter;
            }
            if (orderBy != null)
            {
                sql += " order by " + orderBy;
            }
            if (limit >= 0)
            {
                sql += " limit " + limit.ToString();
            }
            return sql;
        }

        public async Task<ResultMultiple<NestLog>> QueryNestLogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            string sql = FormSql("nest_log", "*", filter, orderBy, limit);
            ResultMultiple<NestLog> result = await QueryLogsAsync<NestLog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                NestLogs = result.Data.Payload;
                OnPropertyChanged("NestLogs");
            }

            return result;
        }

        public async Task<ResultMultiple<SystemCPULog>> QuerySystemCPULogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            CpuSeries.Clear();

            string sql = FormSql("system_cpu", "*", filter, orderBy, limit);
            ResultMultiple<SystemCPULog> result = await QueryLogsAsync<SystemCPULog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                SystemCPULogs = result.Data.Payload;

                if (SystemCPULogs.Any())
                {
                    SystemCPULogs.All(log => { CpuSeries.AddLog(log); return true; });

                    OnPropertyChanged("CpuSeriesUser");
                    OnPropertyChanged("CpuSeriesSystem");
                    OnPropertyChanged("CpuSeriesIRQ");
                    OnPropertyChanged("CpuSeriesNice");
                    OnPropertyChanged("CpuSeriesIOWait");
                }
            }

            return result;
        }

        public async Task<ResultMultiple<SystemIOLog>> QuerySystemIOLogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            string sql = FormSql("system_io", "*", filter, orderBy, limit);
            ResultMultiple<SystemIOLog> result = await QueryLogsAsync<SystemIOLog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                SystemIOLogs = result.Data.Payload;

                if (SystemIOLogs.Any())
                {
                    SystemIOLogs.All(log => { ioSeries.AddLog(log); return true; });

                    OnPropertyChanged("IoSeriesIn");
                    OnPropertyChanged("IoSeriesOut");
                }
            }

            return result;
        }

        public async Task<ResultMultiple<SystemIPLog>> QuerSystemIPLogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            IpSeries.Clear();

            string sql = FormSql("system_ip", "*", filter, orderBy, limit);
            ResultMultiple<SystemIPLog> result = await QueryLogsAsync<SystemIPLog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                SystemIPLogs = result.Data.Payload;

                if (SystemIPLogs.Any())
                {
                    SystemIPLogs.All(log => { IpSeries.AddLog(log); return true; });

                    OnPropertyChanged("IpSeriesSent");
                    OnPropertyChanged("IpSeriesReceived");
                }
            }

            return result;
        }

        public async Task<ResultMultiple<SystemRAMLog>> QuerSystemRAMLogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            RamSeries.Clear();

            string sql = FormSql("system_ram", "*", filter, orderBy, limit);
            ResultMultiple<SystemRAMLog> result = await QueryLogsAsync<SystemRAMLog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                SystemRAMLogs = result.Data.Payload;

                if (SystemRAMLogs.Any())
                {
                    SystemRAMLogs.All(log => { RamSeries.AddLog(log); return true; });

                    OnPropertyChanged("RamSeriesFree");
                    OnPropertyChanged("RamSeriesUsed");
                    OnPropertyChanged("RamSeriesCached");
                    OnPropertyChanged("RamSeriesBuffers");
                }
            }

            return result;
        }

        public async Task<ResultMultiple<T>> QueryLogsAsync<T>(string sql,
            bool doCache = false, bool throwIfError = false) where T : Log, new()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("sql", sql);

            T logsSeed = new T();
            return await ResultMultipleUI<T>.WaitForObjectsAsync(
                true, logsSeed, new CachedHttpRequest<T, ResultMultiple<T>>(
                    Backend.QueryAsyncListAsync), true);
        }
    }
}
