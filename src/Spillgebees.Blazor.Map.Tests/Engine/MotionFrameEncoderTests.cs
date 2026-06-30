using AwesomeAssertions;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map.Tests.Engine;

public class MotionFrameEncoderTests
{
    [Test]
    public void Should_encode_header_with_magic_epoch_count_and_columns()
    {
        // arrange
        var records = new[] { new EntityMotionRecord(3, 6.1319, 49.6117, 90f, 5f) };

        // act
        var bytes = MotionFrameEncoder.Encode(42, records, includeRotation: true, includeSortKey: true);

        // assert
        BitConverter.ToUInt32(bytes, 0).Should().Be(MotionFrameEncoder.MagicV1);
        BitConverter.ToUInt32(bytes, 4).Should().Be(42u);
        BitConverter.ToUInt32(bytes, 8).Should().Be(1u);
        BitConverter.ToUInt32(bytes, 12).Should().Be(0b111u);
        bytes.Length.Should().Be(16 + 4 + 16 + 4 + 4);
    }

    [Test]
    public void Should_omit_optional_columns_when_disabled()
    {
        // arrange
        var records = new[] { new EntityMotionRecord(0, 1, 2, 0f, 0f), new EntityMotionRecord(1, 3, 4, 0f, 0f) };

        // act
        var bytes = MotionFrameEncoder.Encode(1, records, includeRotation: false, includeSortKey: false);

        // assert
        BitConverter.ToUInt32(bytes, 12).Should().Be(0b001u);
        bytes.Length.Should().Be(16 + (2 * 4) + (2 * 16));
    }

    [Test]
    public void Should_write_columnar_sections_in_bitmask_order()
    {
        // arrange
        var records = new[]
        {
            new EntityMotionRecord(7, 6.131923456789012, 49.6117, 90.5f, 1f),
            new EntityMotionRecord(11, -73.5673, 45.5017, 180f, 2f),
        };

        // act
        var bytes = MotionFrameEncoder.Encode(3, records, includeRotation: true, includeSortKey: true);

        // assert
        BitConverter.ToUInt32(bytes, 16).Should().Be(7u);
        BitConverter.ToUInt32(bytes, 20).Should().Be(11u);
        BitConverter.ToDouble(bytes, 24).Should().Be(6.131923456789012);
        BitConverter.ToDouble(bytes, 32).Should().Be(49.6117);
        BitConverter.ToDouble(bytes, 40).Should().Be(-73.5673);
        BitConverter.ToDouble(bytes, 48).Should().Be(45.5017);
        BitConverter.ToSingle(bytes, 56).Should().Be(90.5f);
        BitConverter.ToSingle(bytes, 60).Should().Be(180f);
        BitConverter.ToSingle(bytes, 64).Should().Be(1f);
        BitConverter.ToSingle(bytes, 68).Should().Be(2f);
    }

    [Test]
    public void Should_match_the_cross_language_golden_frame()
    {
        // arrange
        // The same frame is decoded in src/engine/motion.test.ts ("decodes the
        // cross-language golden frame"); both sides must agree on these bytes.
        var records = new[] { new EntityMotionRecord(3, 6.1319, 49.6117, 90f, 5f) };

        // act
        var base64 = Convert.ToBase64String(
            MotionFrameEncoder.Encode(42, records, includeRotation: true, includeSortKey: true)
        );

        // assert
        base64.Should().Be("AUJHUyoAAAABAAAABwAAAAMAAACeXinLEIcYQEp7gy9MzkhAAAC0QgAAoEA=");
    }
}
