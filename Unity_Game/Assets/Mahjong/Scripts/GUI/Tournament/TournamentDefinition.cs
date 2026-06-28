using System;
using UnityEngine;

namespace Mkey.Tournament
{
    [Serializable]
    public class TournamentDefinition
    {
        public string id;
        public string icon;
        public string displayName;
        public int maxPlayers;
        public int entryFee;
        public int prizePool;
        public int platformFee;
        public string rewardInfo;
        public int waitingSeconds;
        public string statusLabel;

        public bool HasPlatformFee => platformFee > 0;
    }
}
