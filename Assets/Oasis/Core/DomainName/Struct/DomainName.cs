using System;
using Unity.Collections;
using Oasis.Grpc;
using UnityEngine.Serialization;

namespace Oasis.Core
{
    [Serializable]
    public struct DomainName : IEquatable<DomainName>
    {
        public FixedString64Bytes version;
        public FixedString64Bytes domain;
        public FixedString64Bytes name;

        public DomainName(Oasis.Grpc.DomainName domainName)
        {
            version = domainName.Version;
            domain = domainName.Domain;
            name = domainName.Name;
        }
        public DomainName(string version, string domain, string name)
        {
            this.version = version;
            this.domain = domain;
            this.name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is DomainName other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (name.GetHashCode() * 397) ^ domain.GetHashCode();
            }
        }

        public override string ToString()
        {
            return version + " : " + domain + " : " + name;
        }

        public Grpc.DomainName ToGrpc()
        {
            return new Grpc.DomainName {Name = name.ToString(), Domain = domain.ToString(), Version = version.ToString()};
        }

        public bool Equals(DomainName other)
        {
            return name == other.name && domain == other.domain;
        }
    }
}