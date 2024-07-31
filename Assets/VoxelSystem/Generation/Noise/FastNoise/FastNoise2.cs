using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class Metadata
{
    public struct Member
    {
        public enum VariableType
        {
            Float,
            Int,
            Enum,
            NodeLookup,
            Hybrid,
        }

        public string name;
        public VariableType type;
        public int index;
        public Dictionary<string, int> enumNames;
    }

    public int id;
    public string name;
    public string unformattedName;
    public string description;
    public string groupName;
    public Dictionary<string, Member> members;
}

public class FastNoiseMetadata
{
    public static Dictionary<string, int> metadataNameLookup;
    public static Metadata[] nodeMetadata;

    public static Metadata GetMetadata(string name)
    {
        if (metadataNameLookup.TryGetValue(FormatLookup(name), out int id))
        {
            return nodeMetadata[id];
        }

        return null;
    }

    static FastNoiseMetadata()
    {
        int metadataCount = FastNoise.fnGetMetadataCount();

        nodeMetadata = new Metadata[metadataCount];
        metadataNameLookup = new Dictionary<string, int>(metadataCount);

        // Collect metadata for all FastNoise node classes
        for (int id = 0; id < metadataCount; id++)
        {
            Metadata metadata = new Metadata();
            metadata.id = id;
            metadata.unformattedName = Marshal.PtrToStringAnsi(FastNoise.fnGetMetadataName(id));
            metadata.name = FormatLookup(metadata.unformattedName);

            metadata.description = Marshal.PtrToStringAnsi(FastNoise.fnGetMetadataDescription(id));
            metadata.groupName = Marshal.PtrToStringAnsi(FastNoise.fnGetMetadataGroupName(id));

            metadataNameLookup.Add(metadata.name, id);

            int variableCount = FastNoise.fnGetMetadataVariableCount(id);
            int nodeLookupCount = FastNoise.fnGetMetadataNodeLookupCount(id);
            int hybridCount = FastNoise.fnGetMetadataHybridCount(id);
            metadata.members = new Dictionary<string, Metadata.Member>(variableCount + nodeLookupCount + hybridCount);

            // Init variables
            for (int variableIdx = 0; variableIdx < variableCount; variableIdx++)
            {
                Metadata.Member member = new Metadata.Member();

                member.name =
                    FormatLookup(Marshal.PtrToStringAnsi(FastNoise.fnGetMetadataVariableName(id, variableIdx)));
                member.type =
                    (Metadata.Member.VariableType)FastNoise.fnGetMetadataVariableType(id, variableIdx);
                member.index = variableIdx;

                member.name = FormatDimensionMember(member.name,
                    FastNoise.fnGetMetadataVariableDimensionIdx(id, variableIdx));

                // Get enum names
                if (member.type == Metadata.Member.VariableType.Enum)
                {
                    int enumCount = FastNoise.fnGetMetadataEnumCount(id, variableIdx);
                    member.enumNames = new Dictionary<string, int>(enumCount);

                    for (int enumIdx = 0; enumIdx < enumCount; enumIdx++)
                    {
                        member.enumNames.Add(
                            FormatLookup(
                                Marshal.PtrToStringAnsi(
                                    FastNoise.fnGetMetadataEnumName(id, variableIdx, enumIdx))), enumIdx);
                    }
                }

                metadata.members.Add(member.name, member);
            }

            // Init node lookups
            for (int nodeLookupIdx = 0; nodeLookupIdx < nodeLookupCount; nodeLookupIdx++)
            {
                Metadata.Member member = new Metadata.Member();

                member.name =
                    FormatLookup(
                        Marshal.PtrToStringAnsi(FastNoise.fnGetMetadataNodeLookupName(id, nodeLookupIdx)));
                member.type = Metadata.Member.VariableType.NodeLookup;
                member.index = nodeLookupIdx;

                member.name = FormatDimensionMember(member.name,
                    FastNoise.fnGetMetadataNodeLookupDimensionIdx(id, nodeLookupIdx));

                metadata.members.Add(member.name, member);
            }

            // Init hybrids
            for (int hybridIdx = 0; hybridIdx < hybridCount; hybridIdx++)
            {
                Metadata.Member member = new Metadata.Member();

                member.name =
                    FormatLookup(Marshal.PtrToStringAnsi(FastNoise.fnGetMetadataHybridName(id, hybridIdx)));
                member.type = Metadata.Member.VariableType.Hybrid;
                member.index = hybridIdx;

                member.name = FormatDimensionMember(member.name,
                    FastNoise.fnGetMetadataHybridDimensionIdx(id, hybridIdx));

                metadata.members.Add(member.name, member);
            }

            nodeMetadata[id] = metadata;
        }
    }

    public static int GetMetadataID(string metadataID)
    {
        return metadataNameLookup[FormatLookup(metadataID)];
    }

    // Append dimension char where neccessary 
    private static string FormatDimensionMember(string name, int dimIdx)
    {
        if (dimIdx >= 0)
        {
            char[] dimSuffix = new char[] { 'x', 'y', 'z', 'w' };
            name += dimSuffix[dimIdx];
        }

        return name;
    }

    // Ignores spaces and caps, harder to mistype strings
    private static string FormatLookup(string s)
    {
        return s.Replace(" ", "").ToLower();
    }
}

public unsafe struct OutputMinMax
{
    public OutputMinMax(float minValue = float.PositiveInfinity, float maxValue = float.NegativeInfinity)
    {
        min = minValue;
        max = maxValue;
    }

    public OutputMinMax(float* nativeOutputMinMax)
    {
        min = nativeOutputMinMax[0];
        max = nativeOutputMinMax[1];
    }

    public void Merge(OutputMinMax other)
    {
        min = Math.Min(min, other.min);
        max = Math.Max(max, other.max);
    }

    public float min;
    public float max;
}