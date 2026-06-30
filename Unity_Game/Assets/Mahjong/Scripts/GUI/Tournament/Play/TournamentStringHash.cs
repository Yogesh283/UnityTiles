namespace Mkey.Tournament
{
    /// <summary>
    /// Deterministic string hash — matches Backend/tournament/level_selector.py (_csharp_string_hash).
    /// Do not use string.GetHashCode(); it is not stable across devices or runtimes.
    /// </summary>
    public static class TournamentStringHash
    {
        public static int Compute(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            uint hash = 0;
            for (int i = 0; i < value.Length; i++)
                hash = (hash * 31 + value[i]) & 0xFFFFFFFF;

            if (hash >= 0x80000000)
                return (int)(hash - 0x100000000);

            return (int)hash;
        }

        public static int ToInt32(long value)
        {
            value &= 0xFFFFFFFF;
            if (value >= 0x80000000)
                value -= 0x100000000;
            return (int)value;
        }
    }
}
