using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Streams;

namespace TestGrains
{
    public class ChatSequesnceToken : StreamSequenceToken
    {
        private readonly long _token;

        public ChatSequesnceToken(long token)
        {
            _token = token;
        }

        public override bool Equals(StreamSequenceToken other)
        {
            return (other as ChatSequesnceToken)?._token == _token;
        }

        public override int CompareTo(StreamSequenceToken other)
        {
            var ctt = other as ChatSequesnceToken;

            if (ctt == null) return 1;
            return _token.CompareTo(ctt._token);
        }
    }
}
