﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.CSA;
using Itinero.Transit.CSA.ConnectionProviders;
using Itinero.Transit.CSA.ConnectionProviders.LinkedConnection;
using Itinero.Transit.CSA.Connections;
using Itinero.Transit.CSA.Data;
using Itinero.Transit.CSA.LocationProviders;
using Itinero.Transit.LinkedData;
using JsonLD.Core;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero.Transit
{
    public static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void TestStuff(IDocumentLoader loader)
        {
            var deLijn = DeLijn.Profile(loader, new LocalStorage("cache/delijn"), "belgium.routerdb");
            var sncb = Sncb.Profile(loader, new LocalStorage("cache/sncb"), "belgium.routerdb");
            var connections =
                new ConnectionProviderMerger(new List<IConnectionsProvider> {sncb, deLijn});

            var osmLocations = new OsmLocationMapping();

            var locations =
                new LocationCombiner(new List<ILocationProvider>
                {
                    sncb, deLijn
                });


            var footpaths
                = new TransferGenerator("belgium.routerdb");

            var profile = new Profile<TransferStats>(
                connections,
                locations,
                footpaths,
                TransferStats.Factory,
                TransferStats.ProfileCompare,
                TransferStats.ParetoCompare
            );

            var startLoc
                = osmLocations.GetCoordinateFor(new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048"));
            var endLoc
                = osmLocations.GetCoordinateFor(new Uri("https://www.openstreetmap.org/#map=17/51.21560/2.87952"));

            var startTime = new DateTime(2018, 10, 30, 10, 00, 00);
            var endTime = new DateTime(2018, 10, 30, 12, 00, 00);

            var walksIn
                = profile.WalkToClosebyStops
                    (DateTime.Now, startLoc, profile.EndpointSearchRadius);
            var walksOut
                = profile.WalkFromClosebyStops(
                    DateTime.Now, endLoc, profile.EndpointSearchRadius);
            var pcs = new ProfiledConnectionScan<TransferStats>(
                walksIn, walksOut, startTime, endTime, profile);

            var journeys = 
                new List<Journey<TransferStats>>
                    (pcs.CalculateJourneys()[startLoc.Uri.ToString()]);


            foreach (var j in journeys)
            {
                Log.Information(j.ToString(profile));
            }

        }


        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            ConfigureLogging();
            Log.Information("Starting...");
            var startTime = DateTime.Now;

            var loader = new Downloader();
            try

            {
                TestStuff(loader);
            }
            catch (Exception e)
            {
                Log.Error(e, "Something went horribly wrong");
            }

            var endTime = DateTime.Now;
            Log.Information($"Calculating took {(endTime - startTime).TotalSeconds}");
            Log.Information(
                $"Downloading {loader.DownloadCounter} entries took {loader.TimeDownloading} sec; got {loader.CacheHits} cache hits"
            );
        }


        private static void ConfigureLogging()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logFile = Path.Combine("logs", $"log-Itinero-Transit-{date}.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(new JsonFormatter(), logFile)
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}