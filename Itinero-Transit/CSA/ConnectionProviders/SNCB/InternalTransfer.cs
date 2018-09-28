﻿using System;
using Itinero_Transit.CSA;
using Itinero_Transit.LinkedData;

namespace Itinero_Transit.CSA
{
    /// <inheritdoc />
    /// <summary>
    /// A 'connection' representing a transfer between two platforms, without leaving the station.
    /// They give a fixed transfer time. Normally, the locations of both connections should be the same.
    /// </summary>
    [Serializable()]
    public class InternalTransfer : IConnection
    {
        private readonly Uri _location, _operator; // TODO should be updated to an Uri indicating the platforms
        private readonly DateTime _departureTime, _arrivalTime;

        public InternalTransfer(Uri location, Uri operatorId, DateTime arrivalTime, DateTime departureTime)
        {
            _location = location;
            _arrivalTime = arrivalTime;
            _departureTime = departureTime;
            _operator = operatorId;
        }

        public Uri Operator()
        {
            return _operator;
        }

        public string Mode()
        {
            return "Transfer";
        }

        public Uri Id()
        {
            return _location;
        }

        public Uri Trip()
        {
            return null;
        }

        public Uri Route()
        {
            return null;
        }

        public Uri DepartureLocation()
        {
            return _location;
        }

        public Uri ArrivalLocation()
        {
            return _location;
        }

        public DateTime ArrivalTime()
        {
            return _arrivalTime;
        }

        public DateTime DepartureTime()
        {
            return _departureTime;
        }

        public bool Continuous()
        {
            return true;
        }

        public override string ToString()
        {
            return $"Transfer in {Stations.GetName(_location)} {_departureTime} --> {_arrivalTime}";
        }
    }
}