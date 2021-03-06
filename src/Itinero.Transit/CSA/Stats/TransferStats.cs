﻿using System;
using Itinero.Transit.CSA.ConnectionProviders;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero.Transit.CSA
{
    /// <inheritdoc />
    /// <summary>
    /// A simple statistic keeping track of the number of trains taken and the total travel time.
    /// This class uses Pareto Optimization. Use either TotalTimeMinimizer or TotalTransferMinimizer to optimize for one of those
    /// </summary>
    public class TransferStats : IJourneyStats<TransferStats>
    {
        public readonly int NumberOfTransfers;
        public readonly DateTime StartTime;
        public readonly DateTime EndTime;
        public readonly TimeSpan TravelTime;

        public readonly float WalkingDistance;

        public static readonly MinimizeTransfers MinimizeTransfers = new MinimizeTransfers();
        public static readonly MinimizeTravelTimes MinimizeTravelTimes = new MinimizeTravelTimes();

        public static readonly ProfileCompare ProfileCompare = new ProfileCompare();
        public static readonly ParetoCompare ParetoCompare = new ParetoCompare();

        public static readonly ChainedComparator<TransferStats> MinimizeTransfersFirst =
            new ChainedComparator<TransferStats>(MinimizeTransfers, MinimizeTravelTimes);

        public static readonly ChainedComparator<TransferStats> MinimizeTravelTimeFirst =
            new ChainedComparator<TransferStats>(MinimizeTravelTimes, MinimizeTransfers);


        public static readonly TransferStats Factory =
            new TransferStats(int.MaxValue, DateTime.MinValue, DateTime.MaxValue, int.MaxValue);

        public TransferStats(int numberOfTransfers, DateTime startTime, DateTime endTime, float walkingDistance)
        {
            NumberOfTransfers = numberOfTransfers;
            StartTime = startTime;
            EndTime = endTime;
            TravelTime = endTime - startTime;
            WalkingDistance = walkingDistance;
            if (endTime < startTime)
            {
                throw new ArgumentException("Arrivaltime before departuretime");
            }
        }

        public TransferStats InitialStats(IConnection c)
        {
            var walk = 0f;
            if (c is WalkingConnection && !c.DepartureLocation().Equals(c.ArrivalLocation()))
            {
                walk = ((WalkingConnection) c).Walk().TotalDistance;
            }

            return new TransferStats(0, c.DepartureTime(), c.ArrivalTime(), walk);
        }

        public TransferStats Add(Journey<TransferStats> journey)
        {
            var transferred =
                journey.PreviousLink?.GetLastTripId() != null &&
                !Equals(journey.PreviousLink?.GetLastTripId(), journey.GetLastTripId());

            var dep = journey.Connection.DepartureTime();
            if (StartTime < dep)
            {
                dep = StartTime;
            }

            var arr = journey.Connection.ArrivalTime();
            if (EndTime > arr)
            {
                arr = EndTime;
            }

            var walk = 0f;
            if (journey.Connection is WalkingConnection)
            {
                walk = ((WalkingConnection) journey.Connection).Walk().TotalDistance;
            }

            return new TransferStats(NumberOfTransfers + (transferred ? 1 : 0), dep, arr, WalkingDistance + walk);
        }

        public override bool Equals(object obj)
        {
            if (obj is TransferStats other)
            {
                return Equals(other);
            }

            return false;
        }

        internal bool Equals(TransferStats other)
        {
            return NumberOfTransfers == other.NumberOfTransfers &&
                   EndTime.Equals(other.EndTime)
                   && StartTime.Equals(other.StartTime)
                   && Equals(WalkingDistance, other.WalkingDistance);
        }

        public override int GetHashCode()
        {
            return NumberOfTransfers + (EndTime - StartTime).GetHashCode();
        }

        public override string ToString()
        {
            return $"{NumberOfTransfers} transfers, {EndTime - StartTime}, {WalkingDistance}m to walk";
        }
    }


    public class MinimizeTransfers : StatsComparator<TransferStats>
    {
        public override int ADominatesB(TransferStats a, TransferStats b)
        {
            return a.NumberOfTransfers.CompareTo(b.NumberOfTransfers);
        }
    }

    public class MinimizeTravelTimes : StatsComparator<TransferStats>
    {
        public override int ADominatesB(TransferStats a, TransferStats b)
        {
            return (a.EndTime - a.StartTime).CompareTo(b.EndTime - b.StartTime);
        }
    }

    public class ProfileCompare : StatsComparator<TransferStats>
    {
        public override int ADominatesB(TransferStats a, TransferStats b)
        {
            if (a.Equals(b))
            {
                return 0;
            }

            var aBetterThenB = AIsBetterThenB(a, b);
            var bBetterThenA = AIsBetterThenB(b, a);

            if (aBetterThenB && bBetterThenA)
            {
                // No Domination either way
                return int.MaxValue;
            }

            if (aBetterThenB)
            {
                return -1;
            }

            if (bBetterThenA)
            {
                return 1;
            }

            // both perform the same
            return 0;
        }

        /// <summary>
        /// Returns true if A performs better then B in at least one aspect.
        /// This does imply that A dominates B!
        /// This does only imply that B does _not_ dominate A
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool AIsBetterThenB(TransferStats a, TransferStats b)
        {
            return a.NumberOfTransfers < b.NumberOfTransfers
                   || a.StartTime > b.StartTime
                   || a.EndTime < b.EndTime;
        }
    }

    public class ParetoCompare : StatsComparator<TransferStats>
    {
        public override int ADominatesB(TransferStats a, TransferStats b)
        {
            if (a.TravelTime.Equals(b.TravelTime) && a.NumberOfTransfers.Equals(b.NumberOfTransfers))
            {
                return 0;
            }

            if (S1DominatesS2(a, b))
            {
                return -1;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (S1DominatesS2(b, a))
            {
                return 1;
            }

            return int.MaxValue;
        }

        private bool S1DominatesS2(TransferStats s1, TransferStats s2)
        {
            return
                (s1.NumberOfTransfers < s2.NumberOfTransfers
                 && s1.TravelTime <= s2.TravelTime)
                || (s1.NumberOfTransfers <= s2.NumberOfTransfers
                    && s1.TravelTime < s2.TravelTime);
        }
    }
}