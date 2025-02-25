﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Scada.Data.Adapters;
using Scada.Data.Models;
using Scada.Data.Tables;
using Scada.Lang;
using Scada.Log;
using Scada.Server.Archives;
using Scada.Server.Config;
using Scada.Server.Lang;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Scada.Server.Modules.ModArcBasic.Logic
{
    /// <summary>
    /// Implements the historical data archive logic.
    /// <para>Реализует логику архива исторических данных.</para>
    /// </summary>
    internal class BasicHAL : HistoricalArchiveLogic
    {
        private readonly ModuleConfig moduleConfig; // the module configuration
        private readonly BasicHAO options;          // the archive options
        private readonly ILog appLog;               // the application log
        private readonly ILog arcLog;               // the archive log
        private readonly Stopwatch stopwatch;       // measures the time of operations
        private readonly TrendTableAdapter adapter; // reads and writes historical data
        private readonly MemoryCache<DateTime, TrendTable> tableCache; // the cache containing trend tables
        private readonly Slice slice;               // the slice for writing
        private readonly int writingPeriod;         // the writing period in seconds

        private DateTime nextWriteTime;  // the next time to write data to the archive
        private int[] cnlIndexes;        // the channel mapping indexes
        private CnlNumList cnlNumList;   // the list of the channel numbers processed by the archive
        private TrendTable currentTable; // the today's trend table
        private TrendTable updatedTable; // the trend table that is currently being updated


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public BasicHAL(IArchiveContext archiveContext, ArchiveConfig archiveConfig, int[] cnlNums, 
            ModuleConfig moduleConfig) : base(archiveContext, archiveConfig, cnlNums)
        {
            this.moduleConfig = moduleConfig ?? throw new ArgumentNullException(nameof(moduleConfig));
            options = new BasicHAO(archiveConfig.CustomOptions);
            appLog = archiveContext.Log;
            arcLog = options.LogEnabled ? CreateLog(ModuleUtils.ModuleCode) : null;
            stopwatch = new Stopwatch();
            adapter = new TrendTableAdapter
            {
                ArchiveCode = Code,
                CnlNumCache = new MemoryCache<long, CnlNumList>(ModuleUtils.CacheExpiration, ModuleUtils.CacheCapacity)
            };
            tableCache = new MemoryCache<DateTime, TrendTable>(ModuleUtils.CacheExpiration, ModuleUtils.CacheCapacity);
            slice = new Slice(DateTime.MinValue, cnlNums);
            writingPeriod = GetPeriodInSec(options.WritingPeriod, options.WritingUnit);

            nextWriteTime = DateTime.MinValue;
            cnlIndexes = null;
            cnlNumList = new CnlNumList(cnlNums);
            currentTable = null;
            updatedTable = null;
        }


        /// <summary>
        /// Validates the archive options and throws an exception on fail.
        /// </summary>
        private void ValidateOptions()
        {
            if (options.WritingPeriod <= 0)
                throw new ScadaException(ServerPhrases.InvalidWritingPeriod);

            if (options.WritingMode != WritingMode.AutoWithPeriod &&
                options.WritingMode != WritingMode.OnDemandWithPeriod)
            {
                throw new ScadaException(ServerPhrases.WritingModeNotSupported);
            }
        }

        /// <summary>
        /// Checks and updates the today's trend table.
        /// </summary>
        private void CheckCurrentTrendTable(DateTime nowDT)
        {
            currentTable = GetCurrentTrendTable(nowDT);
            string tableDir = adapter.GetTablePath(currentTable);
            string metaFileName = adapter.GetMetaPath(currentTable);

            if (Directory.Exists(tableDir))
            {
                TrendTableMeta srcTableMeta = adapter.ReadMetadata(metaFileName);

                if (srcTableMeta == null)
                {
                    // the existing table is invalid and should be deleted
                    Directory.Delete(tableDir, true);
                }
                else if (srcTableMeta.Equals(currentTable.Metadata))
                {
                    if (currentTable.GetDataPosition(nowDT, PositionKind.Ceiling,
                        out TrendTablePage page, out _))
                    {
                        string pageFileName = adapter.GetPagePath(page);
                        CnlNumList srcCnlNums = adapter.ReadCnlNums(pageFileName);

                        if (srcCnlNums == null)
                        {
                            // make sure that there is no page file
                            File.Delete(pageFileName);
                        }
                        else if (srcCnlNums.Equals(cnlNumList))
                        {
                            // re-create the channel list to use the existing list ID
                            cnlNumList = new CnlNumList(srcCnlNums.ListID, cnlNumList);
                        }
                        else
                        {
                            // update the current page
                            string msg = string.Format(Locale.IsRussian ?
                                "Обновление номеров каналов страницы {0}" :
                                "Update channel numbers of the page {0}", pageFileName);
                            appLog.WriteAction(ServerPhrases.ArchiveMessage, Code, msg);
                            arcLog?.WriteAction(msg);
                            adapter.UpdatePageChannels(page, srcCnlNums);
                        }
                    }
                }
                else
                {
                    // updating the entire table structure would take too long, so just backup the table
                    string msg = string.Format(Locale.IsRussian ?
                        "Резервное копирование таблицы {0}" :
                        "Backup the table {0}", tableDir);
                    appLog.WriteAction(ServerPhrases.ArchiveMessage, Code, msg);
                    arcLog?.WriteAction(msg);
                    adapter.BackupTable(currentTable);
                }
            }

            // create an empty table if it does not exist
            if (!Directory.Exists(tableDir))
            {
                adapter.WriteMetadata(metaFileName, currentTable.Metadata);
                currentTable.IsReady = true;
            }

            // add the archive channel list to the cache
            adapter.CnlNumCache.Add(cnlNumList.ListID, cnlNumList);
        }

        /// <summary>
        /// Gets the today's trend table, creating it if necessary.
        /// </summary>
        private TrendTable GetCurrentTrendTable(DateTime nowDT)
        {
            DateTime today = nowDT.Date;

            if (currentTable == null)
            {
                currentTable = new TrendTable(today, writingPeriod) { CnlNumList = cnlNumList };
                currentTable.SetDefaultMetadata();
            }
            else if (currentTable.TableDate != today) // current date is changed
            {
                tableCache.Add(currentTable.TableDate, currentTable);
                currentTable = new TrendTable(today, writingPeriod) { CnlNumList = cnlNumList };
                currentTable.SetDefaultMetadata();
            }

            return currentTable;
        }

        /// <summary>
        /// Gets the trend table from the cache, creating a table if necessary.
        /// </summary>
        private TrendTable GetTrendTable(DateTime timestamp)
        {
            DateTime tableDate = timestamp.Date;

            if (currentTable != null && currentTable.TableDate == tableDate)
            {
                return currentTable;
            }
            else if (updatedTable != null && updatedTable.TableDate == tableDate)
            {
                return updatedTable;
            }
            else
            {
                TrendTable trendTable = tableCache.Get(tableDate);

                if (trendTable == null)
                {
                    trendTable = new TrendTable(tableDate, writingPeriod) { CnlNumList = cnlNumList };
                    tableCache.Add(tableDate, trendTable);
                }

                return trendTable;
            }
        }


        /// <summary>
        /// Makes the archive ready for operating.
        /// </summary>
        public override void MakeReady()
        {
            ValidateOptions();
            adapter.ParentDirectory = Path.Combine(moduleConfig.SelectArcDir(options.UseCopyDir), Code);
            Directory.CreateDirectory(adapter.ParentDirectory);

            DateTime utcNow = DateTime.UtcNow;
            CheckCurrentTrendTable(utcNow);

            if (options.WritingMode == WritingMode.AutoWithPeriod)
                nextWriteTime = GetNextWriteTime(utcNow, writingPeriod);
        }

        /// <summary>
        /// Deletes the outdated data from the archive.
        /// </summary>
        public override void DeleteOutdatedData()
        {
            DirectoryInfo arcDirInfo = new DirectoryInfo(adapter.ParentDirectory);

            if (arcDirInfo.Exists)
            {
                DateTime minDT = DateTime.UtcNow.AddDays(-options.Retention);
                string minDirName = TrendTableAdapter.GetTableDirectory(Code, minDT);
                appLog.WriteAction(ServerPhrases.DeleteOutdatedData, Code, minDT.ToLocalizedDateString());

                foreach (DirectoryInfo dirInfo in
                    arcDirInfo.EnumerateDirectories(Code + "*", SearchOption.TopDirectoryOnly))
                {
                    if (string.CompareOrdinal(dirInfo.Name, minDirName) < 0)
                        dirInfo.Delete(true);
                }
            }
        }

        /// <summary>
        /// Gets the trends of the specified channels.
        /// </summary>
        public override TrendBundle GetTrends(TimeRange timeRange, int[] cnlNums)
        {
            stopwatch.Restart();
            TrendBundle trendBundle;
            List<TrendBundle> bundles = new List<TrendBundle>();
            int totalCapacity = 0;

            foreach (DateTime date in EnumerateDates(timeRange))
            {
                TrendTable trendTable = GetTrendTable(date);
                TrendBundle bundle = adapter.ReadTrends(trendTable, timeRange, cnlNums);
                bundles.Add(bundle);
                totalCapacity += bundle.Timestamps.Count;
            }

            if (bundles.Count <= 0)
            {
                trendBundle = new TrendBundle(cnlNums, 0);
            }
            else if (bundles.Count == 1)
            {
                trendBundle = bundles[0];
            }
            else
            {
                // unite bundles
                trendBundle = new TrendBundle(cnlNums, totalCapacity);

                foreach (TrendBundle bundle in bundles)
                {
                    trendBundle.Timestamps.AddRange(bundle.Timestamps);

                    for (int i = 0, trendCnt = trendBundle.Trends.Count; i < trendCnt; i++)
                    {
                        trendBundle.Trends[i].AddRange(bundle.Trends[i]);
                    }
                }
            }

            stopwatch.Stop();
            arcLog?.WriteAction(ServerPhrases.ReadingTrendsCompleted, 
                trendBundle.Timestamps.Count, stopwatch.ElapsedMilliseconds);
            return trendBundle;
        }

        /// <summary>
        /// Gets the trend of the specified channel.
        /// </summary>
        public override Trend GetTrend(TimeRange timeRange, int cnlNum)
        {
            stopwatch.Restart();
            Trend resultTrend;
            List<Trend> trends = new List<Trend>();
            int totalCapacity = 0;

            foreach (DateTime date in EnumerateDates(timeRange))
            {
                TrendTable trendTable = GetTrendTable(date);
                Trend trend = adapter.ReadTrend(trendTable, timeRange, cnlNum);
                trends.Add(trend);
                totalCapacity += trend.Points.Count;
            }

            if (trends.Count <= 0)
            {
                resultTrend = new Trend(cnlNum, 0);
            }
            else if (trends.Count == 1)
            {
                resultTrend = trends[0];
            }
            else
            {
                // unite trends
                resultTrend = new Trend(cnlNum, totalCapacity);

                foreach (Trend trend in trends)
                {
                    resultTrend.Points.AddRange(trend.Points);
                }
            }

            stopwatch.Stop();
            arcLog?.WriteAction(ServerPhrases.ReadingTrendCompleted,
                resultTrend.Points.Count, stopwatch.ElapsedMilliseconds);
            return resultTrend;
        }

        /// <summary>
        /// Gets the available timestamps.
        /// </summary>
        public override List<DateTime> GetTimestamps(TimeRange timeRange)
        {
            stopwatch.Restart();
            List<DateTime> resultTimestamps;
            List<List<DateTime>> listOfTimestamps = new List<List<DateTime>>();
            int totalCapacity = 0;

            foreach (DateTime date in EnumerateDates(timeRange))
            {
                TrendTable trendTable = GetTrendTable(date);
                List<DateTime> timestamps = adapter.ReadTimestamps(trendTable, timeRange);
                listOfTimestamps.Add(timestamps);
                totalCapacity += timestamps.Count;
            }

            if (listOfTimestamps.Count <= 0)
            {
                resultTimestamps = new List<DateTime>();
            }
            else if (listOfTimestamps.Count == 1)
            {
                resultTimestamps = listOfTimestamps[0];
            }
            else
            {
                // unite timestamps
                resultTimestamps = new List<DateTime>(totalCapacity);

                foreach (List<DateTime> timestamps in listOfTimestamps)
                {
                    resultTimestamps.AddRange(timestamps);
                }
            }

            stopwatch.Stop();
            arcLog?.WriteAction(ServerPhrases.ReadingTimestampsCompleted,
                resultTimestamps.Count, stopwatch.ElapsedMilliseconds);
            return resultTimestamps;
        }

        /// <summary>
        /// Gets the slice of the specified channels at the timestamp.
        /// </summary>
        public override Slice GetSlice(DateTime timestamp, int[] cnlNums)
        {
            stopwatch.Restart();
            Slice slice = adapter.ReadSlice(GetTrendTable(timestamp), timestamp, cnlNums);
            stopwatch.Stop();
            arcLog?.WriteAction(ServerPhrases.ReadingSliceCompleted, 
                slice.CnlNums.Length, stopwatch.ElapsedMilliseconds);
            return slice;
        }

        /// <summary>
        /// Gets the channel data.
        /// </summary>
        public override CnlData GetCnlData(DateTime timestamp, int cnlNum)
        {
            return adapter.ReadCnlData(GetTrendTable(timestamp), timestamp, cnlNum);
        }

        /// <summary>
        /// Processes new data.
        /// </summary>
        public override bool ProcessData(ICurrentData curData)
        {
            if (options.WritingMode == WritingMode.AutoWithPeriod && nextWriteTime <= curData.Timestamp)
            {
                DateTime writeTime = GetClosestWriteTime(curData.Timestamp, writingPeriod);
                nextWriteTime = writeTime.AddSeconds(writingPeriod);

                stopwatch.Restart();
                TrendTable trendTable = GetCurrentTrendTable(writeTime);
                InitCnlIndexes(curData, ref cnlIndexes);
                CopyCnlData(curData, slice, cnlIndexes);
                slice.Timestamp = writeTime;
                adapter.WriteSlice(trendTable, slice);

                stopwatch.Stop();
                arcLog?.WriteAction(ServerPhrases.WritingSliceCompleted,
                    slice.CnlNums.Length, stopwatch.ElapsedMilliseconds);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Accepts or rejects data with the specified timestamp.
        /// </summary>
        public override bool AcceptData(ref DateTime timestamp)
        {
            return options.PullToPeriod > 0 ?
                PullTimeToPeriod(ref timestamp, writingPeriod, options.PullToPeriod) :
                TimeIsMultipleOfPeriod(timestamp, writingPeriod);
        }

        /// <summary>
        /// Maintains performance when data is written one at a time.
        /// </summary>
        public override void BeginUpdate(DateTime timestamp, int deviceNum)
        {
            stopwatch.Restart();
            updatedTable = GetTrendTable(timestamp);
        }

        /// <summary>
        /// Completes the update operation.
        /// </summary>
        public override void EndUpdate(DateTime timestamp, int deviceNum)
        {
            updatedTable = null;
            stopwatch.Stop();
            arcLog?.WriteAction(ServerPhrases.UpdateCompleted, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Writes the channel data.
        /// </summary>
        public override void WriteCnlData(DateTime timestamp, int cnlNum, CnlData cnlData)
        {
            adapter.WriteCnlData(GetTrendTable(timestamp), timestamp, cnlNum, cnlData);
        }
    }
}
