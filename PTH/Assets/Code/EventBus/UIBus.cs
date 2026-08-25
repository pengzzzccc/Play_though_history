using UnityEngine;
using System;

namespace PTH.Bus
{
    ///<summary>
    /// UIBus: All interactive objects or scene events should be standardized into this class.
    /// The request function should be implemented for all events.
    /// </summary>
    public static class UIBus
    {
        /// ex bus
        public static event Action<String> ex;

        public static void RaiseEX(String st)
        {
            ex?.Invoke(st);
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ex = null;
        }
    }
}


