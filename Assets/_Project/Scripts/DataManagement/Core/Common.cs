using System;
using Newtonsoft.Json;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Core.Models.Common
{
    [Serializable]
    public sealed class Vector3Data
    {
        [JsonConstructor]
        private Vector3Data()
        {
        }

        public Vector3Data(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [JsonProperty] public float X { get; private set; }
        [JsonProperty] public float Y { get; private set; }
        [JsonProperty] public float Z { get; private set; }

        public static Vector3Data FromUnityVector3(Vector3 vector)
        {
            return new Vector3Data(vector.x, vector.y, vector.z);
        }

        public Vector3 ToUnityVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3Data other)
                return Math.Abs(X - other.X) < float.Epsilon &&
                       Math.Abs(Y - other.Y) < float.Epsilon &&
                       Math.Abs(Z - other.Z) < float.Epsilon;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }

    [Serializable]
    public sealed class QuaternionData
    {
        [JsonConstructor]
        private QuaternionData()
        {
        }

        public QuaternionData(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        [JsonProperty] public float X { get; private set; }
        [JsonProperty] public float Y { get; private set; }
        [JsonProperty] public float Z { get; private set; }
        [JsonProperty] public float W { get; private set; }

        public static QuaternionData FromUnityQuaternion(Quaternion quaternion)
        {
            return new QuaternionData(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }

        public Quaternion ToUnityQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }

        public override bool Equals(object obj)
        {
            if (obj is QuaternionData other)
                return Math.Abs(X - other.X) < float.Epsilon &&
                       Math.Abs(Y - other.Y) < float.Epsilon &&
                       Math.Abs(Z - other.Z) < float.Epsilon &&
                       Math.Abs(W - other.W) < float.Epsilon;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, W);
        }
    }
}