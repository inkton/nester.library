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
    public class LogViewModel : ViewModel
    {
        private ObservableCollection<NestLog> _nestLogs;
        private ObservableCollection<SystemIOLog> _systemIOLogs;
        private ObservableCollection<SystemRAMLog> _systemRamLogs;
        private ObservableCollection<SystemCPULog> _systemCpuLogs;
        private ObservableCollection<SystemIPLog> _ipLogs;

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

        private MultiSeriesData _ioSeries;
        private MultiSeriesData _ramSeries;
        private MultiSeriesData _cpuSeries;
        private MultiSeriesData _ipSeries;

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

            private Dictionary<string, DataSeriesPoints> _namedSeries;

            public MultiSeriesData()
            {
                _namedSeries = new Dictionary<string, DataSeriesPoints>();
            }

            public Dictionary<string, DataSeriesPoints> Series
            {
                get
                {
                    return _namedSeries;
                }
            }

            public void Init(string name)
            {
                _namedSeries[name] = new MultiSeriesData.DataSeriesPoints();
            }

            public void AddLog(Log log)
            {
                foreach (string key in log.Fields.Keys)
                {
                    if (_namedSeries.Keys.Contains(key))
                    {
                        if (_namedSeries[key].Values == null)
                        {
                            _namedSeries[key].Values = new ObservableCollection<DataSeriesPoints.Point>();
                        }

                        DataSeriesPoints.Point point = new DataSeriesPoints.Point();
                        point.Time = log.Time;
                        point.Value = Convert.ToDouble(log.Fields[key]);
                        _namedSeries[key].Values.Add(point);
                    }
                }
            }

            public void Clear()
            {
                foreach (DataSeriesPoints points in _namedSeries.Values)
                {
                    points.Clear();
                }
            }
        }

        #endregion

        public LogViewModel(NesterService platform, App app) : base(platform, app)
        {
            _cpuSeries = new MultiSeriesData();
            _cpuSeries.Init("User");
            _cpuSeries.Init("System");
            _cpuSeries.Init("IRQ");
            _cpuSeries.Init("Nice");
            _cpuSeries.Init("IOWait");

            _ioSeries = new MultiSeriesData();
            _ioSeries.Init("In");
            _ioSeries.Init("Out");

            _ipSeries = new MultiSeriesData();
            _ipSeries.Init("Sent");
            _ipSeries.Init("Received");

            _ramSeries = new MultiSeriesData();
            _ramSeries.Init("Free");
            _ramSeries.Init("Used");
            _ramSeries.Init("Cached");
            _ramSeries.Init("Buffers");
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

        public ObservableCollection<NestLog> NestLogs
        {
            get
            {
                return _nestLogs;
            }
        }

        public MultiSeriesData CpuSeries
        {
            get
            {
                return _cpuSeries;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesUser
        {
            get
            {
                if (_cpuSeries == null)
                    return null;
                return _cpuSeries.Series["User"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesSystem
        {
            get
            {
                if (_cpuSeries == null)
                    return null;
                return _cpuSeries.Series["System"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesIRQ
        {
            get
            {
                if (_cpuSeries == null)
                    return null;
                return _cpuSeries.Series["IRQ"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesNice
        {
            get
            {
                if (_cpuSeries == null)
                    return null;
                return _cpuSeries.Series["Nice"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> CpuSeriesIOWait
        {
            get
            {
                if (_cpuSeries == null)
                    return null;
                return _cpuSeries.Series["IOWait"].Values;
            }
        }

        public ObservableCollection<SystemCPULog> SystemCPULogs
        {
            get
            { 
                return _systemCpuLogs;
            }
        }

        public MultiSeriesData ioSeries
        {
            get
            {
                return _ioSeries;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IoSeriesIn
        {
            get
            {
                if (_ipSeries == null)
                    return null;
                return _ioSeries.Series["In"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IoSeriesOut
        {
            get
            {
                if (_ioSeries == null)
                    return null;
                return _ioSeries.Series["Out"].Values;
            }
        }

        public ObservableCollection<SystemIOLog> SystemIOLogs
        {
            get
            {
                return _systemIOLogs;
            }
        }

        public MultiSeriesData IpSeries
        {
            get
            {
                return _ipSeries;
            }
        }
        
        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IpSeriesSent
        {
            get
            {
                if (_ipSeries == null)
                    return null;
                return _ipSeries.Series["Sent"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> IpSeriesReceived
        {
            get
            {
                if (_ipSeries == null)
                    return null;
                return _ipSeries.Series["Received"].Values;
            }
        }

        public ObservableCollection<SystemIPLog> SystemIPLogs
        {
            get
            {
                return _ipLogs;
            }
        }

        public MultiSeriesData RamSeries
        {
            get
            {
                return _ramSeries;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesFree
        {
            get
            {
                if (_ramSeries == null)
                    return null;
                return _ramSeries.Series["Free"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesUsed
        {
            get
            {
                if (_ramSeries == null)
                    return null;
                return _ramSeries.Series["Used"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesCached
        {
            get
            {
                if (_ramSeries == null)
                    return null;
                return _ramSeries.Series["Cached"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> RamSeriesBuffers
        {
            get
            {
                if (_ramSeries == null)
                    return null;
                return _ramSeries.Series["Buffers"].Values;
            }
        }

        public ObservableCollection<SystemRAMLog> SystemRAMLogs
        {
            get
            {
                return _systemRamLogs;
            }
        }

        public void ResetBackend()
        {
            // Set the backend address for querying logs and metrics
            Platform.Endpoint = string.Format(
                    "https://{0}/", EditApp.Hostname);
            Platform.BasicAuth = new Inkton.Nester.Cloud.BasicAuth(true,
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
                _nestLogs = result.Data.Payload;
                OnPropertyChanged("NestLogs");
            }

            return result;
        }

        public async Task<ResultMultiple<SystemCPULog>> QuerySystemCPULogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            _cpuSeries.Clear();

            string sql = FormSql("system_cpu", "*", filter, orderBy, limit);
            ResultMultiple<SystemCPULog> result = await QueryLogsAsync<SystemCPULog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                _systemCpuLogs = result.Data.Payload;

                if (_systemCpuLogs.Any())
                {
                    _systemCpuLogs.All(log => { _cpuSeries.AddLog(log); return true; });

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
                _systemIOLogs = result.Data.Payload;

                if (_systemIOLogs.Any())
                {
                    _systemIOLogs.All(log => { _ioSeries.AddLog(log); return true; });

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
            _ipSeries.Clear();

            string sql = FormSql("system_ip", "*", filter, orderBy, limit);
            ResultMultiple<SystemIPLog> result = await QueryLogsAsync<SystemIPLog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                _ipLogs = result.Data.Payload;

                if (_ipLogs.Any())
                {
                    _ipLogs.All(log => { _ipSeries.AddLog(log); return true; });

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
            _ramSeries.Clear();

            string sql = FormSql("system_ram", "*", filter, orderBy, limit);
            ResultMultiple<SystemRAMLog> result = await QueryLogsAsync<SystemRAMLog>(
                sql, doCache, throwIfError);

            if (result.Code == 0)
            {
                _systemRamLogs = result.Data.Payload;

                if (_systemRamLogs.Any())
                {
                    _systemRamLogs.All(log => { _ramSeries.AddLog(log); return true; });

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
            return await ResultMultipleUI<T>.WaitForObjectAsync(
                Platform, doCache, logsSeed, throwIfError, data);
        }
    }
}
