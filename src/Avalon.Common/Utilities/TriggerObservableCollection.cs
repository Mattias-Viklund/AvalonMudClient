﻿/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Avalon.Common.Triggers;
using System.Buffers;
using System.Collections.Generic;

namespace Avalon.Common.Utilities
{
    /// <summary>
    /// A ObservableCollection of triggers that is thread safe and observable down to the
    /// property level.
    /// </summary>
    public class TriggerObservableCollection : FullyObservableCollection<Trigger>
    {
        /// <summary>
        /// Returns an IEnumerable to iterate over currently enabled gag triggers.
        /// </summary>
        public IEnumerable<Trigger> GagEnumerable()
        {
            int found = 0;
            var pool = ArrayPool<Trigger>.Shared;
            Trigger[] snapshot;

            // We only need the lock while we're creating the temporary snapshot, once
            // that's done we can release and then allow the enumeration to continue.  We
            // will get the count after the lock and then use it.
            try
            {
                Lock.EnterUpgradeableReadLock();

                int count = this.Count;
                snapshot = pool.Rent(count);

                for (int i = 0; i < count; i++)
                {
                    // Make sure the trigger is a gag and that it's enabled.
                    if (this[i].Gag && this[i].Enabled)
                    {
                        snapshot[found] = this[i];
                        found++;
                    }
                }
            }
            finally
            {
                Lock.ExitUpgradeableReadLock();
            }

            // Since the array returned from the pool could be larger than we requested
            // we will use the saved count to only iterate over the items we know to be
            // in the range of the ones we requested.
            for (int i = 0; i < found; i++)
            {
                yield return snapshot[i];
            }

            pool.Return(snapshot, true);
        }

        /// <summary>
        /// Returns an IEnumerable to iterate over only enabled triggers.
        /// </summary>
        public IEnumerable<Trigger> EnabledEnumerable()
        {
            int found = 0;
            var pool = ArrayPool<Trigger>.Shared;
            Trigger[] snapshot;

            // We only need the lock while we're creating the temporary snapshot, once
            // that's done we can release and then allow the enumeration to continue.  We
            // will get the count after the lock and then use it.
            try
            {
                Lock.EnterUpgradeableReadLock();

                int count = this.Count;
                snapshot = pool.Rent(count);

                for (int i = 0; i < count; i++)
                {
                    // Make sure the trigger is a gag and that it's enabled and that
                    // it actually has a pattern.
                    if (this[i].Enabled && !string.IsNullOrWhiteSpace(this[i].Pattern))
                    {
                        snapshot[found] = this[i];
                        found++;
                    }
                }
            }
            finally
            {
                Lock.ExitUpgradeableReadLock();
            }

            // Since the array returned from the pool could be larger than we requested
            // we will use the saved count to only iterate over the items we know to be
            // in the range of the ones we requested.
            for (int i = 0; i < found; i++)
            {
                yield return snapshot[i];
            }

            pool.Return(snapshot, true);
        }

    }
}