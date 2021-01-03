using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Libp2p.Connection.Multiselect;
using NUnit.Framework;

namespace Libp2p.Tests.Connection.Multiselect
{
    public class VarIntTests
    {
        [TestCaseSource(nameof(DecodeData))]
        public ulong Decode_FullInput_ReturnsCorrectValue(byte[] input)
        {
            var str = PipeReader.Create(new MemoryStream(input));
            return VarUInt.Decode(str);
        }

        private static IEnumerable<TestCaseData> DecodeData()
        {
            yield return new TestCaseData(new byte[] { 12 }).Returns(12);
            yield return new TestCaseData(new byte[] { 172, 2 }).Returns(300);
        }

        [Test]
        public void Decode_MissingFollowup_Throws()
        {
            var stream = PipeReader.Create(new MemoryStream(new byte[] { 172 }));
            Assert.That(() => VarUInt.Decode(stream), Throws.Exception);
        }

        [TestCaseSource(nameof(EncodeData))]
        public async Task<byte[]> Encode_ValidInput_ReturnsCorrectValue(ulong input)
        {
            var stream = new MemoryStream();
            var writer = PipeWriter.Create(stream);
            await VarUInt.Encode(input, writer);
            return stream.ToArray();
        }

        private static IEnumerable<TestCaseData> EncodeData()
        {
            yield return new TestCaseData(12UL).Returns(new byte[] { 12 });
            yield return new TestCaseData(300UL).Returns(new byte[] { 172, 2 });
        }

        [TestCase(2883UL)]
        [TestCase(28798783UL)]
        public async Task Encode_DecodeRoundtrip_ReturnsInput(ulong input)
        {
            var pipe = new Pipe();
            await VarUInt.Encode(input, pipe.Writer);
            Assert.That(VarUInt.Decode(pipe.Reader), Is.EqualTo(input));
        }

    }
}
