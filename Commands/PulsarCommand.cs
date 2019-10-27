using System;
using System.Collections.Immutable;

namespace Commands
{
    public class PulsarCommand
    {
        public string Command { get; }
        public ImmutableDictionary<string, string> Payload { get; }
        public PulsarCommand(string command, ImmutableDictionary<string, string> payload)
        {
            Command = command;
            Payload = payload;
        }
    }
}
