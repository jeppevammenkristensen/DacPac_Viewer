using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.LandingPage;

internal sealed class ObjectIdentifierComparer : IEqualityComparer<ObjectIdentifier>
{
    public bool Equals(ObjectIdentifier? x, ObjectIdentifier? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return (x.Parts ?? []).SequenceEqual(y.Parts ?? []) && (x.ExternalParts ?? []).SequenceEqual(y.ExternalParts ?? []);
    }

    public int GetHashCode(ObjectIdentifier obj)
    {
        var hash = new HashCode();

        foreach (var part in obj.Parts ?? [])
        {
            hash.Add(part);
        }

        foreach (var externalPart in obj.ExternalParts ?? [])
        {
            hash.Add(externalPart);
        }

        return hash.ToHashCode();
    }
}