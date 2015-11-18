﻿// -----------------------------------------------------------------------
// <copyright file="DiagnoisticsEventThrottlingScheduler.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// <summary></summary>
// -----------------------------------------------------------------------

namespace Microsoft.HockeyApp.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Implementation;

#if WINRT || CORE_PCL || NET45 || NET46 || UWP
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class DiagnoisticsEventThrottlingScheduler 
        : IDiagnoisticsEventThrottlingScheduler, IDisposable
    {
        private readonly IList<TaskTimer> timers = new List<TaskTimer>();
        private volatile bool disposed = false;

        ~DiagnoisticsEventThrottlingScheduler()
        {
            this.Dispose(false);
        }

        public ICollection<object> Tokens
        {
            get
            {
                return new ReadOnlyCollection<object>(this.timers.Cast<object>().ToList());
            }
        }

        public object ScheduleToRunEveryTimeIntervalInMilliseconds(
            int interval,
            Action actionToExecute)
        {
            if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException("interval");
            }

            if (null == actionToExecute)
            {
                throw new ArgumentNullException("actionToExecute");
            }

            var token = InternalCreateAndStartTimer(interval, actionToExecute);
            this.timers.Add(token);

            CoreEventSource.Log.DiagnoisticsEventThrottlingSchedulerTimerWasCreated(interval);

            return token;
        }

        public void RemoveScheduledRoutine(object token)
        {
            if (null == token)
            {
                throw new ArgumentNullException("token");
            }

            var timer = token as TaskTimer;
            if (null == timer)
            {
                throw new ArgumentException("token");
            }

            if (true == this.timers.Remove(timer))
            {
                DisposeTimer(timer);

                CoreEventSource.Log.DiagnoisticsEventThrottlingSchedulerTimerWasRemoved();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Returns a culture-independent string representation of the given <paramref name="exception"/> object, 
        /// appropriate for diagnostics tracing.
        /// </summary>
        internal static string ConvertExceptionToInvariantString(Exception exception)
        {
#if !WINRT && !CORE_PCL && !UWP
            CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
#endif
                return exception.ToString();
#if !WINRT && !CORE_PCL && !UWP
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
#endif
        }

        private static void DisposeTimer(IDisposable timer)
        {
            try
            {
                timer.Dispose();
            }
            catch (Exception exc)
            {
                CoreEventSource.Log.DiagnoisticsEventThrottlingSchedulerDisposeTimerFailure(ConvertExceptionToInvariantString(exc));
            }
        }

        private static TaskTimer InternalCreateAndStartTimer(
            int intervalInMilliseconds,
            Action action)
        {
            var timer = new TaskTimer
            {
                Delay = TimeSpan.FromMilliseconds(intervalInMilliseconds)
            };

            Func<Task> task = null;

            task = () =>
                {
                    timer.Start(task);
                    action();
                    return TaskEx.FromResult<object>(null);
                };

            timer.Start(task);

            return timer;
        }

        private void Dispose(bool managed)
        {
            if (true == managed && false == this.disposed)
            {
                this.DisposeAllTimers();

                GC.SuppressFinalize(this);
            }

            this.disposed = true;
        }

        private void DisposeAllTimers()
        {
            foreach (var timer in this.timers)
            {
                DisposeTimer(timer);
            }

            this.timers.Clear();
        }
    }
}