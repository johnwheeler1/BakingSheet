// BakingSheet, Maxwell Keonwoo Kang <code.athei@gmail.com>, 2022

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Cathei.BakingSheet.Tests
{
    public class JsonImportTests : IDisposable
    {
        private TestFileSystem _fileSystem;
        private TestLogger _logger;
        private TestSheetContainer _container;
        private JsonSheetConverter _converter;

        public JsonImportTests()
        {
            _fileSystem = new TestFileSystem();
            _logger = new TestLogger();
            _container = new TestSheetContainer(_logger);
            _converter = new JsonSheetConverter("testdata", fileSystem: _fileSystem);
        }

        public void Dispose()
        {
            _fileSystem.Dispose();
        }

        [Theory]
        [InlineData("")]
        [InlineData("{}")]
        [InlineData("!@#$RandomText")]
        public async Task TestImportMalformedJson(string content)
        {
            _fileSystem.SetTestData(Path.Combine("testdata", "Tests.json"), content);

            var result = await _container.Bake(_converter);

            Assert.True(result);
            Assert.Null(_container.Tests);
        }

        [Fact]
        public async Task TestImportEmptyJson()
        {
            _fileSystem.SetTestData(Path.Combine("testdata", "Tests.json"), "[]");

            var result = await _container.Bake(_converter);

            _logger.VerifyNoError();

            Assert.True(result);
            Assert.Empty(_container.Tests);
            Assert.Equal(nameof(TestSheetContainer.Tests), _container.Tests.Name);
        }

        [Fact]
        public async Task TestImportWrongEnum()
        {
            _fileSystem.SetTestData(Path.Combine("testdata", "Types.json"), "[{\"Id\":\"WrongEnum\",\"IntColumn\":345},{\"Id\":\"Alpha\",\"IntColumn\":456}]");

            var result = await _container.Bake(_converter);

            Assert.True(result);
            Assert.Single(_container.Types);
            Assert.Equal(TestEnum.Alpha, _container.Types[0].Id);
            Assert.Equal(456, _container.Types[0].IntColumn);

            _logger.VerifyLog(LogLevel.Error, "Error converting value \"WrongEnum\" to type 'Cathei.BakingSheet.Tests.TestEnum'. Path '[0].Id', line 1, position 18.");
        }

        [Fact]
        public async Task TestImportDuplicatedRow()
        {
            _fileSystem.SetTestData(Path.Combine("testdata", "Types.json"), "[{\"Id\":\"Alpha\",\"IntColumn\":1},{\"Id\":\"Charlie\",\"IntColumn\":2},{\"Id\":\"Alpha\",\"IntColumn\":3},{\"Id\":\"Bravo\",\"IntColumn\":4}]");

            var result = await _container.Bake(_converter);

            Assert.True(result);
            Assert.Equal(3, _container.Types.Count);
            Assert.Equal(1, _container.Types[TestEnum.Alpha].IntColumn);
            Assert.Equal(4, _container.Types[TestEnum.Bravo].IntColumn);

            _logger.VerifyLog(LogLevel.Error, "An item with the same key has already been added. Key: Alpha");
        }

        [Fact]
        public async Task TestImportReferenceJson()
        {
            _fileSystem.SetTestData(Path.Combine("testdata", "Tests.json"), "[{\"Id\":\"Test\"}]");
            _fileSystem.SetTestData(Path.Combine("testdata", "Refers.json"), "[{\"ReferColumn\":\"Test\",\"SelfReferColumn\":null,\"ReferList\":[\"Test\",\"Test\"],\"Arr\":[],\"Id\":\"Refer\"}]");

            var result = await _container.Bake(_converter);

            _logger.VerifyNoError();

            Assert.True(result);
            Assert.Equal(2, _container.Refers["Refer"].ReferList.Count);
            Assert.Equal(_container.Tests["Test"], _container.Refers["Refer"].ReferColumn.Ref);
            Assert.Equal(_container.Tests["Test"], _container.Refers["Refer"].ReferList[0].Ref);
        }

        [Fact]
        public async Task TestImportNestedJson()
        {
            _fileSystem.SetTestData(Path.Combine("testdata", "Nested.json"), "[{\"Struct\":{\"XInt\":0,\"YFloat\":0.0,\"ZList\":null},\"StructList\":[],\"Arr\":[{\"IntList\":[1,2,3]},{\"IntList\":[4,5,6,7,8]}],\"Id\":\"Row1\"},{\"Struct\":{\"XInt\":10,\"YFloat\":50.42,\"ZList\":[\"x\",\"y\"]},\"StructList\":null,\"Arr\":[],\"Id\":\"Row2\"},{\"Struct\":{\"XInt\":0,\"YFloat\":0.0,\"ZList\":null},\"StructList\":[{\"XInt\":1,\"YFloat\":0.124,\"ZList\":[\"a\",\"b\"]},{\"XInt\":2,\"YFloat\":20.0,\"ZList\":[\"c\"]}],\"Arr\":[{\"IntList\":null}],\"Id\":\"Row3\"}]");

            var result = await _container.Bake(_converter);

            _logger.VerifyNoError();

            Assert.True(result);
            Assert.Equal(3, _container.Nested.Count);
            Assert.Equal(2, _container.Nested["Row1"].Count);
            Assert.Equal(3, _container.Nested["Row1"][0].IntList.Count);
            Assert.Equal(2, _container.Nested["Row3"].StructList.Count);
            Assert.Equal(10, _container.Nested["Row2"].Struct.XInt);
            Assert.Equal(1, _container.Nested["Row3"].StructList[0].XInt);
            Assert.Equal("b", _container.Nested["Row3"].StructList[0].ZList[1]);
            Assert.Empty(_container.Nested["Row1"].StructList);
        }

        [Fact]
        public async Task TestImportDictJson()
        {
            _fileSystem.SetTestData(Path.Combine("testdata", "Dict.json"), "[{\"Dict\":{\"A\":10.0,\"B\":20.0},\"Arr\":[{\"NestedDict\":{\"2034\":[\"X\",\"YYY\",\"ZZZZZ\"]},\"Value\":0}],\"Id\":\"Dict1\"},{\"Dict\":{\"C\":10.0,\"B\":20.0},\"Arr\":[],\"Id\":\"Dict2\"},{\"Dict\":null,\"Arr\":[{\"NestedDict\":null,\"Value\":8},{\"NestedDict\":null,\"Value\":65}],\"Id\":\"Empty\"}]");

            var result = await _container.Bake(_converter);

            _logger.VerifyNoError();

            Assert.True(result);
            Assert.Equal(3, _container.Dict.Count);
            Assert.Single(_container.Dict["Dict1"]);
            Assert.Empty(_container.Dict["Dict2"]);
            Assert.Equal(2, _container.Dict["Dict1"].Dict.Count);
            Assert.Equal(10.0f, _container.Dict["Dict2"].Dict["C"]);
            Assert.Equal(3, _container.Dict["Dict1"][0].NestedDict[2034].Count);
            Assert.Equal("YYY", _container.Dict["Dict1"][0].NestedDict[2034][1]);
            Assert.Null(_container.Dict["Empty"].Dict);
        }
    }
}
