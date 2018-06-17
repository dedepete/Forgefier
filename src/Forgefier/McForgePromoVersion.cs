using System;

namespace Forgefier
{
    public class McForgePromoVersion : IComparable<McForgePromoVersion>, IEquatable<McForgePromoVersion>
    {
        public McVersion McVersion { get; }
        public string Tag { get; }
        public McForgePromoVersion(string version)
        {
            string[] splittedVersion = version.Split('-');
            if (splittedVersion.Length == 1) {
                Tag = version;
                return;
            }

            if (splittedVersion.Length > 2) {
                throw new ArgumentOutOfRangeException(version);
            }
            McVersion = new McVersion(splittedVersion[0]);
            Tag = splittedVersion[1];
        }

        public static bool TryParse(string version, out McForgePromoVersion mcForgePromoVersion)
        {
            try {
                mcForgePromoVersion = new McForgePromoVersion(version);
                return true;
            }
            catch {
                mcForgePromoVersion = null;
                return false;
            }
        }

        public int CompareTo(McForgePromoVersion other)
        {
            if (other.McVersion == null && McVersion == null) {
                if (Tag.Equals(other.Tag)) {
                    return 0;
                }

                if (other.Tag == "latest") {
                    return -1;
                }

                if (other.Tag == "recommended") {
                    return 1;
                }
            }

            if (other.McVersion == null) {
                return 1;
            }

            if (other.McVersion.CompareTo(McVersion) == 0) {
                if (Tag.Equals(other.Tag)) {
                    return 0;
                }

                if (other.Tag == "latest") {
                    return -1;
                }

                if (other.Tag == "recommended") {
                    return 1;
                }
            }
            return other.McVersion.CompareTo(McVersion);
        }

        public bool Equals(McForgePromoVersion other)
        {
            return McVersion == other.McVersion && Tag.Equals(other.Tag);
        }

        public override string ToString()
        {
            return (McVersion == null ? string.Empty : McVersion + "-") + Tag;
        }

        public override int GetHashCode()
        {
            return Tag.GetHashCode() ^ (McVersion?.GetHashCode() ?? 1);
        }
    }
}