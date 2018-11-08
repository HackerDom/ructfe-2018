using System;
using System.Text;

namespace PartyChat.Master
{
    internal static class StringBuilderCache
    {
        private const int MaxBuilderSize = 1024;
 
        [ThreadStatic]
        private static StringBuilder cachedInstance;
 
        public static Handle Acquire(int capacity = 128)
        {
            if (capacity > MaxBuilderSize)
                return new Handle(new StringBuilder(capacity));
            
            var builder = cachedInstance;
            if (builder == null || capacity > builder.Capacity)
                return new Handle(new StringBuilder(capacity));

            cachedInstance = null;
            builder.Clear();
            
            return new Handle(builder);
        }
 
        private static void Release(StringBuilder builder)
        {
            if (builder.Capacity <= MaxBuilderSize)
                cachedInstance = builder;
        }

        public class Handle : IDisposable
        {
            public StringBuilder Builder { get; }

            public Handle(StringBuilder instance) => Builder = instance;

            public void Dispose() => Release(Builder);
        }
    }
}