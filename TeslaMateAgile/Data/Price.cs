using System;

namespace TeslaMateAgile.Data
{
    public class Price
    {
        public decimal Value { get; set; }

        public DateTimeOffset ValidFrom { get; set; }

        public DateTimeOffset ValidTo { get; set; }
    }
}
