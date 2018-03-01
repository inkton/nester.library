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
using Inkton.Nester.Models;

namespace Inkton.Nester.ViewModels
{
    public class LogViewModel : ViewModel
    {
        private ObservableCollection<NestLog> _nestLogs;
        private ObservableCollection<DiskSpaceLog> _diskSpaceLogs;
        private ObservableCollection<SystemRAMLog> _systemRamLogs;
        private ObservableCollection<SystemCPULog> _systemCpuLogs;
        private ObservableCollection<SystemIPV4Log> _ipv4Logs;

        [Flags]
        public enum QueryIndex : byte
        {
            QueryIndexNestLog = 0x01,
            QueryIndexDiskSpace = 0x02,
            QueryIndexRam = 0x04,
            QueryIndexCpu = 0x08,
            QueryIndexIpV4 = 0x10,
            QueryIndexAll = QueryIndexNestLog | QueryIndexDiskSpace |
                            QueryIndexRam | QueryIndexCpu | QueryIndexIpV4
        }

        private QueryIndex _queryIndexs = QueryIndex.QueryIndexAll;
        private NestLog _editNestLog;

        private MultiCategoryData _diskSpaceData;
        private MultiSeriesData _ramSeries;
        private MultiSeriesData _cpuSeries;
        private MultiSeriesData _ipv4Series;

        #region Data Classes

        public class DataSeries
        {
            public DataSeries()
            {
            }
        }

        public class MultiCategoryData : DataSeries
        {
            protected Log _dataLog;

            public MultiCategoryData()
            {
            }
            
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

            private Dictionary<string, DataSeriesPoints> _namedSeries = 
                new Dictionary<string, DataSeriesPoints>();

            public MultiSeriesData()
            {
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

        public LogViewModel(App app) : base(app)
        {
            _cpuSeries = new MultiSeriesData();
            _cpuSeries.Init("User");
            _cpuSeries.Init("System");
            _cpuSeries.Init("IRQ");
            _cpuSeries.Init("Nice");
            _cpuSeries.Init("IOWait");

            _diskSpaceData = new MultiCategoryData();

            _ipv4Series = new MultiSeriesData();
            _ipv4Series.Init("Sent");
            _ipv4Series.Init("Received");

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

        public MultiCategoryData DiskSpaceSeries
        {
            get
            {
                return _diskSpaceData;
            }
        }

        public ObservableCollection<DiskSpaceLog> DiskSpaceLogs
        {
            get
            {
                return _diskSpaceLogs;
            }
        }

        public MultiSeriesData Ipv4Series
        {
            get
            {
                return _ipv4Series;
            }
        }
        
        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> Ipv4SeriesSent
        {
            get
            {
                if (_ipv4Series == null)
                    return null;
                return _ipv4Series.Series["Sent"].Values;
            }
        }

        public ObservableCollection<MultiSeriesData.DataSeriesPoints.Point> Ipv4SeriesReceived
        {
            get
            {
                if (_ipv4Series == null)
                    return null;
                return _ipv4Series.Series["Received"].Values;
            }
        }

        public ObservableCollection<SystemIPV4Log> SystemIPV4Logs
        {
            get
            {
                return _ipv4Logs;
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

        public async Task QueryMetricsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            if ((_queryIndexs & QueryIndex.QueryIndexCpu) == QueryIndex.QueryIndexCpu)
            {
                await QuerySystemCPULogsAsync(
                    filter, orderBy, limit, doCache, throwIfError);
            }

            if ((_queryIndexs & QueryIndex.QueryIndexDiskSpace) == QueryIndex.QueryIndexDiskSpace)
            {
                await QueryDiskSpaceLogsAsync(
                    filter, orderBy, limit, doCache, throwIfError);
            }

            if ((_queryIndexs & QueryIndex.QueryIndexIpV4) == QueryIndex.QueryIndexIpV4)
            {
                await QuerSystemIPV4LogsAsync(
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

        public async void QueryAsync(
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

        public async Task<Cloud.ServerStatus> QueryNestLogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            string sql = FormSql("nest_log", "*", filter, orderBy, limit);
            Cloud.ServerStatus status = await QueryLogsAsync<NestLog>(
                sql, doCache, throwIfError);

            _nestLogs = status.PayloadToList<NestLog>();

            OnPropertyChanged("NestLogs");
            return status;
        }

        public async Task<Cloud.ServerStatus> QuerySystemCPULogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            _cpuSeries.Clear();

            string sql = FormSql("system_cpu", "*", filter, orderBy, limit);
            Cloud.ServerStatus status = await QueryLogsAsync<SystemCPULog>(
                sql, doCache, throwIfError);

            _systemCpuLogs = status.PayloadToList<SystemCPULog>();

            if (_systemCpuLogs.Any())
            {
                _systemCpuLogs.All(log => { _cpuSeries.AddLog(log); return true; });

                OnPropertyChanged("CpuSeriesUser");
                OnPropertyChanged("CpuSeriesSystem");
                OnPropertyChanged("CpuSeriesIRQ");
                OnPropertyChanged("CpuSeriesNice");
                OnPropertyChanged("CpuSeriesIOWait");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryDiskSpaceLogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            string sql = FormSql("disk_space", "*", filter, orderBy, limit);
            Cloud.ServerStatus status = await QueryLogsAsync<DiskSpaceLog>(
                sql, doCache, throwIfError);

            _diskSpaceLogs = status.PayloadToList<DiskSpaceLog>();

            if (_diskSpaceLogs.Any())
            {
                _diskSpaceData.DataLog = _diskSpaceLogs.Last();

                OnPropertyChanged("DiskSpaceSeries");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QuerSystemIPV4LogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            _ipv4Series.Clear();

            string sql = FormSql("system_ipv4", "*", filter, orderBy, limit);
            Cloud.ServerStatus status = await QueryLogsAsync<SystemIPV4Log>(
                sql, doCache, throwIfError);

            _ipv4Logs = status.PayloadToList<SystemIPV4Log>();

            if (_ipv4Logs.Any())
            {
                _ipv4Logs.All(log => { _ipv4Series.AddLog(log); return true; });

                OnPropertyChanged("Ipv4SeriesSent");
                OnPropertyChanged("Ipv4SeriesReceived");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QuerSystemRAMLogsAsync(
            string filter = null, string orderBy = null, int limit = -1,
            bool doCache = false, bool throwIfError = true)
        {
            _ramSeries.Clear();

            string sql = FormSql("system_ram", "*", filter, orderBy, limit);
            Cloud.ServerStatus status = await QueryLogsAsync<SystemRAMLog>(
                sql, doCache, throwIfError);

            _systemRamLogs = status.PayloadToList<SystemRAMLog>();

            if (_systemRamLogs.Any())
            {
                _systemRamLogs.All(log => { _ramSeries.AddLog(log); return true; });

                OnPropertyChanged("RamSeriesFree");
                OnPropertyChanged("RamSeriesUsed");
                OnPropertyChanged("RamSeriesCached");
                OnPropertyChanged("RamSeriesBuffers");
            }

            return status;
        }

        public async Task<Cloud.ServerStatus> QueryLogsAsync<T>(string sql,
            bool doCache = false, bool throwIfError = false) where T : Log, new()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("sql", sql);

            T logsSeed = new T();
            Cloud.ServerStatus status = await Cloud.ResultMultiple<T>.WaitForObjectAsync(
                NesterControl.DeployedApp, doCache, logsSeed, throwIfError, data);

            return status;
        }
    }
}
