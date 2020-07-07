using System.ComponentModel;

namespace Drexel.VidUp.Business
{
    public enum DayPosition
    {
        First,
        Second,
        Third,
        Fourth,
        Fifth,
        Last,
        [Description("Last-1")]
        LastMinus1,
        [Description("Last-2")]
        LastMinus2,
        [Description("Last-3")]
        LastMinus3,
        [Description("Last-4")]
        LastMinus4
    }
}