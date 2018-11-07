using System;
using System.Collections;
using System.Collections.Generic;

namespace PartyChat.Master
{
    internal static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random random;

        public static int Next(int maxValue) => (random ?? (random = new Random())).Next();

        public static T Select<T>(IReadOnlyList<T> items) => items[Next(items.Count)];
    }
}