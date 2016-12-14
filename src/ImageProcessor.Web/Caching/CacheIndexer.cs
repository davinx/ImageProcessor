﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheIndexer.cs" company="James Jackson-South">
//   Copyright (c) James Jackson-South.
//   Licensed under the Apache License, Version 2.0.
// </copyright>
// <summary>
//   Represents an in memory collection of keys and values whose operations are concurrent.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using ImageProcessor.Web.Configuration;

namespace ImageProcessor.Web.Caching
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Caching;

    /// <summary>
    /// Represents an in memory collection of cached images whose operations are concurrent.
    /// </summary>
    public static class CacheIndexer
    {
        /// <summary>
        /// Gets the <see cref="CachedImage"/> associated with the specified key.
        /// </summary>
        /// <param name="cachedPath">
        /// The cached path of the value to get.
        /// </param>
        /// <returns>
        /// The <see cref="CachedImage"/> matching the given key if the <see cref="CacheIndexer"/> contains an element with 
        /// the specified key; otherwise, null.
        /// </returns>
        public static CachedImage Get(string cachedPath)
        {
            string key = Path.GetFileNameWithoutExtension(cachedPath);
            CachedImage cachedImage = (CachedImage)MemCache.GetItem(key);
            return cachedImage;
        }

        /// <summary>
        /// Removes the value associated with the specified key.
        /// </summary>
        /// <param name="cachedPath">
        /// The key of the item to remove.
        /// </param>
        /// <returns>
        /// true if the <see cref="CacheIndexer"/> removes an element with 
        /// the specified key; otherwise, false.
        /// </returns>
        public static bool Remove(string cachedPath)
        {
            string key = Path.GetFileNameWithoutExtension(cachedPath);
            return MemCache.RemoveItem(key);
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary or returns the value if it exists.
        /// </summary>
        /// <param name="cachedImage">
        /// The cached image to add.
        /// </param>
        /// <returns>
        /// The value of the item to add or get.
        /// </returns>
        public static CachedImage Add(CachedImage cachedImage)
        {
            // Add the CachedImage with a sliding expiration of 1 minutes.
            CacheItemPolicy policy = new CacheItemPolicy { SlidingExpiration = new TimeSpan(0, 1, 0) };

            if (new Uri(cachedImage.Path).IsFile)
            {
                if (ImageProcessorConfiguration.Instance.UseFileChangeMonitors)
                {
                    //When adding a file to monitor this increases the number of files that ASP.Net will actively monitoring
                    // which directly relates to FCN in ASP.Net. If there are too many monitors then the FCN buffer could overflow
                    // resulting in ASP.Net app domain restarts.

                    //If change monitoring is enabled, we should only monitor the folder, not every individual file. This will
                    // reduce the amount of file monitors, however since there are still a lot of folders generated by IP this number could
                    // still be rather large.

                    //Further to this is that by default ASP.Net will actively monitor these paths anyways so by creating a file change monitor
                    // here and the IP cache is within the web root, there will most likely be duplicate file change monitors created.

                    //If we want to add a monitor per file:
                    policy.ChangeMonitors.Add(new HostFileChangeMonitor(new List<string> { cachedImage.Path }));

                    //If we want to add a monitor per folder, we'd need to create our own implementation of `FileChangeMonitor` (i.e. DirectoryChangeMonitor)
                    // to pass in a folder to be watched and override the correct members
                    //policy.ChangeMonitors.Add(new DirectoryChangeMonitor(new List<string> { Path.GetDirectoryName(cachedImage.Path) }));    
                }


                MemCache.AddItem(Path.GetFileNameWithoutExtension(cachedImage.Key), cachedImage, policy);
            }
            else
            {
                MemCache.AddItem(Path.GetFileNameWithoutExtension(cachedImage.Key), cachedImage, policy);
            }

            return cachedImage;
        }
    }
}