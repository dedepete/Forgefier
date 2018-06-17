using System;

namespace Forgefier
{
    public class McVersion : IComparable<McVersion>, IEquatable<McVersion>
    {
        public Version Version { get; }
        public string Tag { get; }
        public McVersion(string version)
        {
            string[] splittedVersion = version.Split('_');
            Version = Version.Parse(splittedVersion[0]);
            if (splittedVersion.Length > 1) {
                Tag = splittedVersion[1];
            }
        }

        public int CompareTo(McVersion other)
        {
            if (other == null) {
                return -1;
            }
            int toReturn = Version.CompareTo(other.Version);
            if (toReturn == 0 && Tag != null && other.Tag != null) {
                toReturn = string.Compare(Tag, other.Tag, StringComparison.Ordinal);
            }

            return toReturn;
        }

        public bool Equals(McVersion other)
        {
            return CompareTo(other) == 0;
        }

        public override string ToString()
        {
            return Version + (Tag == null ? string.Empty : "_" + Tag);
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode() ^ (Tag?.GetHashCode() ?? 1);
        }
    }
}