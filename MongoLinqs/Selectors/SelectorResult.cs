﻿namespace MongoLinqs.Selectors
{
    public class SelectorResult
    {
        public SelectorResultKind Kind { get; set; }
        public string Script { get; set; }
    }
}