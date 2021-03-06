﻿using System;
using System.IO;
using System.Net.Http;
using CacheCow.Client;
using CacheCow.Client.FileCacheStore;
using CacheCow.Client.Headers;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero.Transit.LinkedData
{
    /// <summary>
    /// Utilities to help downloading, caching and testing (e.g. to inject a fixed string while testing)
    /// </summary>
    public class Downloader : IDocumentLoader
    {
        /// <summary>
        /// This string can be set during tests, in which this string will _always_ be given as "downloaded" string
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public string AlwaysReturn = null;

        public int DownloadCounter;
        public int CacheHits;
        public double TimeDownloading;

        private readonly HttpClient _client;

        public Downloader(bool caching = true)
        {
            if (caching)
            {
                var store = new FileStore("cache");
                _client = store.CreateClient();
            }
            else
            {
                _client = new HttpClient();
            }

            _client.DefaultRequestHeaders.Add("user-agent", "Itinero-Transit-dev/0.0.1");
            _client.DefaultRequestHeaders.Add("accept", "application/ld+json");
        }

        public JToken LoadDocument(Uri uri)
        {
            return JObject.Parse(DownloadRaw(uri));
        }


        /// <summary>
        /// Actually download the contents.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public string DownloadRaw(Uri uri)
        {
            if (AlwaysReturn != null)
            {
                // Used for testing
                return AlwaysReturn;
            }

            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                var u = uri.ToString();
                uri = new Uri(u.Substring(0, u.Length - uri.Fragment.Length));
                
            }
            DownloadCounter++;
            var start = DateTime.Now;
            
            Log.Information($"Downloading {uri}...");

            var response = _client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new FileNotFoundException("Could not open " + uri);
            }

            var data = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var end = DateTime.Now;

            var cacheHit= response.Headers.GetCacheCowHeader() != null &&
                response.Headers.GetCacheCowHeader().ToString().Contains("did-not-exist=false");
            if (cacheHit)
            {
                CacheHits++;
            }

            var timeNeeded = (end - start).TotalMilliseconds/1000;
            Log.Information($"Downloading {uri} completed in {timeNeeded}s, got {data.Length} bytes; hit cache: {cacheHit}");
            TimeDownloading +=timeNeeded;
            return data;
        }

        // ReSharper disable once UnusedMember.Global
        public void ResetCounters()
        {
            TimeDownloading = 0;
            DownloadCounter = 0;
            CacheHits = 0;
        }
    }
}